using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reso.Core.Utilities;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.SERVICES.Enum;
using UPOD.SERVICES.Services;
using IRequestService = UPOD.SERVICES.Services.IRequestService;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/requests")]
    public partial class RequestsController : ControllerBase
    {

        private readonly IRequestService _request_sv;
        public RequestsController(IRequestService request_sv)
        {
            _request_sv = request_sv;
        }

        [HttpGet]
        [Route("auto_fill_request_by_report_service_id")]
        public async Task<ActionResult<ObjectModelResponse>> AutoFillRequestAdmin(Guid id)
        {
            try
            {
                return await _request_sv.AutoFillRequestAdmin(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_requests")]
        public async Task<ActionResult<ResponseModel<RequestResponse>>> GetListRequests([FromQuery] PaginationRequest model, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _request_sv.GetListRequests(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_requests_of_agency")]
        public async Task<ActionResult<ResponseModel<RequestResponse>>> GetListRequestsOfAgency([FromQuery] PaginationRequest model, Guid id, [FromQuery] FilterStatusRequest status)
        {
            try
            {
                return await _request_sv.GetListRequestsOfAgency(model, id, status);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_request_details_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> GetDetailsRequest([FromQuery] Guid id)
        {
            try
            {
                return await _request_sv.GetDetailsRequest(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_technicians_by_id_request")]
        public async Task<ActionResult<ResponseModel<TechnicianOfRequestResponse>>> GetTechnicianRequest(Guid id)
        {
            try
            {
                return await _request_sv.GetTechnicianRequest(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_ticket_details_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> GetTicketDetails(Guid id)
        {
            try
            {
                return await _request_sv.GetTicketDetails(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_technicians_by_id_report_service")]
        public async Task<ActionResult<ResponseModel<TechnicianOfRequestResponse>>> GetTechnicianRequestAdmin(Guid agency_id, Guid service_id)
        {
            try
            {
                return await _request_sv.GetTechnicianRequestAdmin(agency_id, service_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_contract_by_customer_service")]
        public async Task<ActionResult<ObjectModelResponse>> GetContractByCustomerSerivce(Guid cus_id, Guid service_id)
        {
            try
            {
                return await _request_sv.GetContractByCustomerSerivce(cus_id, service_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_devices_by_id_request")]
        public async Task<ActionResult<ResponseModel<DeviceResponse>>> GetDeviceRequest([FromQuery] PaginationRequest model, Guid id)
        {
            try
            {
                return await _request_sv.GetDeviceRequest(model, id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("create_request")]
        public async Task<ActionResult<ObjectModelResponse>> CreateRequest(RequestRequest model)
        {

            try
            {
                return await _request_sv.CreateRequest(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_request_by_admin")]
        public async Task<ActionResult<ObjectModelResponse>> CreateRequestByAdmin(RequestAdminRequest model)
        {

            try
            {
                return await _request_sv.CreateRequestByAdmin(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("update_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateRequest([FromQuery] Guid id, RequestUpdateRequest model)
        {
            try
            {
                return await _request_sv.UpdateRequest(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("update_request_admin_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateRequestAdmin([FromQuery] Guid id, RequestUpdateAdminRequest model)
        {
            try
            {
                return await _request_sv.UpdateRequestAdmin(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("reject_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> RejectRequest(Guid id, [FromQuery] RejectRequest value)
        {
            try
            {
                return await _request_sv.RejectRequest(id, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("cancel_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> CancelRequest(Guid id, RejectRequest model)

        {
            try
            {
                return await _request_sv.CancelRequest(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("mapping_technician_to_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> MappingTechnicianRequest(Guid request_id, Guid technician_id)
        {
            try
            {
                return await _request_sv.MappingTechnicianRequest(request_id, technician_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("disable_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableRequest([FromQuery] Guid id)
        {
            try
            {
                return await _request_sv.DisableRequest(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
