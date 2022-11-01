﻿using Microsoft.AspNetCore.Mvc;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using IAgencyService = UPOD.SERVICES.Services.IAgencyService;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/agencies")]
    public partial class AgenciesController : ControllerBase
    {

        private readonly IAgencyService _agency_sv;
        public AgenciesController(IAgencyService agency_sv)
        {
            _agency_sv = agency_sv;
        }

        [HttpGet]
        [Route("get_list_agencies")]
        public async Task<ActionResult<ResponseModel<AgencyResponse>>> GetListAgencies([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            try
            {
                return await _agency_sv.GetListAgencies(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_list_agencies_by_technician_id")]
        public async Task<ActionResult<ResponseModel<AgencyResponse>>> GetListAgenciesByTechnician([FromQuery] PaginationRequest model, Guid id, [FromQuery] SearchRequest value)

        {
            try
            {
                return await _agency_sv.GetListAgenciesByTechnician(model, id, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_technicians_by_agency_id")]
        public async Task<ActionResult<ResponseModel<TechnicianViewResponse>>> GetTechnicianByAgencyId([FromQuery] PaginationRequest model, [FromQuery] Guid id)
        {
            try
            {
                return await _agency_sv.GetTechnicianByAgencyId(model, id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get_agency_details")]
        public async Task<ActionResult<ObjectModelResponse>> GetDetailsAgency(Guid id)
        {
            try
            {
                return await _agency_sv.GetDetailsAgency(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_agency")]
        public async Task<ActionResult<ObjectModelResponse>> CreateAgency(AgencyRequest model)
        {
            try
            {
                return await _agency_sv.CreateAgency(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("update_agency_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateAgency(Guid id, AgencyUpdateRequest model)
        {
            try
            {
                return await _agency_sv.UpdateAgency(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("disable_agency_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableAgency(Guid id)
        {
            try
            {
                return await _agency_sv.DisableAgency(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
