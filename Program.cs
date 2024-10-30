using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule.Models;

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

                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
                var operationIds = result.Results.Select(result => result.Operation?.OperationId).Where(operationId => !string.IsNullOrEmpty(operationId)).ToHashSet();

                completedOperations.Clear();
                await UtilityMethods.PollOperationStatus(cts, operationIds, completedOperations, location, subscriptionResource);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with ErrorCode:{ex} and ErrorMessage: {ex.Message}");
                throw;
            }
        }
    }
}