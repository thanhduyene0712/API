﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.SERVICES.Services;
using IAccountService = UPOD.SERVICES.Services.IAccountService;

namespace UPOD.API.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    public partial class AccountsController : ControllerBase
    {
        private readonly IAccountService _account_sv;
        private readonly IUserAccessor _userAccessor;

        public AccountsController(IAccountService account_sv, IUserAccessor userAccessor)
        {
            _account_sv = account_sv;
            _userAccessor = userAccessor;
        }
        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        public async Task<ActionResult<ObjectModelResponse>> Login(LoginRequest model)
        {
            try
            {
                return await _account_sv.Login(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_all_accounts")]
        public async Task<ActionResult<ResponseModel<AccountResponse>>> GetAllAccounts([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            //var accountRole = _userAccessor.GetRoleId();
            //if (accountRole != Guid.Parse("dd3cb3b4-84fe-432e-bb06-2d8aecaa640d"))
            //{
            //    return BadRequest("Don't have permission!");
            //}
            try
            {
                return await _account_sv.GetAll(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_all_accounts_is_not_assign")]
        public async Task<ActionResult<ResponseModel<AccountResponse>>> GetAllAccountIsNotAssign([FromQuery] PaginationRequest model, [FromQuery] SearchRequest value)
        {
            //var accountRole = _userAccessor.GetRoleId();
            //if (accountRole != Guid.Parse("dd3cb3b4-84fe-432e-bb06-2d8aecaa640d"))
            //{
            //    return BadRequest("Don't have permission!");
            //}
            try
            {
                return await _account_sv.GetAllAccountIsNotAssign(model, value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("get_all_roles")]
        public async Task<ActionResult<ResponseModel<RoleResponse>>> GetAllRoles([FromQuery] PaginationRequest model)
        {
            try
            {
                return await _account_sv.GetAllRoles(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("search_accounts_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> GetAccountDetails(Guid id)
        {
            try
            {
                return await _account_sv.GetAccountDetails(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("create_account")]
        public async Task<ActionResult<ObjectModelResponse>> CreateAccount(AccountCreateRequest model)
        {
            try
            {
                return await _account_sv.CreateAccount(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_account_admin")]
        public async Task<ActionResult<ObjectModelResponse>> CreateAccountAdmin(AccountRequest model)
        {
            try
            {
                return await _account_sv.CreateAccountAdmin(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_account_technician")]
        public async Task<ActionResult<ObjectModelResponse>> CreateAccountTechnician(AccountRequest model)
        {
            try
            {
                return await _account_sv.CreateAccountTechnician(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("create_account_customer")]
        public async Task<ActionResult<ObjectModelResponse>> CreateAccountCustomer(AccountRequest model)
        {
            try
            {
                return await _account_sv.CreateAccountCustomer(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("update_account_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateAccount(Guid id, AccountUpdateRequest model)
        {
            try
            {
                return await _account_sv.UpdateAccount(id, model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("change_password_by_account_id")]
        public async Task<ActionResult<ObjectModelResponse>> ChangePassword(ChangePasswordRequest model, Guid id)
        {
            try
            {
                return await _account_sv.ChangePassword(model, id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPut]
        [Route("disable_account_by_id")]
        public async Task<ActionResult<ObjectModelResponse>> DisableAccount(Guid id)
        {
            try
            {
                return await _account_sv.DisableAccount(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
