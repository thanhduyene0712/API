using Microsoft.AspNetCore.Mvc;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using ITechnicianService = UPOD.SERVICES.Services.ITechnicianService;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/technicians")]
    public partial class TechniciansController : ControllerBase
    {

        private readonly ITechnicianService _technician_sv;
        public TechniciansController(ITechnicianService technician_sv)
        {
            _technician_sv = technician_sv;
        }

        [HttpGet]
        [Route("get_list_technicians")]
        public async Task<ActionResult<ResponseModel<TechnicianResponse>>> GetListTechnicians([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            try
            {
                return await _technician_sv.GetListTechnicians(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get_technician_details")]
        public async Task<ActionResult<ObjectModelResponse>> GetDetailsTechnician(Guid id)
        {
            try
            {
                return await _technician_sv.GetDetailsTechnician(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_requests_by_id_technician")]
        public async Task<ActionResult<ResponseModel<RequestResponse>>> GetListRequestsOfTechnician([FromQuery] PaginationRequest model, Guid id, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _technician_sv.GetListRequestsOfTechnician(model, id, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_requests_by_id_technician_agency")]
        public async Task<ActionResult<ResponseModel<RequestResponse>>> GetListRequestsOfTechnicianAgency([FromQuery] PaginationRequest model, Guid tech_id, Guid agency_id, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _technician_sv.GetListRequestsOfTechnicianAgency(model, tech_id, agency_id, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_ticket_by_request_id")]
        public async Task<ActionResult<ResponseModel<DevicesOfRequestResponse>>> GetDevicesByRequest([FromQuery] PaginationRequest model, [FromQuery] Guid id)
        {
            try
            {
                return await _technician_sv.GetDevicesByRequest(model, id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_task_by_technician_id")]
        public async Task<ActionResult<ResponseModel<TaskResponse>>> GetTask([FromQuery] PaginationRequest model, Guid id,int task, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _technician_sv.GetTask(model, id, task, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_technician")]
        public async Task<ActionResult<ObjectModelResponse>> CreateTechnician(TechnicianRequest model)
        {
            try
            {
                return await _technician_sv.CreateTechnician(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("create_ticket_by_id_request")]
        public async Task<ActionResult<ResponseModel<DevicesOfRequestResponse>>> CreateTicket(Guid id, Guid tech_id, ListTicketRequest model)
        {
            try
            {
                return await _technician_sv.CreateTicket(id, tech_id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("update_device_of_ticket_by_request_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateDeviceTicket(Guid id, ListTicketRequest model)
        {
            try
            {
                return await _technician_sv.UpdateDeviceTicket(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("disable_device_of_ticket_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableDeviceOfTicket(Guid id)
        {
            try
            {
                return await _technician_sv.DisableDeviceOfTicket(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("resolving_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> ResolvingRequest(Guid id, Guid tech_id)
        {
            try
            {
                return await _technician_sv.ResolvingRequest(id, tech_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("technician_reject_request_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> RejectRequest(Guid id, Guid tech_id)
        {
            try
            {
                return await _technician_sv.RejectRequest(id, tech_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("reset_break_technician")]
        public async Task<ActionResult<ResponseModel<Technician>>> ResetBreachTechnician()
        {
            try
            {
                return await _technician_sv.ResetBreachTechnician();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("update_technician_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateTechnician(Guid id, TechnicianUpdateRequest model)
        {
            try
            {
                return await _technician_sv.UpdateTechnician(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("is_busy_technician_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> IsBusyTechnician(Guid id, IsBusyRequest model)
        {
            try
            {
                return await _technician_sv.IsBusyTechnician(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("disable_technician_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableTechnician(Guid id)
        {
            try
            {
                return await _technician_sv.DisableTechnician(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
