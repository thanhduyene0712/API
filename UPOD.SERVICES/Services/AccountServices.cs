using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Text;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.SERVICES.Helpers;

namespace UPOD.SERVICES.Services
{
    public interface IAccountService
    {
        Task<ResponseModel<AccountResponse>> GetAll(PaginationRequest model, SearchRequest value);
        Task<ObjectModelResponse> GetAccountDetails(Guid id);
        Task<ObjectModelResponse> UpdateAccount(Guid id, AccountUpdateRequest model);
        Task<ObjectModelResponse> CreateAccountTechnician(AccountRequest model);
        Task<ObjectModelResponse> CreateAccountCustomer(AccountRequest model);
        Task<ObjectModelResponse> CreateAccountAdmin(AccountRequest model);
        Task<ObjectModelResponse> DisableAccount(Guid id);
        Task<ObjectModelResponse> Login(LoginRequest model);
        Task<ResponseModel<RoleResponse>> GetAllRoles(PaginationRequest model);
        Task<ObjectModelResponse> CreateAccount(AccountCreateRequest model);
        Task<ObjectModelResponse> ChangePassword(ChangePasswordRequest model, Guid id);
        Task<ResponseModel<AccountResponse>> GetAllAccountIsNotAssign(PaginationRequest model, SearchRequest value);

    }

    public class AccountService : IAccountService
    {
        private readonly Database_UPODContext _context;
        private readonly IConfiguration _configuration;

