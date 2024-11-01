﻿using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ComputeSchedule;
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
            // add comments on what these variables mean
            const string subscriptionId = "d93f78f2-e878-40c2-9d5d-dcfdbb8042a0";
            const int retryCount = 3;
            const int retryWindowInMinutes = 60;

            Dictionary<string, ResourceOperationDetails> completedOperations = [];
            TokenCredential cred = new DefaultAzureCredential();
            ArmClient client = new(cred);
            var subscriptionResource = UtilityMethods.GetSubscriptionResource(client, subscriptionId);

            // Execution parameters for the scheduled action including the retry policy used by Scheduledactions to retry the operation in case of failures
            var executionParams = new ScheduledActionExecutionParameterDetail()
            {
                RetryPolicy = new UserRequestRetryPolicy()
                {
                    // Number of times ScheduledActions should retry the operation in case of failures
                    RetryCount = retryCount,
                    // Time window in minutes within which ScheduledActions should retry the operation in case of failures
                    RetryWindowInMinutes = retryWindowInMinutes
                }
            };

            await ScheduledActions_ExecuteTypeOperation_HappyPath(completedOperations, executionParams, subscriptionResource, subscriptionId);
        }

        /// <summary>
        /// This method details the happy path for executing an execute type operation in ScheduledActions
        /// </summary>
        /// <param name="completedOperations"></param>
        /// <param name="retryPolicy"></param>
        /// <param name="subscriptionResource"></param>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        private static async Task ScheduledActions_ExecuteTypeOperation_HappyPath(Dictionary<string, ResourceOperationDetails> completedOperations, ScheduledActionExecutionParameterDetail retryPolicy, SubscriptionResource subscriptionResource, string subscriptionId)
        {
            const string location = "eastasia";

            // List of virtual machine resource identifiers to perform execute type operations on, in this case, we are using dummy VMs. Virtual Machines must all be under the same subscriptionid
            var resourceIds = new List<ResourceIdentifier>()
            {
                new($"/subscriptions/{subscriptionId}/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-600"),
                new($"/subscriptions/{subscriptionId}/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-611"),
                new($"/subscriptions/{subscriptionId}/resourceGroups/ScheduledActions_Baseline_EastAsia/providers/Microsoft.Compute/virtualMachines/dummy-vm-612"),
            };
            var resources = new UserRequestResources(resourceIds);

            try
            {
                // CorrelationId: This is a unique identifier used internally to track and monitor operations in ScheduledActions
                var correlationId = Guid.NewGuid().ToString();

                var executeStartRequest = new ExecuteStartContent(retryPolicy, resources, correlationId);

                StartResourceOperationResult? result = await subscriptionResource.ExecuteVirtualMachineStartAsync(location, executeStartRequest);
                /// <summary>
                /// Each operationId corresponds to a virtual machine operation in ScheduledActions. 
                /// This method excludes resources that have not been processed in ScheduledActions due to a number of reasons 
                /// like operation conflicts etc and passes only the valid operations that have passed validation checks to be polled.
                /// </summary>
                var validOperationIds = UtilityMethods.ExcludeResourcesNotProcessed(result.Results);

                completedOperations.Clear();
                await UtilityMethods.PollOperationStatus(validOperationIds, completedOperations, location, subscriptionResource);
            }
            catch (RequestFailedException ex)
            {
                /// <summary>
                /// Request examples that could make a request fall into this catch block include:
                /// VALIDATION ERRORS:
                /// - No resourceids provided in request
                /// - Over 100 resourceids provided in request
                /// - RetryPolicy.RetryCount value > 7
                /// - RetryPolicy.RetryWindowInMinutes value > 120
                /// </summary>
                
                Console.WriteLine($"Request failed with ErrorCode:{ex.ErrorCode} and ErrorMessage: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with ErrorCode:{ex} and ErrorMessage: {ex.Message}");
                throw;
            }
        }
    }
}