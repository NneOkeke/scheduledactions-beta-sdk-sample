using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule.Models;
using Azure.ResourceManager.Resources;

namespace ComputeScheduleSampleProject
{
    public static class UtilityMethods
    {
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
        public static Task<bool> ShouldRetryPolling(GetOperationStatusResult response, int totalVmsCount, Dictionary<string, ResourceOperationDetails> completedOps)
        {
            var shouldRetry = true;
            completedOps.Clear();

            IReadOnlyList<ResourceOperationResult> results = response.Results;

            foreach (ResourceOperationResult item in results)
            {
                // what does this mean?
                if (item.ErrorCode != null)
                {
                    completedOps.Add(item.Operation.OperationId, item.Operation);
                    Console.WriteLine($"Operation {item.Operation.OperationId} failed with error code {item.Operation.ResourceOperationError.ErrorCode}");
                }
                else
                {
                    if (UtilityMethods.IsOperationStateComplete(item.Operation.State))
                    {
                        completedOps.Add(item.Operation.OperationId, item.Operation);
                        Console.WriteLine($"Operation {item.Operation.OperationId} completed with state {item.Operation.State}");
                    }
                }
            }

            if (completedOps.Count == totalVmsCount)
            {
                shouldRetry = false;
            }
            return Task.FromResult(shouldRetry);
        }
    }
}