        public AccountService(Database_UPODContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task<ResponseModel<RoleResponse>> GetAllRoles(PaginationRequest model)
        {
            var total = await _context.Roles.Where(a => a.IsDelete == false && a.RoleName != "Admin").ToListAsync();
            var roles = await _context.Roles.Where(a => a.IsDelete == false && a.RoleName != "Admin").Select(a => new RoleResponse
            {

                id = a.Id,
                code = a.Code,
                role_name = a.RoleName

            }).OrderBy(x => x.code).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            return new ResponseModel<RoleResponse>(roles)
            {
                Total = total.Count,
                Type = "Roles"

            };
        }
        public async Task<ObjectModelResponse> ChangePassword(ChangePasswordRequest model, Guid id)
        {
            var account = await _context.Accounts.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var message = "blank";
            var status = 500;
            var data = new ChagnePasswordResponse();

            if (account!.Password!.Equals(model.new_password) && model.new_password!.Equals(model.old_password))
            {
                message = "The new password must not be the same as the old password";
                status = 400;
            }
            else if (model.old_password == null || model.old_password == "")
            {
                message = "Old password cannot be blank";
                status = 400;
            }
            else if (model.new_password == null || model.new_password == "")
            {
                message = "New password cannot be blank";
                status = 400;
            }
            else if (model.new_password != model.confirm_password)
            {
                message = "Confirmation password does not match";
                status = 400;
            }
            else if (account.Password! != model.old_password)
            {
                message = "Old password does not match";
                status = 400;
            }
            else
            {
                account.Password = model.new_password;
                account.UpdateDate = DateTime.UtcNow.AddHours(7);
                _context.Accounts.Update(account);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    message = "Password has been changed";
                    status = 201;
                    data = new ChagnePasswordResponse
                    {
                        message = message,
                    };

                }

            }

            return new ObjectModelResponse(data!)
            {
                Type = "Change password",
                Message = message,
                Status = status
            };
        }
        public async Task<ObjectModelResponse> Login(LoginRequest model)
        {
            var user = await _context.Accounts.Where(a => a.Username!.Equals(model.username) && a.Password!.Equals(model.password) && a.IsDelete == false).FirstOrDefaultAsync();
            if (user == null)
            {
                return new ObjectModelResponse(user!)
                {
                    Type = "Login",
                    Message = "Invalid username or password!",
                    Status = 401,
                };
            }
            else
            {
                var account = new LoginResponse();
                var role = await _context.Roles.Where(a => a.Id.Equals(user.RoleId)).Select(a => a.RoleName).FirstOrDefaultAsync();
                //if (user.RoleId.Equals(Guid.Parse("d66fe081-becb-4538-a371-a1fb56c89a33")))
                if (role!.Equals("Technician"))
                {
                    account = new LoginResponse
                    {
                        id = _context.Technicians.Where(a => a.AccountId.Equals(user.Id)).Select(a => a.Id).FirstOrDefault(),
                        code = user.Code,
                        account_id = user.Id,
                        role_id = user.RoleId,
                        role_name = _context.Roles.Where(a => a.Id.Equals(user.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                        username = user.Username,
                        token = GenerateToken(user.Id, user.RoleId, user.Code!)
                    };
                }
                //else if (user.RoleId.Equals(Guid.Parse("ef9edd4f-0885-4910-a02c-831ca863c733")))
                else if (role!.Equals("Customer"))
                {
                    account = new LoginResponse
                    {
                        id = _context.Customers.Where(a => a.AccountId.Equals(user.Id)).Select(a => a.Id).FirstOrDefault(),
                        code = user.Code,
                        account_id = user.Id,
                        role_id = user.RoleId,
                        role_name = _context.Roles.Where(a => a.Id.Equals(user.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                        username = user.Username,
                        token = GenerateToken(user.Id, user.RoleId, user.Code!)
                    };
                }
                //else if (user.RoleId.Equals(Guid.Parse("dd3cb3b4-84fe-432e-bb06-2d8aecaa640d")))
                else if (role!.Equals("Admin"))
                {
                    account = new LoginResponse
                    {
                        id = _context.Admins.Where(a => a.AccountId.Equals(user.Id)).Select(a => a.Id).FirstOrDefault(),
                        code = user.Code,
                        account_id = user.Id,
                        role_id = user.RoleId,
                        role_name = _context.Roles.Where(a => a.Id.Equals(user.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                        username = user.Username,
                        token = GenerateToken(user.Id, user.RoleId, user.Code!)
                    };
                }

                return new ObjectModelResponse(account!)
                {
                    Type = "Login",
                };
            }
        }
        public string GenerateToken(Guid accountId, Guid? roleId, string code)
        {
            var Claims = new List<Claim>
            {
                new Claim("RoleId", roleId.ToString()!),
                new Claim("AccountId", accountId.ToString()),
                new Claim("Code", code)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(Claims),
                Expires = DateTime.Now.AddDays(int.Parse(_configuration["Jwt:DateExprise"])),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<ObjectModelResponse> DisableAccount(Guid id)
        {
            var account = await _context.Accounts.Where(a => a.Id.Equals(id)).Include(a => a.Role).FirstOrDefaultAsync();
            account!.IsDelete = true;
            account.UpdateDate = DateTime.UtcNow.AddHours(7);
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            var model = new AccountResponse
            {
                id = account.Id,
                code = account.Code,
                role = new RoleResponse
                {
                    id = account.Role!.Id,
                    code = account.Role.Code,
                    role_name = account.Role.RoleName,
                },
                username = account.Username,
                is_delete = account.IsDelete,
                is_assign = account.IsAssign,
                create_date = account.CreateDate,
                update_date = account.UpdateDate,
            };
            if (account.Role.RoleName!.Equals("Customer"))
            {
                var customer = await _context.Customers.Where(a => a.IsDelete == false && a.AccountId.Equals(account.Id)).FirstOrDefaultAsync();
                if (customer != null)
                {
                    customer!.AccountId = null;
                }
            }
            else if (account.Role.RoleName!.Equals("Technician"))
            {
                var technician = await _context.Technicians.Where(a => a.IsDelete == false && a.AccountId.Equals(account.Id)).FirstOrDefaultAsync();
                if (technician != null)
                {
                    technician!.AccountId = null;
                }
            }
            return new ObjectModelResponse(model)
            {
                Status = 201,
                Type = "Account",
            };
        }
        public async Task<ResponseModel<AccountResponse>> GetAllAccountIsNotAssign(PaginationRequest model, SearchRequest value)
        {
            var role_name = await _context.Roles.Where(a => a.RoleName!.Equals(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
            var total = await _context.Accounts.Where(a => a.IsDelete == false && a.IsAssign == false && a.RoleId!.Equals(role_name)).ToListAsync();
            var accounts = await _context.Accounts.Where(a => a.IsDelete == false && a.IsAssign == false && a.RoleId!.Equals(role_name)).Select(p => new AccountResponse
            {
                id = p.Id,
                code = p.Code,
                role = new RoleResponse
                {
                    id = p.Role!.Id,
                    code = p.Role.Code,
                    role_name = p.Role.RoleName,
                },
                username = p.Username,
                is_assign = p.IsAssign,
                is_delete = p.IsDelete,
                create_date = p.CreateDate,
                update_date = p.UpdateDate,

            }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            return new ResponseModel<AccountResponse>(accounts)
            {
                Total = total.Count,
                Type = "Accounts"
            };
        }
        public async Task<ResponseModel<AccountResponse>> GetAll(PaginationRequest model, SearchRequest value)
        {
            var total = await _context.Accounts.Where(a => a.IsDelete == false && a.Role!.RoleName != "Admin").ToListAsync();
            var accounts = new List<AccountResponse>();
            if (value.search == null)
            {
                accounts = await _context.Accounts.Where(a => a.IsDelete == false && a.Role!.RoleName != "Admin").Select(p => new AccountResponse
                {
                    id = p.Id,
                    code = p.Code,
                    role = new RoleResponse
                    {
                        id = p.Role!.Id,
                        code = p.Role.Code,
                        role_name = p.Role.RoleName,
                    },
                    username = p.Username,
                    is_assign = p.IsAssign,
                    is_delete = p.IsDelete,
                    create_date = p.CreateDate,
                    update_date = p.UpdateDate,

                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                var role = await _context.Roles.Where(a => a.IsDelete == false && a.RoleName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.Accounts.Where(a => a.IsDelete == false
                && a.Role!.RoleName != "Admin"
                && (a.RoleId.Equals(role)
                || a.Username!.Contains(value.search)
                || a.Code!.Contains(value.search))).ToListAsync();
                accounts = await _context.Accounts.Where(a => a.IsDelete == false
                && a.Role!.RoleName != "Admin"
                && (a.RoleId.Equals(role)
                || a.Username!.Contains(value.search)
                || a.Code!.Contains(value.search))).Select(p => new AccountResponse
                {
                    id = p.Id,
                    code = p.Code,
                    role = new RoleResponse
                    {
                        id = p.Role!.Id,
                        code = p.Role.Code,
                        role_name = p.Role.RoleName,
                    },
                    username = p.Username,
                    is_assign = p.IsAssign,
                    is_delete = p.IsDelete,
                    create_date = p.CreateDate,
                    update_date = p.UpdateDate,

                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<AccountResponse>(accounts)
            {
                Total = total.Count,
                Type = "Accounts"
            };
        }
        public async Task<ObjectModelResponse> GetAccountDetails(Guid id)
        {
            var account = await _context.Accounts.Where(a => a.IsDelete == false && a.Id.Equals(id)).Select(p => new AccountDetailsResponse
            {
                id = p.Id,
                code = p.Code,
                role = new RoleResponse
                {
                    id = p.Role!.Id,
                    code = p.Role.Code,
                    role_name = p.Role.RoleName,
                },
                password = p.Password,
                username = p.Username,
                is_assign = p.IsAssign,
                is_delete = p.IsDelete,
                create_date = p.CreateDate,
                update_date = p.UpdateDate,

            }).FirstOrDefaultAsync();
            return new ObjectModelResponse(account!)
            {
                Type = "Account"
            };
        }
        public async Task<ObjectModelResponse> CreateAccount(AccountCreateRequest model)
        {
            var account_id = Guid.NewGuid();
            while (true)
            {
                var account_dup = await _context.Accounts.Where(x => x.Id.Equals(account_id)).FirstOrDefaultAsync();
                if (account_dup == null)
                {
                    break;
                }
                else
                {
                    account_id = Guid.NewGuid();
                }
            }
            var data = new AccountResponse();
            var message = "blank";
            var status = 500;
            if (model.password!.Length <= 7)
            {
                message = "Password must not be less than 8 characters!";
                status = 400;
            }
            else
            {
                var code_number = await GetLastCode();
                var code = CodeHelper.GeneratorCode("ACC", code_number + 1);
                while (true)
                {
                    var code_dup = await _context.Accounts.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                    if (code_dup == null)
                    {
                        break;
                    }
                    else
                    {
                        code = "ACC-" + code_number++.ToString();
                    }
                }
                while (true)
                {
                    var code_dup = await _context.Requests.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                    if (code_dup == null)
                    {
                        break;
                    }
                    else
                    {
                        code = "ACC-" + code_number++.ToString();
                    }
                }
                var account = new Account
                {
                    Id = account_id,
                    Code = code,
                    RoleId = await _context.Roles.Where(a => a.RoleName.Equals(model.role_name) && a.IsDelete == false).Select(a => a.Id).FirstOrDefaultAsync(),
                    Username = model.user_name,
                    Password = model.password,
                    IsDelete = false,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    UpdateDate = DateTime.UtcNow.AddHours(7),
                    IsAssign = false,
                };
                var username = await _context.Accounts.Where(x => x.Username!.Equals(account.Username) && x.IsDelete == false).FirstOrDefaultAsync();
                if (username != null)
                {
                    status = 400;
                    message = "Username is already exists!";
                }
                else
                {

                    message = "Successfully";
                    status = 200;
                    _context.Accounts.Add(account);
                    var rs = await _context.SaveChangesAsync();
                    if (rs > 0)
                    {
                        data = (new AccountResponse
                        {
                            id = account.Id,
                            code = account.Code,
                            role = new RoleResponse
                            {
                                id = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Id).FirstOrDefault(),
                                code = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Code).FirstOrDefault(),
                                role_name = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                            },
                            username = account.Username,
                            is_delete = account.IsDelete,
                            create_date = account.CreateDate,
                            update_date = account.UpdateDate,
                            is_assign = account.IsAssign,
                        });
                    }
                }
            }

            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Account"
            };
        }
        public async Task<ObjectModelResponse> CreateAccountTechnician(AccountRequest model)
        {
            var account_id = Guid.NewGuid();
            while (true)
            {
                var account_dup = await _context.Accounts.Where(x => x.Id.Equals(account_id)).FirstOrDefaultAsync();
                if (account_dup == null)
                {
                    break;
                }
                else
                {
                    account_id = Guid.NewGuid();
                }
            }
            var data = new AccountResponse();
            var message = "blank";
            var status = 500;
            if (model.password!.Length <= 7)
            {
                message = "Password must not be less than 8 characters!";
                status = 400;
            }
            else
            {
                var code_number = await GetLastCode();
                var code = CodeHelper.GeneratorCode("ACC", code_number + 1);
                while (true)
                {
                    var code_dup = await _context.Accounts.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                    if (code_dup == null)
                    {
                        break;
                    }
                    else
                    {
                        code = "ACC-" + code_number++.ToString();
                    }
                }
                var account = new Account
                {
                    Id = account_id,
                    Code = code,
                    RoleId = await _context.Roles.Where(a => a.RoleName.Equals("Technician") && a.IsDelete == false).Select(a => a.Id).FirstOrDefaultAsync(),
                    Username = model.user_name,
                    Password = model.password,
                    IsDelete = false,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    UpdateDate = DateTime.UtcNow.AddHours(7),
                    IsAssign = false,
                };
                var username = await _context.Accounts.Where(x => x.Username!.Equals(account.Username) && x.IsDelete == false).FirstOrDefaultAsync();
                if (username != null)
                {
                    status = 400;
                    message = "Username is already exists!";
                }
                else
                {

                    message = "Successfully";
                    status = 200;
                    _context.Accounts.Add(account);
                    var rs = await _context.SaveChangesAsync();
                    if (rs > 0)
                    {
                        data = (new AccountResponse
                        {
                            id = account.Id,
                            code = account.Code,
                            role = new RoleResponse
                            {
                                id = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Id).FirstOrDefault(),
                                code = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Code).FirstOrDefault(),
                                role_name = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                            },
                            username = account.Username,
                            is_delete = account.IsDelete,
                            create_date = account.CreateDate,
                            update_date = account.UpdateDate,
                            is_assign = account.IsAssign,
                        });
                    }
                }
            }
            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Account"
            };
        }
        public async Task<ObjectModelResponse> CreateAccountCustomer(AccountRequest model)
        {
            var account_id = Guid.NewGuid();
            while (true)
            {
                var account_dup = await _context.Accounts.Where(x => x.Id.Equals(account_id)).FirstOrDefaultAsync();
                if (account_dup == null)
                {
                    break;
                }
                else
                {
                    account_id = Guid.NewGuid();
                }
            }
            var data = new AccountResponse();
            var message = "blank";
            var status = 500;
            if (model.password!.Length <= 7)
            {
                message = "Password must not be less than 8 characters!";
                status = 400;
            }
            else
            {
                var code_number = await GetLastCode();
                var code = CodeHelper.GeneratorCode("ACC", code_number + 1);
                while (true)
                {
                    var code_dup = await _context.Accounts.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                    if (code_dup == null)
                    {
                        break;
                    }
                    else
                    {
                        code = "ACC-" + code_number++.ToString();
                    }
                }
                var account = new Account
                {
                    Id = account_id,
                    Code = code,
                    RoleId = await _context.Roles.Where(a => a.RoleName.Equals("Customer") && a.IsDelete == false).Select(a => a.Id).FirstOrDefaultAsync(),
                    Username = model.user_name,
                    Password = model.password,
                    IsDelete = false,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    UpdateDate = DateTime.UtcNow.AddHours(7),
                    IsAssign = false,
                };
                var username = await _context.Accounts.Where(x => x.Username!.Equals(account.Username) && x.IsDelete == false).FirstOrDefaultAsync();
                if (username != null)
                {
                    status = 400;
                    message = "Username is already exists!";
                }
                else
                {

                    message = "Successfully";
                    status = 200;
                    _context.Accounts.Add(account);
                    var rs = await _context.SaveChangesAsync();
                    if (rs > 0)
                    {
                        data = (new AccountResponse
                        {
                            id = account.Id,
                            code = account.Code,
                            role = new RoleResponse
                            {
                                id = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Id).FirstOrDefault(),
                                code = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Code).FirstOrDefault(),
                                role_name = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                            },
                            username = account.Username,
                            is_delete = account.IsDelete,
                            create_date = account.CreateDate,
                            is_assign = account.IsAssign,
                            update_date = account.UpdateDate,
                        });
                    }
                }
            }
            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Account"
            };
        }
        public async Task<ObjectModelResponse> CreateAccountAdmin(AccountRequest model)
        {
            var account_id = Guid.NewGuid();
            while (true)
            {
                var account_dup = await _context.Accounts.Where(x => x.Id.Equals(account_id)).FirstOrDefaultAsync();
                if (account_dup == null)
                {
                    break;
                }
                else
                {
                    account_id = Guid.NewGuid();
                }
            }
            var data = new AccountResponse();
            var message = "blank";
            var status = 500;
            if (model.password!.Length <= 7)
            {
                message = "Password must not be less than 8 characters!";
                status = 400;
            }
            else
            {
                var code_number = await GetLastCode();
                var code = CodeHelper.GeneratorCode("ACC", code_number + 1);
                while (true)
                {
                    var code_dup = await _context.Accounts.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                    if (code_dup == null)
                    {
                        break;
                    }
                    else
                    {
                        code = "ACC-" + code_number++.ToString();
                    }
                }
                var account = new Account
                {
                    Id = account_id,
                    Code = code,
                    RoleId = await _context.Roles.Where(a => a.RoleName.Equals("Admin") && a.IsDelete == false).Select(a => a.Id).FirstOrDefaultAsync(),
                    Username = model.user_name,
                    Password = model.password,
                    IsDelete = false,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    UpdateDate = DateTime.UtcNow.AddHours(7),
                    IsAssign = false,
                };
                var username = await _context.Accounts.Where(x => x.Username!.Equals(account.Username) && x.IsDelete == false).FirstOrDefaultAsync();
                if (username != null)
                {
                    status = 400;
                    message = "Username is already exists!";
                }
                else
                {

                    message = "Successfully";
                    status = 200;
                    _context.Accounts.Add(account);
                    var rs = await _context.SaveChangesAsync();
                    if (rs > 0)
                    {
                        data = (new AccountResponse
                        {
                            id = account.Id,
                            code = account.Code,
                            role = new RoleResponse
                            {
                                id = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Id).FirstOrDefault(),
                                code = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Code).FirstOrDefault(),
                                role_name = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                            },
                            username = account.Username,
                            is_delete = account.IsDelete,
                            create_date = account.CreateDate,
                            is_assign = account.IsAssign,
                            update_date = account.UpdateDate,
                        });
                    }
                }
            }
            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Account"
            };
        }


        public async Task<ObjectModelResponse> UpdateAccount(Guid id, AccountUpdateRequest model)
        {
            var account = await _context.Accounts.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var data = new AccountResponse();
            account!.RoleId = model.role_id;
            account!.Password = model.password;
            account!.UpdateDate = DateTime.UtcNow.AddHours(7);
            var message = "blank";
            var status = 500;
            if (model.password!.Length <= 7)
            {
                message = "Password must not be less than 8 characters!";
                status = 400;
            }
            else
            {
                message = "Successfully";
                status = 200;
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new AccountResponse
                    {
                        id = account!.Id,
                        code = account.Code,
                        role = new RoleResponse
                        {
                            id = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.Code).FirstOrDefault(),
                            role_name = _context.Roles.Where(a => a.Id.Equals(account.RoleId)).Select(a => a.RoleName).FirstOrDefault(),
                        },
                        username = account.Username,
                        is_delete = account.IsDelete,
                        create_date = account.CreateDate,
                        update_date = account.UpdateDate,
                        is_assign = account.IsAssign,
                    };
                }
            }
            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "Account"
            };
        }
        private async Task<int> GetLastCode()
        {
            var account = await _context.Accounts.OrderByDescending(x => x.Code).FirstOrDefaultAsync();
            return CodeHelper.StringToInt(account!.Code!);
        }
    }

}
