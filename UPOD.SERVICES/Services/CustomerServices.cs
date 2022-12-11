using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq.Dynamic.Core;
using System.Numerics;
using UPOD.API.HubService;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using UPOD.SERVICES.Enum;
using UPOD.SERVICES.Helpers;

namespace UPOD.SERVICES.Services
{
    public interface ICustomerService
    {
        Task<ResponseModel<CustomerResponse>> GetAll(PaginationRequest model, SearchRequest value);
        Task<ResponseModel<ContractResponse>> GetAllContractByCustomer(PaginationRequest model, SearchRequest value, Guid id);
        Task<ObjectModelResponse> CreateCustomer(CustomerRequest model);
        Task<ObjectModelResponse> GetCustomerDetails(Guid id);
        Task<ObjectModelResponse> UpdateCustomer(Guid id, CustomerUpdateRequest model);
        Task<ObjectModelResponse> DisableCustomer(Guid id);
        Task<ResponseModel<ServiceResponse>> GetServiceByCustomerId(Guid id);
        Task<ResponseModel<AgencyOfCustomerResponse>> GetAgenciesByCustomerId(Guid id, PaginationRequest model, SearchRequest value);
        Task<ResponseModel<RequestResponse>> GetListRequestsByCustomerId(PaginationRequest model, FilterStatusRequest value, Guid id);
        Task<ResponseModel<ServiceNotInContractViewResponse>> GetServiceNotInContractCustomerId(Guid id);
        Task<ObjectModelResponse> ApproveContract(Guid cus_id, Guid con_id);
        Task<ObjectModelResponse> RejectContract(Guid cus_id, Guid con_id, ContractRejectRequest model);
        Task<ObjectModelResponse> ApproveRequestResolved(Guid id);
        Task<ObjectModelResponse> ApproveMaintenanceReport(Guid id);


    }

    public class CustomerServices : ICustomerService
    {
        private readonly Database_UPODContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotifyHub> _notifyHub;

