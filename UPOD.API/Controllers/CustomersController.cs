using Microsoft.AspNetCore.Mvc;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using UPOD.SERVICES.Services;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public partial class CustomersController : ControllerBase
    {

        private readonly ICustomerService _customer_sv;
        public CustomersController(ICustomerService customer_sv)
        {
            _customer_sv = customer_sv;
        }

        [HttpGet]
        [Route("get_all_customers")]
        public async Task<ActionResult<ResponseModel<CustomerResponse>>> GetAllCustomers([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            try
            {
                return await _customer_sv.GetAll(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_customer_details_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> GetCustomerDetails(Guid id)
        {
            try
            {
                return await _customer_sv.GetCustomerDetails(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_agencies_by_customer_id")]
        public async Task<ActionResult<ResponseModel<AgencyOfCustomerResponse>>> GetAgenciesByCustomerId(Guid id, [FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            try
            {
                return await _customer_sv.GetAgenciesByCustomerId(id, model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_contracts_by_customer_id")]
        public async Task<ActionResult<ResponseModel<ContractResponse>>> GetAllContractByCustomer([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value, Guid id)
        {
            try
            {
                return await _customer_sv.GetAllContractByCustomer(model,value, id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpGet]
        [Route("get_requests_by_customer_id")]
        public async Task<ActionResult<ResponseModel<RequestResponse>>> GetListRequestsByCustomerId([FromQuery] PaginationRequest model, [FromQuery] FilterStatusRequest value, Guid id)
        {
            try
            {
                return await _customer_sv.GetListRequestsByCustomerId(model, value, id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get_services_by_customer_id")]
        public async Task<ActionResult<ResponseModel<ServiceResponse>>> GetServiceByCustomerId(Guid id)
        {
            try
            {
                return await _customer_sv.GetServiceByCustomerId(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_services_not_in_contract_customer")]
        public async Task<ActionResult<ResponseModel<ServiceNotInContractViewResponse>>> GetServiceNotInContractCustomerId(Guid id)
        {
            try
            {
                return await _customer_sv.GetServiceNotInContractCustomerId(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("create_customer")]
        public async Task<ActionResult<ObjectModelResponse>> CreateCustomer(CustomerRequest model)
        {
            try
            {
                return await _customer_sv.CreateCustomer(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
       
        [HttpPut]
        [Route("update_customer_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateCustomer(Guid id, CustomerUpdateRequest model)
        {
            try
            {
                return await _customer_sv.UpdateCustomer(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("approve_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> ApproveRequestResolved(Guid id)
        {
            try
            {
                return await _customer_sv.ApproveRequestResolved(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }      
        [HttpPut]
        [Route("reject_contract_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> RecjectContract(Guid cus_id, Guid con_id, ContractRejectRequest model)
        {
            try
            {
                return await _customer_sv.RejectContract(cus_id, con_id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
       
        [HttpPut]
        [Route("approve_contract_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> ApproveContract(Guid cus_id, Guid con_id)
        {
            try
            {
                return await _customer_sv.ApproveContract(cus_id, con_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("disable_customer_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableCompany(Guid id)
        {
            try
            {
                return await _customer_sv.DisableCustomer(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("approve_maintenance_report_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> ApproveMaintenanceReport(Guid id)
        {
            try
            {
                return await _customer_sv.ApproveMaintenanceReport(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
