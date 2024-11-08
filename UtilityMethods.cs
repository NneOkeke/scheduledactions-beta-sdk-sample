using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule;
using Azure.ResourceManager.ComputeSchedule.Models;
using Azure.ResourceManager.Resources;

namespace ComputeScheduleSampleProject
{
    public static class UtilityMethods
    {
        // Amount of time to wait between each polling request
        private static readonly int PollingIntervalInSeconds = 15;

        // Amount of time to wait before polling requests start, this is because the p50 for compute operations is approximately 2 minutes
        private static readonly int InitialWaitTimeBeforePollingInSeconds = 30;

        // Timeout for polling operation status
        private static readonly int OperationTimeoutInSeconds = 180;

        /// <summary>
        /// Generates a resource identifier for the subscriptionId
        /// </summary>
        /// <param name="client"></param>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public static SubscriptionResource GetSubscriptionResource(ArmClient client, string subscriptionId)
        {
            ResourceIdentifier subscriptionResourceId = SubscriptionResource.CreateResourceIdentifier(subscriptionId);
            return client.GetSubscriptionResource(subscriptionResourceId);
        }


        /// <summary>
        /// Determine if the operation state is complete
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsOperationStateComplete(ScheduledActionOperationState? state)
        {
            return state != null &&
                (state == ScheduledActionOperationState.Succeeded |
                state == ScheduledActionOperationState.Failed ||
                state == ScheduledActionOperationState.Cancelled);
        }

        /// <summary>
        /// Determine if polling for operation status should continue based on the response from GetOperationsRequest
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
                var operation = operationResult.Operation;
                var operationId = operation.OperationId;
                var operationState = operation.State;
                var operationError = operation.ResourceOperationError;

                if (IsOperationStateComplete(operationState))
                {
                    completedOps.TryAdd(operationId, operation);
                    Console.WriteLine($"Operation {operationId} completed with state {operationState}");

                    if (operationError != null)
                    {
                        Console.WriteLine($"Operation {operationId} encountered the following error: errorCode {operationError.ErrorCode}, errorDetails: {operationError.ErrorDetails}");
                    }
                }
            }

            // CompletedOps.Count == TotalVmsCount means that all the operations have completed and there would be no need to retry polling
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
            var incompleteOps = new HashSet<string?>(allOps);

            foreach (var op in allOps)
            {
                if (op != null && completedOps.ContainsKey(op))
                {
                    incompleteOps.Remove(op);
                }
            }
            Console.WriteLine(string.Join(", ", incompleteOps));
            return incompleteOps;
        }

        /// <summary>
        /// This method excludes resources not processed in Scheduledactions due to a number of reasons like operation conflicts etc.
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public static HashSet<string?> ExcludeResourcesNotProcessed(IEnumerable<ResourceOperationResult> results)
        {
            var validOperationIds = new HashSet<string?>();
            foreach (var result in results)
            {
                if (result.ErrorCode != null)
                {
                    Console.WriteLine($"VM with resourceId: {result.ResourceId} encountered the following error: errorCode {result.ErrorCode}, errorDetails: {result.ErrorDetails}");
                }
                else
                {
                    validOperationIds.Add(result.Operation.OperationId);
                }
            }
            return validOperationIds;
        }

        /// <summary>
        /// Polls the operation status for the operations that are in not yet in completed state
        /// </summary>
        /// <param name="cts"></param>
        /// <param name="opIdsFromOperationReq"> OperationIds from execute type operations </param>
        /// <param name="completedOps"> OperationIds of completed operations </param>
        /// <param name="location"> Location of the virtual machines from execute type operations </param>
        /// <param name="resource"> ARM subscription resource </param>
        /// <returns></returns>
 
        public static async Task PollOperationStatus(HashSet<string?> opIdsFromOperationReq, Dictionary<string, ResourceOperationDetails> completedOps, string location, SubscriptionResource resource)
        {
            await Task.Delay(InitialWaitTimeBeforePollingInSeconds);

            GetOperationStatusContent getOpsStatusRequest = new(opIdsFromOperationReq, Guid.NewGuid().ToString());
            GetOperationStatusResult? response = await resource.GetVirtualMachineOperationStatusAsync(location, getOpsStatusRequest);

            // Cancellation token source is used in this case to cancel the polling operation after a certain time
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(OperationTimeoutInSeconds));
            while (!cts.Token.IsCancellationRequested)
            {

                if (!ShouldRetryPolling(response, opIdsFromOperationReq.Count, completedOps))
                {
                    break;
                }
                else
                {
                    var incompleteOperations = ExcludeCompletedOperations(completedOps, opIdsFromOperationReq);
                    GetOperationStatusContent pendingOpIds = new(incompleteOperations, Guid.NewGuid().ToString());
                    response = await resource.GetVirtualMachineOperationStatusAsync(location, pendingOpIds);
                }

                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalInSeconds), cts.Token);
            }
        }
    }
}
