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
            // SubscriptionId: The subscription id under which the virtual machines are located, in this case, we are using a dummy subscriptionId
            const string subscriptionId = "d93f78f2-e878-40c2-9d5d-dcfdbb8042a0";

            Dictionary<string, ResourceOperationDetails> completedOperations = [];
            // Credential: The Azure credential used to authenticate the request
            TokenCredential cred = new DefaultAzureCredential();

            // Client: The Azure Resource Manager client used to interact with the Azure Resource Manager API
            ArmClient client = new(cred);
            var subscriptionResource = UtilityMethods.GetSubscriptionResource(client, subscriptionId);

            // Execution parameters for the request including the retry policy used by Scheduledactions to retry the operation in case of failures
            var executionParams = new ScheduledActionExecutionParameterDetail()
            {
                RetryPolicy = new UserRequestRetryPolicy()
                {
                    // Number of times ScheduledActions should retry the operation in case of failures: Range 0-7
                    RetryCount = 3,
                    // Time window in minutes within which ScheduledActions should retry the operation in case of failures: Range in minutes 5-120
                    RetryWindowInMinutes = 45
                }
            };

            // Execute type operation: Start operation on virtual machines
            await ScheduledActions_ExecuteTypeOperation(completedOperations, executionParams, subscriptionResource, subscriptionId);
        }

        /// <summary>
        /// This method details the happy path for executing an execute type operation in ScheduledActions
        /// </summary>
        /// <param name="completedOperations"></param>
        /// <param name="retryPolicy"></param>
        /// <param name="subscriptionResource"></param>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        private static async Task ScheduledActions_ExecuteTypeOperation(Dictionary<string, ResourceOperationDetails> completedOperations, ScheduledActionExecutionParameterDetail retryPolicy, SubscriptionResource subscriptionResource, string subscriptionId)
        {
            var blockedOperationsException = new HashSet<string> { "SchedulingOperationsBlockedException", "NonSchedulingOperationsBlockedException" };

            // Location: The location of the virtual machines
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

                // The request body for the executestart operation on virtual machines
                var executeStartRequest = new ExecuteStartContent(retryPolicy, resources, correlationId);

                StartResourceOperationResult? result = await subscriptionResource.ExecuteVirtualMachineStartAsync(location, executeStartRequest);

                /// <summary>
                /// Each operationId corresponds to a virtual machine operation in ScheduledActions. 
                /// The method below excludes resources that have not been processed in ScheduledActions due to a number of reasons 
                /// like operation conflicts, virtual machines not being found in an Azure location etc 
                /// and returns only the valid operations that have passed validation checks to be polled.
                /// </summary>
                var validOperationIds = UtilityMethods.ExcludeResourcesNotProcessed(result.Results);
                completedOperations.Clear();

                if(validOperationIds.Count > 0)
                {
                    await UtilityMethods.PollOperationStatus(validOperationIds, completedOperations, location, subscriptionResource);
                }
                else
                {
                    Console.WriteLine("No valid operations to poll");
                    return;
                }
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
                /// COMPUTESCHEDULE BLOCKING ERRORS:
                /// - Scheduling Operations Blocked due to an ongoing outage in downstream services
                /// - Non-Scheduling Operations Blocked, eg VirtualMachinesGetOperationStatus operations, due to an ongoing outage in downstream services
                /// </summary>
                Console.WriteLine($"Request failed with ErrorCode:{ex.ErrorCode} and ErrorMessage: {ex.Message}");

                if(ex.ErrorCode != null && blockedOperationsException.Contains(ex.ErrorCode))
                {
                    /// Operation blocking on scheduling/non-scheduling actions can be due to scenarios like outages in downstream services.
                    Console.WriteLine($"Operation Blocking is turned on, request may succeed later.");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with Exception:{ex.Message}");
                throw;
            }
        }
    }
}