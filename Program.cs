using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule.Models;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace ComputeScheduleSampleProject
{
    public static class Program
    {
        /// <summary>
        /// This project shows a sample use case for the ComputeSchedule SDK
        /// </summary>
        public static async Task Main(string[] args)
        {
            // Setup
            const string subId = "afe495ca-b99a-4e36-86c8-9e0e41697f1c";
            const string location = "eastasia";
            const int retryCount = 3;
            const int retryWindowInMinutes = 60;

            const int pollingRetryCount = 5;
            const int pollingRetryWindowInMinutes = 3;

            Dictionary<string, ResourceOperationDetails> completedOperations = [];
            TokenCredential cred = new DefaultAzureCredential();
            ArmClient client = new(cred);
            var subscriptionResource = UtilityMethods.GetSubscriptionResource(client, subId);

            // List of virtual machine resource identifiers to perform submit/execute type operations on
            var resourceIds = new List<ResourceIdentifier>()
            {
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-600"),
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-611"),
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-612"),
            };
            var resources = new UserRequestResources(resourceIds);

            // Execution parameters for the scheduled action including the retry policy used by Scheduledactions to retry the operation in case of failures
            var executionParams = new ScheduledActionExecutionParameterDetail()
            {
                RetryPolicy = new UserRequestRetryPolicy()
                {
                    RetryCount = retryCount,
                    RetryWindowInMinutes = retryWindowInMinutes
                }
            };

            try
            {
                // Testing the ExecuteStart operation
                var executeStartRequest = new ExecuteStartContent(executionParams, resources, Guid.NewGuid().ToString());
                var result = await ScheduledActionsOperations.TestExecuteStartAsync(location, executeStartRequest, subscriptionResource);

                GetOperationStatusResult getOperationStatus = await PollOperationStatus(resourceIds.Count, completedOperations).ExecuteAsync(async () =>
                {
                    // OperationIds: Each virtual machine operation is assigned a unique operationId that can be used for tracking the status of the operation, canceling the operation, or getting the errors that might have existed during the lifetime of the operation
                    var operationIdsFromScheduledActionsOperation = result.Results.Select(result => result.Operation?.OperationId).Where(operationId => !string.IsNullOrEmpty(operationId)).ToHashSet();

                    // PendingOperationIds: OperationIds for operations that are not yet in a terminal state, that is, operations that are not yet in Succeeded, Failed, or Cancelled state
                    var pendingOperationIds = ExcludeCompletedOperations(completedOperations, operationIdsFromScheduledActionsOperation);

                    var getOpsStatusRequest = new GetOperationStatusContent(pendingOperationIds, Guid.NewGuid().ToString());

                    return await ScheduledActionsOperations.TestGetOpsStatusAsync(location, getOpsStatusRequest, subscriptionResource);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with ErrorCode:{ex} and ErrorMessage: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// add more documentation here
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
        /// Add documentation here for what this method does
        /// </summary>
        /// <param name="vmCount"></param>
        /// <param name="completedOps"></param>
        /// <returns></returns>
        private static Polly.Retry.AsyncRetryPolicy<GetOperationStatusResult> PollOperationStatus(int vmCount, Dictionary<string, ResourceOperationDetails> completedOps)
        {
            int pollyRetryCount = 5;
            var maxDelay = TimeSpan.FromSeconds(20);

            IEnumerable<TimeSpan> delay = Backoff.ExponentialBackoff(initialDelay: TimeSpan.FromSeconds(5), retryCount: pollyRetryCount).Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, maxDelay.Ticks)));

            return Policy
                .HandleResult<GetOperationStatusResult>(r => UtilityMethods.ShouldRetryPolling(r, vmCount, completedOps).GetAwaiter().GetResult())
                .WaitAndRetryAsync(delay);
        }
    }
}