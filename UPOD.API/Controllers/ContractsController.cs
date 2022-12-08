using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reso.Core.Utilities;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using IContractServiceService = UPOD.SERVICES.Services.IContractServiceService;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/contracts")]
    public partial class ContractsController : ControllerBase
    {

        private readonly IContractServiceService _contract_service_sv;
        public ContractsController(IContractServiceService contract_service_sv)
        {
            _contract_service_sv = contract_service_sv;
        }

        [HttpGet]
        [Route("get_all_contracts")]
        public async Task<ActionResult<ResponseModel<ContractResponse>>> GetAllContracts([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            try
            {
                return await _contract_service_sv.GetAll(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("get_service_contract_by_id")]
        public async Task<ActionResult<ResponseModel<ServiceResponse>>> GetServiceByContractId(Guid Id)
        {
            try
            {
                return await _contract_service_sv.GetServiceByContractId(Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_contract_details_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> GetDetailsContract([FromQuery] Guid Id)
        {
            try
            {
                return await _contract_service_sv.GetDetailsContract(Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_contract")]
        public async Task<ActionResult<ObjectModelResponse>> CreateContract(ContractRequest model)
        {
            try
            {
                return await _contract_service_sv.CreateContract(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("terminal_contract_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> TerminationContract(Guid id, ContractTermanationRequest model)
        {
            try
            {
                return await _contract_service_sv.TerminationContract(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPut]
        [Route("disable_contract_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableContract(Guid id)
        {
            try
            {
                return await _contract_service_sv.DisableContract(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
