using Azure.ResourceManager.ComputeSchedule;
using Azure.ResourceManager.ComputeSchedule.Models;
using Azure.ResourceManager.Resources;
using System.ClientModel.Primitives;

namespace ComputeScheduleSampleProject
{
    public static class ScheduledActionsOperations
    {

        /// <summary>
        /// Execute Type Operation: Start a batch of virtual machines immediately
        /// </summary>
        /// <param name="location"></param>
        /// <param name="executeStartRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<StartResourceOperationResult> TestExecuteStartAsync(string location, ExecuteStartContent executeStartRequest, SubscriptionResource subscriptionResource)
        {
            StartResourceOperationResult? result;
            try
            {
                result = await subscriptionResource.ExecuteVirtualMachineStartAsync(location, executeStartRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
                return result;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }
        }

        /// <summary>
        /// Execute Type Operation: Deallocate a batch of virtual machines immediately
        /// </summary>
        /// <param name="location"></param>
        /// <param name="executeDeallocateRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<DeallocateResourceOperationResult> TestExecuteDeallocateAsync(string location, ExecuteDeallocateContent executeDeallocateRequest, SubscriptionResource subscriptionResource)
        {
            DeallocateResourceOperationResult? result;
            try
            {
                result = await subscriptionResource.ExecuteVirtualMachineDeallocateAsync(location, executeDeallocateRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;

        }

        /// <summary>
        /// Execute Type Operation: Hibernate a batch of virtual machines immediately
        /// </summary>
        /// <param name="location"></param>
        /// <param name="executeHibernateRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<HibernateResourceOperationResult> TestExecuteHibernateAsync(string location, ExecuteHibernateContent executeHibernateRequest, SubscriptionResource subscriptionResource)
        {
            HibernateResourceOperationResult? result;

            try
            {
                result = await subscriptionResource.ExecuteVirtualMachineHibernateAsync(location, executeHibernateRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Submit Type Operation: Start a batch of virtual machines at a later time, up to 14 days, in the future
        /// </summary>
        /// <param name="location"></param>
        /// <param name="submitStartRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<StartResourceOperationResult> TestSubmitStartAsync(string location, SubmitStartContent submitStartRequest, SubscriptionResource subscriptionResource)
        {
            StartResourceOperationResult? result;
            try
            {
                result = await subscriptionResource.SubmitVirtualMachineStartAsync(location, submitStartRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Submit Type Operation: Deallocate a batch of virtual machines at a later time, up to 14 days, in the future
        /// </summary>
        /// <param name="location"></param>
        /// <param name="submitDeallocateRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<DeallocateResourceOperationResult> TestSubmitDeallocateAsync(string location, SubmitDeallocateContent submitDeallocateRequest, SubscriptionResource subscriptionResource)
        {
            DeallocateResourceOperationResult? result;
            try
            {
                result = await subscriptionResource.SubmitVirtualMachineDeallocateAsync(location, submitDeallocateRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;

        }

        /// <summary>
        /// Submit Type Operation: Hibernate a batch of virtual machines at a later time, up to 14 days, in the future
        /// </summary>
        /// <param name="location"></param>
        /// <param name="submitHibernateRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<HibernateResourceOperationResult> TestSubmitHibernateAsync(string location, SubmitHibernateContent submitHibernateRequest, SubscriptionResource subscriptionResource)
        {
            HibernateResourceOperationResult? result;

            try
            {
                result = await subscriptionResource.SubmitVirtualMachineHibernateAsync(location, submitHibernateRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Cancel operations scheduled but not yet being performed on a batch of virtual machines, this is useful for cancelling operations performed with Submit-Type operations
        /// </summary>
        /// <param name="location"></param>
        /// <param name="cancelOpsRequest"></param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<CancelOperationsResult> TestCancelOpsAsync(string location, CancelOperationsContent cancelOpsRequest, SubscriptionResource subscriptionResource)
        {
            CancelOperationsResult? result;

            try
            {
                result = await subscriptionResource.CancelVirtualMachineOperationsAsync(location, cancelOpsRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Get the statuc of an operation on a virtual machine
        /// </summary>
        /// <param name="location"> The location of the virtual machines </param>
        /// <param name="getOpsStatusRequest"> The request body for GetVirtualMachineOperationStatusAsync </param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<GetOperationStatusResult> TestGetOpsStatusAsync(string location, GetOperationStatusContent getOpsStatusRequest, SubscriptionResource subscriptionResource)
        {
            GetOperationStatusResult? result;

            try
            {
                result = await subscriptionResource.GetVirtualMachineOperationStatusAsync(location, getOpsStatusRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Gets errors that occured during the lifetime of an operation on a virtual machine
        /// </summary>
        /// <param name="location"> The location of the virtual machines </param>
        /// <param name="getOpsErrorsRequest"> The request body for GetVirtualMachineOperationErrorsAsync </param>
        /// <param name="subscriptionResource"></param>
        /// <returns></returns>
        public static async Task<GetOperationErrorsResult> TestGetOpsErrorsAsync(string location, GetOperationErrorsContent getOpsErrorsRequest, SubscriptionResource subscriptionResource)
        {
            GetOperationErrorsResult? result;

            try
            {
                result = await subscriptionResource.GetVirtualMachineOperationErrorsAsync(location, getOpsErrorsRequest);
                Console.WriteLine(ModelReaderWriter.Write(result, ModelReaderWriterOptions.Json).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            return result;
        }
    }
}
