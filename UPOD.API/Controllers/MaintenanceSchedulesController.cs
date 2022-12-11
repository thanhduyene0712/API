using Firebase.Auth;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using UPOD.API.HubService;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.SERVICES.Enum;
using UPOD.SERVICES.Services;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/maintenance_schedules")]
    public partial class MaintenanceSchedulesController : ControllerBase
    {

        private readonly IMaintenanceScheduleService _maintenanceSchedule_sv;
        private readonly IMaintenanceReportService _maintenanceReport_sv;
        private readonly IRequestService _request_Sv;
        private readonly IContractServiceService _contract_sv;
        public MaintenanceSchedulesController(IMaintenanceScheduleService maintenanceSchedule_sv
            , IContractServiceService contract_sv
            , IMaintenanceReportService maintenanceReport_sv
            , IRequestService request_Sv)
        {
            _maintenanceSchedule_sv = maintenanceSchedule_sv;
            _contract_sv = contract_sv;
            _maintenanceReport_sv = maintenanceReport_sv;
            _request_Sv = request_Sv;
        }

        [HttpPut]
        [Route("notifications")]
        public async Task<ActionResult> Notifications()
        {
            try
            {

                await _maintenanceSchedule_sv.SetMaintenanceSchedulesNotify();
                await _maintenanceSchedule_sv.SetMaintenanceSchedulesNotifyWarning();
                await _maintenanceSchedule_sv.SetMaintenanceSchedulesNotifyMissing();
                await _contract_sv.SetContractNotify();
                await _request_Sv.WarningRequest();
                await _request_Sv.CompletedRequest();
                await _maintenanceReport_sv.CheckMaintenanceReport();
                var timeShedule = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(5), DateTimeKind.Utc);
                BackgroundJob.Schedule(() => Notifications(), timeShedule);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get_list_maintenance_schedules")]
        public async Task<ActionResult<ResponseModel<MaintenanceScheduleResponse>>> GetListMaintenanceSchedules([FromQuery] PaginationRequest model, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _maintenanceSchedule_sv.GetListMaintenanceSchedules(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_maintenance_schedules_by_customer_id")]
        public async Task<ActionResult<ResponseModel<MaintenanceScheduleResponse>>> GetListMaintenanceSchedulesByCustomer(Guid id, [FromQuery] PaginationRequest model, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _maintenanceSchedule_sv.GetListMaintenanceSchedulesByCustomer(id, model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_details_maintenance_schedule")]
        public async Task<ActionResult<ObjectModelResponse>> MaintenanceScheduleDetails(Guid id)
        {
            try
            {
                return await _maintenanceSchedule_sv.MaintenanceScheduleDetails(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_technician_maintenance_schedule_by_id")]
        public async Task<ActionResult<ResponseModel<TechnicianOfRequestResponse>>> GetTechnicianSchedule(Guid id)
        {
            try
            {
                return await _maintenanceSchedule_sv.GetTechnicianSchedule(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_maintenance_schedules_by_technician_id")]
        public async Task<ActionResult<ResponseModel<MaintenanceScheduleResponse>>> GetListMaintenanceSchedulesTechnician([FromQuery] PaginationRequest model, Guid id, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _maintenanceSchedule_sv.GetListMaintenanceSchedulesTechnician(model, id, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_maintenance_schedules_by_agency_id")]
        public async Task<ActionResult<ResponseModel<MaintenanceScheduleResponse>>> GetListMaintenanceSchedulesAgency([FromQuery] PaginationRequest model, Guid id, [FromQuery] FilterStatusRequest value)
        {
            try
            {
                return await _maintenanceSchedule_sv.GetListMaintenanceSchedulesAgency(model, id, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("maintaining_maintenance_schedule_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> MaintainingSchedule(Guid id, Guid tech_id)

        {
            try
            {
                return await _maintenanceSchedule_sv.MaintainingSchedule(id, tech_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("update_maintenance_schedule_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateMaintenanceSchedule(Guid id, MaintenanceScheduleRequest model)
        {
            try
            {
                return await _maintenanceSchedule_sv.UpdateMaintenanceSchedule(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("accept_maintenance_schedule_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> AcceptMaintenanceSchedule(Guid id, Guid tech_id)
        {
            try
            {
                return await _maintenanceSchedule_sv.AcceptMaintenanceSchedule(id, tech_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("disable_maintenance_schedule_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableMaintenanceSchedule(Guid id)
        {
            try
            {
                return await _maintenanceSchedule_sv.DisableMaintenanceSchedule(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
