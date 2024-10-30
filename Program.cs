using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule.Models;
using Azure.ResourceManager.Resources;

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
            const int retryCount = 3;
            const int retryWindowInMinutes = 60;
            const int operationTimeoutInMinutes = 3;

            Dictionary<string, ResourceOperationDetails> completedOperations = [];
            TokenCredential cred = new DefaultAzureCredential();
            ArmClient client = new(cred);
            var subscriptionResource = UtilityMethods.GetSubscriptionResource(client, subId);

            // Execution parameters for the scheduled action including the retry policy used by Scheduledactions to retry the operation in case of failures
            var executionParams = new ScheduledActionExecutionParameterDetail()
            {
                RetryPolicy = new UserRequestRetryPolicy()
                {
                    RetryCount = retryCount,
                    RetryWindowInMinutes = retryWindowInMinutes
                }
            };

            await ScheduledActions_ExecuteTypeOperation_ValidationFailed_NoResourceProvided(completedOperations, executionParams, subscriptionResource, operationTimeoutInMinutes);
        }

        private static async Task ScheduledActions_ExecuteTypeOperation_ValidationFailed_NoResourceProvided(Dictionary<string, ResourceOperationDetails> completedOperations, ScheduledActionExecutionParameterDetail retryPolicy, SubscriptionResource subscriptionResource, int operationTimeoutInMinutes)
        {
            const string location = "eastasia";

            // List of virtual machine resource identifiers to perform submit/execute type operations on
            var resources = new UserRequestResources([]);

            try
            {
                var executeStartRequest = new ExecuteStartContent(retryPolicy, resources, Guid.NewGuid().ToString());
                var result = await ScheduledActionsOperations.TestExecuteStartAsync(location, executeStartRequest, subscriptionResource);

                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(operationTimeoutInMinutes));

                // Every virtual machine operation in scheduledactions is identified by a unique operationId
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

        private static async Task ScheduledActions_ExecuteTypeOperation_HappyPath(Dictionary<string, ResourceOperationDetails> completedOperations, ScheduledActionExecutionParameterDetail retryPolicy, SubscriptionResource subscriptionResource, int operationTimeoutInMinutes)
        {
            const string location = "eastasia";

            // List of virtual machine resource identifiers to perform submit/execute type operations on, in this case, we are using dummy VMs
            var resourceIds = new List<ResourceIdentifier>()
            {
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-600"),
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-611"),
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-612"),
            };
            var resources = new UserRequestResources(resourceIds);

            try
            {
                var executeStartRequest = new ExecuteStartContent(retryPolicy, resources, Guid.NewGuid().ToString());
                var result = await ScheduledActionsOperations.TestExecuteStartAsync(location, executeStartRequest, subscriptionResource);

                using CancellationTokenSource cts = new(TimeSpan.FromMinutes(operationTimeoutInMinutes));

                // Every virtual machine operation in scheduledactions is identified by a unique operationId
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

        private static async Task ScheduledActions_SubmitTypeOperation_HappyPath(ScheduledActionExecutionParameterDetail retryPolicy, SubscriptionResource subscriptionResource, int operationTimeoutInMinutes)
        {
            const string location = "eastasia";
            var timeNow = DateTime.UtcNow;
            var scheduleTime = timeNow.AddHours(10);

            // List of virtual machine resource identifiers to perform submit/execute type operations on, in this case, we are using dummy VMs
            var resourceIds = new List<ResourceIdentifier>()
            {
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-600"),
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-611"),
                new("/subscriptions/afe495ca-b99a-4e36-86c8-9e0e41697f1c/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-612"),
            };
            var resources = new UserRequestResources(resourceIds);

            var schedule = new UserRequestSchedule(scheduleTime, "UTC", ScheduledActionDeadlineType.InitiateAt);


            try
            {
                var submitStartRequest = new SubmitStartContent(schedule, retryPolicy, resources, Guid.NewGuid().ToString());
                var result = await ScheduledActionsOperations.TestSubmitStartAsync(location, submitStartRequest, subscriptionResource);

                // Polling can be added here to check the status of the operation at a time that is closer to the scheduled time
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with ErrorCode:{ex} and ErrorMessage: {ex.Message}");
                throw;
            }
        }
    }
}