        public CustomerServices(Database_UPODContext context, INotificationService notificationService, IHubContext<NotifyHub> notifyHub)
        {
            _context = context;
            _notificationService = notificationService;
            _notifyHub = notifyHub;
        }
        public async Task<ObjectModelResponse> ApproveMaintenanceReport(Guid id)
        {
            var report = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.IsProcessed == true && a.Id.Equals(id)).FirstOrDefaultAsync();
            var reportServices = await _context.MaintenanceReportServices.Where(a => a.IsResolved == false && a.Created == true).ToListAsync();
            if (reportServices != null)
            {
                foreach (var item in reportServices)
                {
                    var request = await _context.Requests.Where(a => a.IsDelete == false && a.Id.Equals(item.RequestId)).FirstOrDefaultAsync();
                    if (request!.RequestStatus!.Equals("RESOLVED"))
                    {
                        request!.RequestStatus = ProcessStatus.COMPLETED.ToString();
                    }
                }
            }
            report!.Status = ReportStatus.COMPLETED.ToString();
            report.UpdateDate = DateTime.UtcNow.AddHours(7);
            var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
            foreach (var item in admins)
            {
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.MR.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a maintenance report completed!",
                    CurrentObject_Id = report.MaintenanceScheduleId,
                    UserId = item.Id,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
            }
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.MR.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "You have a maintenance report completed!",
                CurrentObject_Id = report.MaintenanceScheduleId,
                UserId = report.CreateBy,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage", report.CreateBy);
            await _context.SaveChangesAsync();
            return new ObjectModelResponse(report)
            {
                Type = "Maintenance Report"
            };
        }
        public async Task<ObjectModelResponse> ApproveRequestResolved(Guid id)
        {
            var request = await _context.Requests.Where(a => a.IsDelete == false && a.Id.Equals(id)).FirstOrDefaultAsync();
            request!.UpdateDate = DateTime.UtcNow.AddHours(7);
            request!.RequestStatus = ProcessStatus.COMPLETED.ToString();
            var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
            foreach (var item in admins)
            {
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.RE.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a request completed!",
                    CurrentObject_Id = request.Id,
                    UserId = item.Id,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
            }
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.RE.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "You have a request completed!",
                CurrentObject_Id = request.Id,
                UserId = request.CurrentTechnicianId,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CurrentTechnicianId);
            await _context.SaveChangesAsync();
            return new ObjectModelResponse(request!)
            {
                Type = "Request"
            };
        }


        public async Task<ObjectModelResponse> RejectContract(Guid cus_id, Guid con_id, ContractRejectRequest model)
        {
            var contract = await _context.Contracts.Where(a => a.IsDelete == false
            && a.Id.Equals(con_id)
            && a.IsAccepted == false).FirstOrDefaultAsync();
            var data = new ContractResponse();
            var status = 500;
            var message = "blank";
            if (contract!.CustomerId != cus_id)
            {
                message = "The customer does not own the contract";
                status = 400;
            }
            else
            {
                message = "Susscessfully";
                status = 200;
                contract!.UpdateDate = DateTime.UtcNow.AddHours(7);
                contract!.RejectReason = model.reject_reason;
                contract!.IsExpire = true;
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.CON.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "The contract have been rejected by customer!",
                        CurrentObject_Id = contract.Id,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                }
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {

                    data = new ContractResponse
                    {
                        id = contract!.Id,
                        code = contract.Code,
                        contract_name = contract.ContractName,
                        customer = new CustomerViewResponse
                        {
                            id = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                            cus_name = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                            description = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                            phone = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                            address = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                            mail = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                        },
                        reject_reason = contract.RejectReason,
                        is_accepted = contract.IsAccepted,
                        start_date = contract.StartDate,
                        end_date = contract.EndDate,
                        is_delete = contract.IsDelete,
                        create_date = contract.CreateDate,
                        update_date = contract.UpdateDate,
                        contract_price = contract.ContractPrice,
                        description = contract.Description,
                        is_expire = contract.IsExpire,
                        attachment = contract.Attachment,
                        terminal_content = contract.TerminalContent,
                        frequency_maintain_time = contract.FrequencyMaintainTime,
                        service = _context.ContractServices.Where(x => x.ContractId.Equals(contract.Id)).Select(x => new ServiceViewResponse
                        {
                            id = x.ServiceId,
                            code = x.Service!.Code,
                            service_name = x.Service!.ServiceName,
                            description = x.Service!.Description,
                        }).ToList(),
                    };
                }
            }
            return new ObjectModelResponse(data!)
            {
                Message = message,
                Status = status,
                Type = "Contract",
            };
        }
        private async Task<int> GetLastCode1()
        {
            var maintainSchedules = await _context.MaintenanceSchedules.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(maintainSchedules!.Code!);
        }
        public async Task<ObjectModelResponse> ApproveContract(Guid cus_id, Guid con_id)
        {
            var contract = await _context.Contracts.Where(a => a.IsDelete == false
            && a.Id.Equals(con_id)
            && a.IsAccepted == false).FirstOrDefaultAsync();
            var data = new ContractResponse();
            var status = 500;
            var message = "blank";
            if (contract!.CustomerId != cus_id)
            {
                message = "The customer does not own the contract";
                status = 400;
            }
            else
            {
                if (contract.FrequencyMaintainTime > 0)
                {
                    var lastTime = contract.EndDate - contract.StartDate;
                    var lastDay = lastTime!.Value.Days - 10;
                    var listAgency = await _context.Agencies.Where(a => a.CustomerId.Equals(contract.CustomerId) && a.IsDelete == false).ToListAsync();
                    var code_number1 = await GetLastCode1();
                    var maintenanceTime = lastDay / contract.FrequencyMaintainTime;

                    foreach (var item in listAgency)
                    {
                        var maintenanceDate = contract.StartDate;
                        for (int i = 1; i <= contract.FrequencyMaintainTime; i++)
                        {
                            maintenanceDate = maintenanceDate!.Value.AddDays(maintenanceTime!.Value);
                            var maintenance_id = Guid.NewGuid();
                            while (true)
                            {
                                var maintenance_dup = await _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenance_id)).FirstOrDefaultAsync();
                                if (maintenance_dup == null)
                                {
                                    break;
                                }
                                else
                                {
                                    maintenance_id = Guid.NewGuid();
                                }
                            }
                            var code1 = CodeHelper.GeneratorCode("MS", code_number1++);
                            while (true)
                            {
                                var code_dup = await _context.MaintenanceSchedules.Where(x => x.Code.Equals(code1)).FirstOrDefaultAsync();
                                if (code_dup == null)
                                {
                                    break;
                                }
                                else
                                {
                                    code1 = "MS-" + code_number1++.ToString();
                                }
                            }
                            var maintenanceSchedule = new MaintenanceSchedule
                            {
                                Id = maintenance_id,
                                Code = code1,
                                AgencyId = item.Id,
                                CreateDate = DateTime.UtcNow.AddHours(7),
                                UpdateDate = DateTime.UtcNow.AddHours(7),
                                IsDelete = false,
                                Name = "Maintenance Schedule: " + i,
                                Status = Enum.ScheduleStatus.SCHEDULED.ToString(),
                                TechnicianId = item.TechnicianId,
                                MaintainTime = maintenanceDate,
                                StartDate = null,
                                EndDate = null,
                                ContractId = contract.Id

                            };
                            await _context.MaintenanceSchedules.AddAsync(maintenanceSchedule);
                        }
                    }
                }

                message = "Susscessfully";
                status = 200;
                contract!.IsAccepted = true;
                contract!.IsExpire = false;
                contract!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.CON.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "The contract have been approved by customer!",
                        CurrentObject_Id = contract.Id,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                }
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {

                    data = new ContractResponse
                    {
                        id = contract!.Id,
                        code = contract.Code,
                        contract_name = contract.ContractName,
                        customer = new CustomerViewResponse
                        {
                            id = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                            cus_name = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                            description = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                            phone = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                            address = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                            mail = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                        },
                        reject_reason = contract.RejectReason,
                        is_accepted = contract.IsAccepted,
                        start_date = contract.StartDate,
                        end_date = contract.EndDate,
                        is_delete = contract.IsDelete,
                        create_date = contract.CreateDate,
                        update_date = contract.UpdateDate,
                        contract_price = contract.ContractPrice,
                        description = contract.Description,
                        is_expire = contract.IsExpire,
                        attachment = contract.Attachment,
                        terminal_content = contract.TerminalContent,
                        frequency_maintain_time = contract.FrequencyMaintainTime,
                        service = _context.ContractServices.Where(x => x.ContractId.Equals(contract.Id)).Select(x => new ServiceViewResponse
                        {
                            id = x.ServiceId,
                            code = x.Service!.Code,
                            service_name = x.Service!.ServiceName,
                            description = x.Service!.Description,
                        }).ToList(),
                    };
                }
            }
            return new ObjectModelResponse(data!)
            {
                Status = status,
                Message = message,
                Type = "Contract",
            };
        }
        public async Task<ResponseModel<ContractResponse>> GetAllContractByCustomer(PaginationRequest model, SearchRequest value, Guid id)
        {
            var total = await _context.Contracts.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).ToListAsync();
            var contracts = new List<ContractResponse>();
            if (value.search == null)
            {
                total = await _context.Contracts.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).ToListAsync();
                contracts = await _context.Contracts.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).Select(a => new ContractResponse
                {
                    id = a.Id,
                    code = a.Code,
                    contract_name = a.ContractName,
                    customer = new CustomerViewResponse
                    {
                        id = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                    },
                    is_accepted = a.IsAccepted,
                    start_date = a.StartDate,
                    end_date = a.EndDate,
                    is_delete = a.IsDelete,
                    is_expire = a.IsExpire,
                    reject_reason = a.RejectReason,
                    terminal_content = a.TerminalContent,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    contract_price = a.ContractPrice,
                    description = a.Description,
                    attachment = a.Attachment,
                    frequency_maintain_time = a.FrequencyMaintainTime,
                    service = _context.ContractServices.Where(x => x.ContractId.Equals(a.Id)).Select(x => new ServiceViewResponse
                    {
                        id = x.ServiceId,
                        code = x.Service!.Code,
                        service_name = x.Service!.ServiceName,
                        description = x.Service!.Description,
                    }).ToList()

                }).OrderByDescending(x => x.create_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {

                var customer = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.Contracts.Where(a => a.IsDelete == false
                && a.CustomerId.Equals(id)
                 && (a.Code!.Contains(value.search)
                 || a.ContractName!.Contains(value.search)
                 || a.CustomerId!.Equals(customer))).ToListAsync();
                contracts = await _context.Contracts.Where(a => a.IsDelete == false
                && a.CustomerId.Equals(id)
                && (a.Code!.Contains(value.search)
                || a.ContractName!.Contains(value.search)
                || a.CustomerId!.Equals(customer))).Select(a => new ContractResponse
                {
                    id = a.Id,
                    code = a.Code,
                    contract_name = a.ContractName,
                    customer = new CustomerViewResponse
                    {
                        id = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                    },
                    start_date = a.StartDate,
                    end_date = a.EndDate,
                    is_delete = a.IsDelete,
                    is_expire = a.IsExpire,
                    terminal_content = a.TerminalContent,
                    reject_reason = a.RejectReason,
                    is_accepted = a.IsAccepted,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    contract_price = a.ContractPrice,
                    description = a.Description,
                    attachment = a.Attachment,
                    frequency_maintain_time = a.FrequencyMaintainTime,
                    service = _context.ContractServices.Where(x => x.ContractId.Equals(a.Id)).Select(x => new ServiceViewResponse
                    {
                        id = x.ServiceId,
                        code = x.Service!.Code,
                        service_name = x.Service!.ServiceName,
                        description = x.Service!.Description,
                    }).ToList()

                }).OrderByDescending(x => x.create_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<ContractResponse>(contracts)
            {
                Total = total.Count,
                Type = "Contracts"
            };
        }

        public async Task<ResponseModel<RequestResponse>> GetListRequestsByCustomerId(PaginationRequest model, FilterStatusRequest value, Guid id)
        {
            var total = await _context.Requests.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).ToListAsync();
            var requests = new List<RequestResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.Requests.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).ToListAsync();
                requests = await _context.Requests.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).Select(a => new RequestResponse
                {
                    id = a.Id,
                    code = a.Code,
                    request_name = a.RequestName,
                    admin_id = a.AdminId,
                    customer = new CustomerViewResponse
                    {
                        id = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Address).FirstOrDefault(),
                    },
                    service = new ServiceViewResponse
                    {
                        id = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Code).FirstOrDefault(),
                        service_name = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.ServiceName).FirstOrDefault(),
                        description = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Description).FirstOrDefault(),
                    },
                    contract = new ContractViewResponse
                    {
                        id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                    },
                    is_system = a.IsSystem,
                    reject_reason = a.ReasonReject,
                    description = a.RequestDesciption,
                    request_status = a.RequestStatus,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    technicican = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    }

                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                if (value.search == null)
                {
                    value.search = "";
                }
                if (value.status == null)
                {
                    value.status = "";
                }
                if (value.search.ToLower().Contains("admin"))
                {
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CustomerId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && a.AdminId != null)).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CustomerId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && a.AdminId != null)).Select(a => new RequestResponse
                    {
                        id = a.Id,
                        code = a.Code,
                        admin_id = a.AdminId,
                        request_name = a.RequestName,
                        customer = new CustomerViewResponse
                        {
                            id = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                            cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                            description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                            phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                            address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                            mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                        },
                        agency = new AgencyViewResponse
                        {
                            id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Code).FirstOrDefault(),
                            agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Address).FirstOrDefault(),
                        },
                        service = new ServiceViewResponse
                        {
                            id = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Code).FirstOrDefault(),
                            service_name = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.ServiceName).FirstOrDefault(),
                            description = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Description).FirstOrDefault(),
                        },
                        contract = new ContractViewResponse
                        {
                            id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                            name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                        },
                        reject_reason = a.ReasonReject,
                        description = a.RequestDesciption,
                        request_status = a.RequestStatus,
                        create_date = a.CreateDate,
                        update_date = a.UpdateDate,
                        is_system = a.IsSystem,
                        technicican = new TechnicianViewResponse
                        {
                            id = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            email = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        }
                    }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
                }
                else if (value.search.ToLower().Contains("system"))
                {
                    var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && a.IsSystem == true
                    && a.CustomerId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.IsSystem == true
                    && a.CustomerId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!))).Select(a => new RequestResponse
                    {
                        id = a.Id,
                        code = a.Code,
                        request_name = a.RequestName,
                        customer = new CustomerViewResponse
                        {
                            id = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                            address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                            mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                            phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                            cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                            description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                        },
                        admin_id = a.AdminId,
                        agency = new AgencyViewResponse
                        {
                            id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Code).FirstOrDefault(),
                            agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Address).FirstOrDefault(),
                            phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                        },
                        service = new ServiceViewResponse
                        {
                            id = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Code).FirstOrDefault(),
                            service_name = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.ServiceName).FirstOrDefault(),
                            description = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Description).FirstOrDefault(),
                        },
                        contract = new ContractViewResponse
                        {
                            id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                            name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                        },
                        is_system = a.IsSystem,
                        reject_reason = a.ReasonReject,
                        description = a.RequestDesciption,
                        request_status = a.RequestStatus,
                        create_date = a.CreateDate,
                        update_date = a.UpdateDate,
                        technicican = new TechnicianViewResponse
                        {
                            id = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                            email = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        },
                    }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
                }
                else
                {
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CurrentTechnicianId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && a.AdminId != null)).ToListAsync();
                    var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CustomerId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.RequestName!.Contains(value.search!)
                    || a.Code!.Contains(value.search!)
                    || a.RequestDesciption!.Contains(value.search!)
                    || a.AgencyId!.Equals(agency_name)
                    || a.CustomerId!.Equals(customer_name)
                    || a.ContractId!.Equals(contract_name)
                    || a.ServiceId!.Equals(service_name)))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CustomerId.Equals(id)
                    && (a.RequestName!.Contains(value.search!)
                    || a.Code!.Contains(value.search)
                    || a.RequestDesciption!.Contains(value.search!)
                    || a.AgencyId!.Equals(agency_name)
                    || a.CustomerId!.Equals(customer_name)
                    || a.ContractId!.Equals(contract_name)
                    || a.ServiceId!.Equals(service_name))
                    && (a.RequestStatus!.Contains(value.status!))).Select(a => new RequestResponse
                    {
                        id = a.Id,
                        code = a.Code,
                        admin_id = a.AdminId,
                        request_name = a.RequestName,
                        customer = new CustomerViewResponse
                        {
                            id = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Code).FirstOrDefault(),
                            cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                            description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Description).FirstOrDefault(),
                            phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
                            address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Address).FirstOrDefault(),
                            mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
                        },
                        agency = new AgencyViewResponse
                        {
                            id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Code).FirstOrDefault(),
                            agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Address).FirstOrDefault(),
                        },
                        service = new ServiceViewResponse
                        {
                            id = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Code).FirstOrDefault(),
                            service_name = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.ServiceName).FirstOrDefault(),
                            description = _context.Services.Where(x => x.Id.Equals(a.ServiceId)).Select(a => a.Description).FirstOrDefault(),
                        },
                        contract = new ContractViewResponse
                        {
                            id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                            name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                        },
                        reject_reason = a.ReasonReject,
                        description = a.RequestDesciption,
                        request_status = a.RequestStatus,
                        create_date = a.CreateDate,
                        update_date = a.UpdateDate,
                        is_system = a.IsSystem,
                        technicican = new TechnicianViewResponse
                        {
                            id = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            email = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        }
                    }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
                }
            }

            return new ResponseModel<RequestResponse>(requests)
            {
                Total = total.Count,
                Type = "Requests"
            };
        }
        public async Task<ResponseModel<CustomerResponse>> GetAll(PaginationRequest model, SearchRequest value)
        {
            var total = await _context.Customers.Where(a => a.IsDelete == false).ToListAsync();
            var customers = new List<CustomerResponse>();
            if (value.search == null)
            {
                total = await _context.Customers.Where(a => a.IsDelete == false).ToListAsync();
                customers = await _context.Customers.Where(a => a.IsDelete == false).Select(a => new CustomerResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    account = new AccountViewResponse
                    {
                        id = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Code).FirstOrDefault(),
                        role_name = _context.Roles.Where(x => x.Id.Equals(a.Account!.RoleId)).Select(x => x.RoleName).FirstOrDefault(),
                        username = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Username).FirstOrDefault(),
                        password = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Password).FirstOrDefault(),
                    },
                    address = a.Address,
                    mail = a.Mail,
                    phone = a.Phone,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,


                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                total = await _context.Customers.Where(a => a.IsDelete == false
                && (a.Code!.Contains(value.search)
                || a.Name!.Contains(value.search)
                || a.Address!.Contains(value.search)
                || a.Phone!.Contains(value.search))).ToListAsync();
                customers = await _context.Customers.Where(a => a.IsDelete == false
                && (a.Code!.Contains(value.search)
                || a.Name!.Contains(value.search)
                || a.Address!.Contains(value.search)
                || a.Phone!.Contains(value.search))).Select(a => new CustomerResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    account = new AccountViewResponse
                    {
                        id = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Code).FirstOrDefault(),
                        role_name = _context.Roles.Where(x => x.Id.Equals(a.Account!.RoleId)).Select(x => x.RoleName).FirstOrDefault(),
                        username = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Username).FirstOrDefault(),
                        password = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Password).FirstOrDefault(),
                    },
                    address = a.Address,
                    mail = a.Mail,
                    phone = a.Phone,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,


                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<CustomerResponse>(customers)
            {
                Total = total.Count,
                Type = "Customers"
            };
        }
        public async Task<ObjectModelResponse> GetCustomerDetails(Guid id)
        {
            var customer = await _context.Customers.Where(a => a.Id.Equals(id) && a.IsDelete == false).Include(x => x.Account).Select(a => new CustomerResponse
            {
                id = a.Id,
                code = a.Code,
                name = a.Name,
                account = new AccountViewResponse
                {
                    id = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Id).FirstOrDefault(),
                    code = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Code).FirstOrDefault(),
                    role_name = _context.Roles.Where(x => x.Id.Equals(a.Account!.RoleId)).Select(x => x.RoleName).FirstOrDefault(),
                    username = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Username).FirstOrDefault(),
                    password = _context.Accounts.Where(x => x.Id.Equals(a.AccountId)).Select(x => x.Password).FirstOrDefault(),
                },
                description = a.Description,
                address = a.Address,
                mail = a.Mail,
                phone = a.Phone,
                is_delete = a.IsDelete,
                create_date = a.CreateDate,
                update_date = a.UpdateDate,


            }).ToListAsync();
            return new ObjectModelResponse(customer)
            {
                Type = "Customer"
            };
        }

        public async Task<ResponseModel<ServiceResponse>> GetServiceByCustomerId(Guid id)
        {

            var services = await _context.ContractServices.Where(x => x.Contract!.CustomerId.Equals(id)
            && x.Contract.IsDelete == false && x.Contract.IsExpire == false && x.Contract.IsAccepted == true
            && (x.Contract.StartDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date
            && x.Contract.EndDate!.Value.Date >= DateTime.UtcNow.AddHours(7).Date)).Select(x => new ServiceResponse
            {
                id = x.ServiceId,
                code = x.Service!.Code,
                service_name = x.Service!.ServiceName,
                description = x.Service!.Description,
                create_date = x.Service!.CreateDate,
                update_date = x.Service!.UpdateDate,
                guideline = x.Service!.Guideline,
                is_delete = x.Service!.IsDelete,
            }).Distinct().ToListAsync();

            return new ResponseModel<ServiceResponse>(services)
            {
                Total = services.Count,
                Type = "Services"
            };
        }
        public async Task<ResponseModel<ServiceNotInContractViewResponse>> GetServiceNotInContractCustomerId(Guid id)
        {

            var services_in_contract = await _context.ContractServices.Where(x => x.Contract!.CustomerId.Equals(id)
            && x.Contract.IsDelete == false && x.Contract.IsExpire == false && x.Contract.IsAccepted == true
            && (x.Contract.StartDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date
            && x.Contract.EndDate!.Value.Date >= DateTime.UtcNow.AddHours(7).Date)).Select(a => new ServiceNotInContractViewResponse
            {
                id = a.ServiceId,
                service_name = a.Service!.ServiceName,
                code = a.Service!.Code,
            }).Distinct().ToListAsync();
            var list_services = await _context.Services.Where(x => x.IsDelete == false).Select(a => new ServiceNotInContractViewResponse
            {
                id = a.Id,
                service_name = a.ServiceName,
                code = a.Code,
            }).Distinct().ToListAsync();
            var sv = new List<ServiceNotInContractViewResponse>();
            var rs = list_services.Except(services_in_contract).ToList();
            var listService = new List<ServiceNotInContractViewResponse>();
            foreach (var service in rs)
            {
                listService.Add(new ServiceNotInContractViewResponse
                {
                    id = service.id,
                    code = service.code,
                    service_name = service.service_name,
                });
            }
            var total = rs;
            return new ResponseModel<ServiceNotInContractViewResponse>(listService)
            {
                Total = total.Count,
                Type = "Services"
            };
        }
        public async Task<ResponseModel<AgencyOfCustomerResponse>> GetAgenciesByCustomerId(Guid id, PaginationRequest model, SearchRequest value)
        {
            var total = await _context.Agencies.Where(a => a.CustomerId.Equals(id) && a.IsDelete == false).ToListAsync();
            var agencies = new List<AgencyOfCustomerResponse>();
            if (value.search == null)
            {
                agencies = await _context.Agencies.Where(a => a.CustomerId.Equals(id) && a.IsDelete == false).Select(a => new AgencyOfCustomerResponse
                {
                    id = a.Id,
                    code = a.Code,
                    agency_name = a.AgencyName,
                    address = a.Address,
                    phone = a.Telephone,
                    manager_name = a.ManagerName,

                }).OrderByDescending(a => a.code).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                total = await _context.Agencies.Where(a => a.CustomerId.Equals(id) && a.IsDelete == false
                && a.CustomerId.Equals(id)
                 && (a.Code!.Contains(value.search)
                 || a.AgencyName!.Contains(value.search)
                 || a.ManagerName!.Contains(value.search))).ToListAsync();
                agencies = await _context.Agencies.Where(a => a.CustomerId.Equals(id) && a.IsDelete == false
                 && a.CustomerId.Equals(id)
                 && (a.Code!.Contains(value.search)
                 || a.AgencyName!.Contains(value.search)
                 || a.ManagerName!.Contains(value.search))).Select(a => new AgencyOfCustomerResponse
                 {
                     id = a.Id,
                     code = a.Code,
                     agency_name = a.AgencyName,
                     address = a.Address,
                     phone = a.Telephone,
                     manager_name = a.ManagerName,

                 }).OrderByDescending(a => a.code).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            return new ResponseModel<AgencyOfCustomerResponse>(agencies!)
            {
                Type = "Agencies",
                Total = total.Count,
            };
        }
        public async Task<ObjectModelResponse> CreateCustomer(CustomerRequest model)
        {
            var customer_id = Guid.NewGuid();
            while (true)
            {
                var customer_dup = await _context.Customers.Where(x => x.Id.Equals(customer_id)).FirstOrDefaultAsync();
                if (customer_dup == null)
                {
                    break;
                }
                else
                {
                    customer_id = Guid.NewGuid();
                }
            }
            var code_number = await GetLastCode();
            var code = CodeHelper.GeneratorCode("CU", code_number + 1);
            while (true)
            {
                var code_dup = await _context.Customers.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                if (code_dup == null)
                {
                    break;
                }
                else
                {
                    code = "CU-" + code_number++.ToString();
                }
            }
            var customer = new Customer
            {
                Id = customer_id,
                Code = code,
                Name = model.name,
                AccountId = model.account_id,
                Description = model.description,
                Mail = model.mail,
                Address = model.address,
                Phone = model.phone,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7),
                IsDelete = false
            };
            var account_asign = await _context.Accounts.Where(a => a.Id.Equals(model.account_id)).FirstOrDefaultAsync();
            account_asign!.IsAssign = true;
            var message = "blank";
            var status = 500;
            var data = new CustomerResponse();
            var customer_name = await _context.Customers.Where(x => x.Name!.Equals(model.name) && x.IsDelete == false).FirstOrDefaultAsync();
            var customer_phone = await _context.Customers.Where(x => x.Phone!.Equals(model.phone) && x.IsDelete == false).FirstOrDefaultAsync();
            var customer_mail = await _context.Customers.Where(x => x.Mail!.Equals(model.mail) && x.IsDelete == false).FirstOrDefaultAsync();
            if (customer_name != null)
            {
                status = 400;
                message = "CustomerName is already exists!";
            }
            else if (customer_phone != null)
            {
                status = 400;
                message = "Phone is already exists!";
            }
            else if (customer_mail != null)
            {
                status = 400;
                message = "Mail is already exists!";
            }
            else
            {
                message = "Successfully";
                status = 200;
                await _context.Customers.AddAsync(customer);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    var account = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).FirstOrDefault();
                    var role = _context.Roles.Where(x => x.Id.Equals(account!.RoleId)).FirstOrDefault();
                    data = new CustomerResponse
                    {
                        id = customer.Id,
                        code = customer.Code,
                        name = customer.Name,
                        account = new AccountViewResponse
                        {
                            id = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Code).FirstOrDefault(),
                            role_name = role!.RoleName,
                            username = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Username).FirstOrDefault(),
                            password = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Password).FirstOrDefault(),
                        },
                        description = customer.Description,
                        address = customer.Address,
                        mail = customer.Mail,
                        phone = customer.Phone,
                        is_delete = customer.IsDelete,
                        create_date = customer.CreateDate,
                        update_date = customer.UpdateDate,
                    };
                }
            }

            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Customer"
            };
        }
        public async Task<ObjectModelResponse> DisableCustomer(Guid id)
        {
            var customer = await _context.Customers.Where(x => x.Id.Equals(id)).FirstOrDefaultAsync();
            var agencies = await _context.Agencies.Where(x => x.CustomerId.Equals(id)).ToListAsync();
            var contracts = await _context.Contracts.Where(x => x.CustomerId.Equals(id)).ToListAsync();
            var account_assign = await _context.Accounts.Where(x => x.IsDelete == false && x.Id.Equals(customer!.AccountId)).FirstOrDefaultAsync();
            if (account_assign != null)
            {
                account_assign!.IsAssign = false;
            }
            customer!.IsDelete = true;
            foreach (var item in agencies)
            {
                item.IsDelete = true;
                var devices = await _context.Devices.Where(x => x.AgencyId.Equals(item.Id)).ToListAsync();
                foreach (var item1 in devices)
                {
                    item1.IsDelete = true;
                }
            }
            foreach (var item in contracts)
            {
                item.IsDelete = true;
            }

            customer.UpdateDate = DateTime.UtcNow.AddHours(7);
            var data = new CustomerResponse();
            _context.Customers.Update(customer);
            var rs = await _context.SaveChangesAsync();

            if (rs > 0)
            {
                var account = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).FirstOrDefault();
                var role = _context.Roles.Where(x => x.Id.Equals(account!.RoleId)).FirstOrDefault();
                data = new CustomerResponse
                {
                    id = customer.Id,
                    code = customer.Code,
                    name = customer.Name,
                    account = new AccountViewResponse
                    {
                        id = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Id).FirstOrDefault(),
                        code = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Code).FirstOrDefault(),
                        role_name = role!.RoleName,
                        username = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Username).FirstOrDefault(),
                        password = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Password).FirstOrDefault(),
                    },
                    description = customer.Description,
                    address = customer.Address,
                    mail = customer.Mail,
                    phone = customer.Phone,
                    is_delete = customer.IsDelete,
                    create_date = customer.CreateDate,
                    update_date = customer.UpdateDate,

                };
            }


            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Customer"
            };
        }
        public async Task<ObjectModelResponse> UpdateCustomer(Guid id, CustomerUpdateRequest model)
        {
            var customer = await _context.Customers.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var data = new CustomerResponse();
            var message = "blank";
            var status = 500;
            var customer_name = await _context.Customers.Where(x => x.Name!.Equals(model.name)).FirstOrDefaultAsync();
            var customer_phone = await _context.Customers.Where(x => x.Phone!.Equals(model.phone)).FirstOrDefaultAsync();
            var customer_mail = await _context.Customers.Where(x => x.Mail!.Equals(model.mail)).FirstOrDefaultAsync();
            if (customer_name != null && customer!.Name != model.name)
            {
                status = 400;
                message = "CustomerName is already exists!";
            }
            else if (customer_phone != null && customer!.Phone != model.phone)
            {
                status = 400;
                message = "Phone is already exists!";
            }
            else if (customer_mail != null && customer!.Mail != model.mail)
            {
                status = 400;
                message = "Mail is already exists!";
            }
            else
            {
                status = 200;
                message = "Successfully";
                customer!.Name = model.name;
                customer!.Description = model.description;
                customer!.Address = model.address;
                customer!.Mail = model.mail;
                customer!.Phone = model.phone;
                customer!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    var account = _context.Accounts.Where(x => x.Id.Equals(customer!.AccountId)).FirstOrDefault();
                    var role = _context.Roles.Where(x => x.Id.Equals(account!.RoleId)).FirstOrDefault();
                    data = new CustomerResponse
                    {
                        id = customer!.Id,
                        code = customer.Code,
                        name = customer.Name,
                        account = new AccountViewResponse
                        {
                            id = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Id).FirstOrDefault(),
                            code = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Code).FirstOrDefault(),
                            role_name = role!.RoleName,
                            username = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Username).FirstOrDefault(),
                            password = _context.Accounts.Where(x => x.Id.Equals(customer.AccountId)).Select(x => x.Password).FirstOrDefault(),
                        },
                        description = customer.Description,
                        address = customer.Address,
                        mail = customer.Mail,
                        phone = customer.Phone,
                        is_delete = customer.IsDelete,
                        create_date = customer.CreateDate,
                        update_date = customer.UpdateDate,

                    };
                }
            }
            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "Customer"
            };
        }

        private async Task<int> GetLastCode()
        {
            var customer = await _context.Customers.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(customer!.Code!);
        }
    }
}
