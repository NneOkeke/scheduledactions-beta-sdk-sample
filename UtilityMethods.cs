using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule.Models;
using Azure.ResourceManager.Resources;

namespace ComputeScheduleSampleProject
{
    public static class UtilityMethods
    {
        private static readonly int PollintIntervalUpperBoundInSeconds = 1;
        private static readonly int PollintIntervalLowerBoundInSeconds = 16;
        private static readonly int InitialWaitTimeBeforePollingInMilliseconds = 10000;


        public static SubscriptionResource GetSubscriptionResource(ArmClient client, string subscriptionId)
        {
            ResourceIdentifier subscriptionResourceId = SubscriptionResource.CreateResourceIdentifier(subscriptionId);
            return client.GetSubscriptionResource(subscriptionResourceId);
        }

        public static bool IsOperationStateComplete(ScheduledActionOperationState? state)
        {
            return state != null &&
                (state == ScheduledActionOperationState.Succeeded |
                state == ScheduledActionOperationState.Failed ||
                state == ScheduledActionOperationState.Cancelled);
        }

        /// <summary>
        /// Determine if polling should continue based on the response from GetOperationsRequest
        /// </summary>
        /// <param name="response"> Response from GetOperationsRequest that is used to determine if polling should continue </param>
        /// <param name="totalVmsCount">Total number of virtual machines in the initial Start/Hibernate/Deallocate operation </param>
        /// <param name="completedOps"> Dictionary of completed operations, that is, operations where state is either Succeeded, Failed, Cancelled </param>
        /// <returns></returns>
        public static bool ShouldRetryPolling(GetOperationStatusResult response, int totalVmsCount, Dictionary<string, ResourceOperationDetails> completedOps)
        {
            var shouldRetry = true;

            foreach (var operationResult in response.Results)
            {
                // When ResourceOperationResult.ErrorCode is not null, it means the operation was never created in ScheduledActions due to an error in the Azure stack, this failure will not cancel the submit/execute request for other operations in the batch
                if (operationResult.ErrorCode != null)
                {
                    completedOps.TryAdd(operationResult.Operation.OperationId, operationResult.Operation);
                    Console.WriteLine($"Operation {operationResult.Operation.OperationId} failed with error code {operationResult.Operation.ResourceOperationError.ErrorCode}");
                }
                else
                {
                    if (IsOperationStateComplete(operationResult.Operation.State))
                    {
                        completedOps.TryAdd(operationResult.Operation.OperationId, operationResult.Operation);
                        Console.WriteLine($"Operation {operationResult.Operation.OperationId} completed with state {operationResult.Operation.State}");
                    }
                }
            }

            if (completedOps.Count == totalVmsCount)
            {
                shouldRetry = false;
            }
            return shouldRetry;
        }


        /// <summary>
        /// Removes the operations that have completed from the list of operations to poll
        /// </summary>
        /// <param name="completedOps"> Dictionary of completed operations, that is, operations where state is either Succeeded, Failed, Cancelled </param>
        /// <param name="allOps"></param>
        /// <returns></returns>
        private static HashSet<string?> ExcludeCompletedOperations(Dictionary<string, ResourceOperationDetails> completedOps, HashSet<string?> allOps)
        {
            var originalOps = new HashSet<string?>(allOps);

            foreach (var op in allOps)
            {
                if (op != null && completedOps.ContainsKey(op))
                {
                    originalOps.Remove(op);
                }
            }
            Console.WriteLine(string.Join(", ", originalOps));
            return originalOps;
        }


        /// <summary>
        /// Polls the operation status for the operations that are in not yet in completed state
        /// </summary>
        /// <param name="cts"></param>
        /// <param name="opIdsFromOperationReq"> OperationIds from submit/execute type operations </param>
        /// <param name="completedOps"> OperationIds of completed operations </param>
        /// <param name="location"> Location of the virtual machines from submit/execute type operations </param>
        /// <param name="resource"> ARM subscription resource </param>
        /// <returns></returns>

        public static async Task PollOperationStatus(CancellationTokenSource cts, HashSet<string?> opIdsFromOperationReq, Dictionary<string, ResourceOperationDetails> completedOps, string location, SubscriptionResource resource)
        {
            Random random = new();

            // This value can be set to 30s since p50 for virtual machine operations in Azure is around 30 seconds
            await Task.Delay(InitialWaitTimeBeforePollingInMilliseconds);

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    GetOperationStatusContent getOpsStatusRequest = new(opIdsFromOperationReq, Guid.NewGuid().ToString());
                    var response = await ScheduledActionsOperations.TestGetOpsStatusAsync(location, getOpsStatusRequest, resource);

                    if (!ShouldRetryPolling(response, opIdsFromOperationReq.Count, completedOps))
                    {
                        break;
                    }
                    else
                    {
                        var excludedOps = ExcludeCompletedOperations(completedOps, opIdsFromOperationReq);
                        GetOperationStatusContent pendingOpIds = new(excludedOps, Guid.NewGuid().ToString());
                        response = await ScheduledActionsOperations.TestGetOpsStatusAsync(location, pendingOpIds, resource);
                    }

                    int pollingIntervalInSeconds = random.Next(PollintIntervalLowerBoundInSeconds, PollintIntervalLowerBoundInSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(pollingIntervalInSeconds), cts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with ErrorCode:{ex} and ErrorMessage: {ex.Message}");
                throw;
            }
        }
    }
}
