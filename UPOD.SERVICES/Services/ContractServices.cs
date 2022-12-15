using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using System.Linq.Dynamic.Core;
using UPOD.API.HubService;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using UPOD.SERVICES.Enum;
using UPOD.SERVICES.Helpers;
using Contract = UPOD.REPOSITORIES.Models.Contract;

namespace UPOD.SERVICES.Services
{

    public interface IContractServiceService
    {
        Task<ResponseModel<ContractResponse>> GetAll(PaginationRequest model, SearchRequest value);
        Task<ObjectModelResponse> CreateContract(ContractRequest model);
        Task<ObjectModelResponse> GetDetailsContract(Guid id);
        Task<ObjectModelResponse> DisableContract(Guid id);
        Task<ObjectModelResponse> TerminationContract(Guid id, ContractTermanationRequest model);
        Task<ResponseModel<ServiceResponse>> GetServiceByContractId(Guid id);
        Task SetContractNotify();
    }

    public class ContractServiceService : IContractServiceService
    {
        private readonly Database_UPODContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotifyHub> _notifyHub;

        public ContractServiceService(Database_UPODContext context, INotificationService notificationService, IHubContext<NotifyHub> notifyHub)
        {
            _context = context;
            _notificationService = notificationService;
            _notifyHub = notifyHub;
        }
        public async Task<ResponseModel<ServiceResponse>> GetServiceByContractId(Guid id)
        {

            var services = await _context.ContractServices.Where(x => x.Contract!.Id.Equals(id) && (x.Contract.StartDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date
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
        public async Task SetContractNotify()
        {

            var todayContracts = await _context.Contracts.Where(a => (a.EndDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date && a.IsDelete == false && a.IsExpire == false && a.IsAccepted == true)).ToListAsync();
            foreach (var item in todayContracts)
            {
                item!.IsExpire = true;
                item!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var maintainSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.ContractId.Equals(item!.Id)).ToListAsync();
                foreach (var item1 in maintainSchedules)
                {
                    if (item1.Status != "COMPLETED" && item1.Status != "MISSED")
                    {
                        item1.IsDelete = true;
                    }
                }
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.CON.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a contract is expired!",
                    CurrentObject_Id = item.Id,
                    UserId = item.CustomerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CustomerId);
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item1 in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.CON.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "A Contract of Customer is expired!",
                        CurrentObject_Id = item.Id,
                        UserId = item1.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item1.Id);
                }
            }
            await _context.SaveChangesAsync();
        }
        public async Task<ObjectModelResponse> DisableContract(Guid id)
        {
            var contract = await _context.Contracts.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            contract!.IsDelete = true;
            contract!.IsExpire = true;
            contract.UpdateDate = DateTime.UtcNow.AddHours(7);
            _context.Contracts.Update(contract);
            var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(contract!.Id)).ToListAsync();
            foreach (var item in contract_services)
            {
                _context.ContractServices.Remove(item);
            }
            var schedule = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.ContractId.Equals(contract.Id)).ToListAsync();
            foreach (var item in schedule)
            {
                if (item.Status != "COMPLETED" && item.Status != "MISSED")
                {
                    item.IsDelete = true;
                }
            }
            var data = new ContractResponse();
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new ContractResponse
                {
                    id = id,
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
                    is_accepted = contract.IsAccepted,
                    start_date = contract.StartDate,
                    end_date = contract.EndDate,
                    is_delete = contract.IsDelete,
                    create_date = contract.CreateDate,
                    reject_reason = contract.RejectReason,
                    update_date = contract.UpdateDate,
                    contract_price = contract.ContractPrice,
                    description = contract.Description,
                    attachment = contract.Attachment,
                    is_expire = contract.IsExpire,
                    frequency_maintain_time = contract.FrequencyMaintainTime,
                    terminal_content = contract.TerminalContent,
                    service = _context.ContractServices.Where(a => a.ContractId.Equals(contract.Id)).Select(x => new ServiceViewResponse
                    {
                        id = x.ServiceId,
                        code = x.Service!.Code,
                        service_name = x.Service!.ServiceName,
                        description = x.Service!.Description,
                    }).ToList()

                };
            }

            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Contract"
            };
        }

        public async Task<ResponseModel<ContractResponse>> GetAll(PaginationRequest model, SearchRequest value)
        {
            var total = await _context.Contracts.Where(a => a.IsDelete == false).ToListAsync();
            var contracts = new List<ContractResponse>();
            if (value.search == null)
            {
                total = await _context.Contracts.Where(a => a.IsDelete == false).ToListAsync();
                contracts = await _context.Contracts.Where(a => a.IsDelete == false).Select(a => new ContractResponse
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
                    terminal_content = a.TerminalContent,
                    start_date = a.StartDate,
                    end_date = a.EndDate,
                    is_delete = a.IsDelete,
                    is_expire = a.IsExpire,
                    reject_reason = a.RejectReason,
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

                var customer = await _context.Customers.Where(a => a.Name!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.Contracts.Where(a => a.IsDelete == false
                 && (a.Code!.Contains(value.search)
                 || a.ContractName!.Contains(value.search)
                 || a.CustomerId.Equals(customer))).ToListAsync();
                contracts = await _context.Contracts.Where(a => a.IsDelete == false
                && (a.Code!.Contains(value.search)
                || a.ContractName!.Contains(value.search)
                || a.CustomerId.Equals(customer))).Select(a => new ContractResponse
                {
                    id = a.Id,
                    terminal_content = a.TerminalContent,
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
                    reject_reason = a.RejectReason,
                    is_expire = a.IsExpire,
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


        public async Task<ObjectModelResponse> GetDetailsContract(Guid id)
        {
            var contract = await _context.Contracts.Where(a => a.Id.Equals(id) && a.IsDelete == false).Select(a => new ContractResponse
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
                terminal_time = a.TerminalTime,
                terminal_content = a.TerminalContent,
                img = _context.Images.Where(d => d.CurrentObject_Id.Equals(a.Id) && d.ObjectName!.Equals(ObjectName.CON.ToString())).Select(x => x.Link).ToList()!,
                start_date = a.StartDate,
                is_expire = a.IsExpire,
                is_accepted = a.IsAccepted,
                end_date = a.EndDate,
                is_delete = a.IsDelete,
                reject_reason = a.RejectReason,
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
                }).ToList(),
            }).FirstOrDefaultAsync();
            return new ObjectModelResponse(contract!)
            {
                Type = "Contract"
            };
        }
        public async Task<ObjectModelResponse> TerminationContract(Guid id, ContractTermanationRequest model)
        {
            var contract = await _context.Contracts.Where(a => a.IsDelete == false && a.Id.Equals(id)).FirstOrDefaultAsync();
            var data = new ContractResponse();
            var message = "blank";
            var status = 500;
            var checkSchedule = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
            && a.ContractId.Equals(contract!.Id)
            && (a.Status!.Equals("WARNING")
            || a.Status.Equals("PREPARING")
            || a.Status.Equals("NOTIFIED")
            || a.Status.Equals("MAINTAINING"))).ToListAsync();
            if (checkSchedule.Count > 0)
            {
                message = "Ongoing Maintenance Schedules need to be completed";
                status = 400;
            }
            else
            {
                message = "Successfully";
                status = 200;
                contract!.TerminalTime = DateTime.UtcNow.AddHours(7);
                contract!.TerminalContent = model.terminal_content;
                contract!.IsExpire = true;
                contract!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var schedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.ContractId.Equals(contract.Id)).ToListAsync();
                foreach (var item in schedules)
                {
                    if (item.Status != "COMPLETED" && item.Status != "MISSED")
                    {
                        item.IsDelete = true;
                    }
                }
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.CON.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "The contract have been terminated",
                        CurrentObject_Id = contract.Id,
                        UserId = contract.CustomerId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", contract.CustomerId);
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
        //public async Task<ObjectModelResponse> UpdateContract(Guid id, ContractRequest model)
        //{
        //    var contract = await _context.Contracts.Where(a => a.IsDelete == false && a.Id.Equals(id)).Select(x => new Contract
        //    {
        //        Id = id,
        //        Code = x.Code,
        //        CustomerId = model.customer_id,
        //        ContractName = model.contract_name,
        //        StartDate = model.start_date,
        //        EndDate = model.end_date,
        //        IsDelete = false,
        //        CreateDate = x.CreateDate,
        //        UpdateDate = DateTime.UtcNow.AddHours(7),
        //        ContractPrice = model.contract_price,
        //        Attachment = model.attachment,
        //        Description = model.description!,
        //        TerminalTime = null,
        //        TerminalContent = null,
        //        IsExpire = false
        //    }).FirstOrDefaultAsync();
        //    var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(contract!.Id)).ToListAsync();
        //    foreach (var item in contract_services)
        //    {
        //        _context.ContractServices.Remove(item);
        //    }
        //    var maintainSchedules = await _context.MaintenanceSchedules.Where(a => a.ContractId.Equals(contract!.Id)).ToListAsync();
        //    foreach (var item in maintainSchedules)
        //    {
        //        _context.MaintenanceSchedules.Remove(item);
        //    }
        //    foreach (var item in model.service)
        //    {
        //        var contract_service_id = Guid.NewGuid();
        //        while (true)
        //        {
        //            var contract_service_dup = await _context.ContractServices.Where(x => x.Id.Equals(contract_service_id)).FirstOrDefaultAsync();
        //            if (contract_service_dup == null)
        //            {
        //                break;
        //            }
        //            else
        //            {
        //                contract_service_id = Guid.NewGuid();
        //            }
        //        }
        //        var contract_service = new ContractService
        //        {
        //            Id = contract_service_id,
        //            FrequencyMaintain = item.frequency_maintain,
        //            ContractId = contract!.Id,
        //            ServiceId = item.service_id,
        //            IsDelete = false
        //        };
        //        _context.ContractServices.Add(contract_service);
        //    }
        //    var lastTime = model.end_date - model.start_date;
        //    var lastDay = lastTime!.Value.Days - 15;
        //    var listAgency = await _context.Agencies.Where(a => a.CustomerId.Equals(model.customer_id) && a.IsDelete == false).ToListAsync();
        //    var code_number1 = await GetLastCode1();
        //    foreach (var itemService in model.service)
        //    {
        //        var maintenanceTime = lastDay / itemService.frequency_maintain;

        //        foreach (var item in listAgency)
        //        {
        //            var maintenanceDate = model.start_date;
        //            for (int i = 1; i <= itemService.frequency_maintain; i++)
        //            {
        //                maintenanceDate = maintenanceDate!.Value.AddDays(maintenanceTime!.Value);
        //                var maintenance_id = Guid.NewGuid();
        //                while (true)
        //                {
        //                    var maintenance_dup = await _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenance_id)).FirstOrDefaultAsync();
        //                    if (maintenance_dup == null)
        //                    {
        //                        break;
        //                    }
        //                    else
        //                    {
        //                        maintenance_id = Guid.NewGuid();
        //                    }
        //                }
        //                var code1 = CodeHelper.GeneratorCode("MS", code_number1++);
        //                while (true)
        //                {
        //                    var code_dup = await _context.MaintenanceSchedules.Where(x => x.Code.Equals(code1)).FirstOrDefaultAsync();
        //                    if (code_dup == null)
        //                    {
        //                        break;
        //                    }
        //                    else
        //                    {

        //                        code1 = "MS-" + code_number1++.ToString();
        //                    }
        //                }
        //                var service = await _context.Services.Where(a => a.Id.Equals(itemService.service_id) && a.IsDelete == false).FirstOrDefaultAsync();
        //                var maintenanceSchedule = new MaintenanceSchedule
        //                {
        //                    Id = maintenance_id,
        //                    Code = code1,
        //                    AgencyId = item.Id,
        //                    CreateDate = DateTime.UtcNow.AddHours(7),
        //                    UpdateDate = DateTime.UtcNow.AddHours(7),
        //                    IsDelete = false,
        //                    Name = "Maintenance of Agency: " + item.AgencyName + ", Service: " + service!.ServiceName + ", time " + i,
        //                    Status = Enum.ScheduleStatus.SCHEDULED.ToString(),
        //                    TechnicianId = item.TechnicianId,
        //                    MaintainTime = maintenanceDate,
        //                    StartDate = null,
        //                    EndDate = null,
        //                    ContractId = contract!.Id

        //                };
        //                await _context.MaintenanceSchedules.AddAsync(maintenanceSchedule);
        //            }
        //        }
        //    }

        //    var data = new ContractResponse();

        //    var message = "Successfully";
        //    var status = 200;
        //    _context.Contracts.Update(contract!);
        //    var rs = await _context.SaveChangesAsync();
        //    if (rs > 0)
        //    {
        //        data = new ContractResponse
        //        {
        //            id = contract!.Id,
        //            code = contract.Code,
        //            contract_name = contract.ContractName,
        //            customer = new CustomerViewResponse
        //            {
        //                id = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Id).FirstOrDefault(),
        //                code = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Code).FirstOrDefault(),
        //                cus_name = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Name).FirstOrDefault(),
        //                description = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Description).FirstOrDefault(),
        //                phone = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Phone).FirstOrDefault(),
        //                address = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Address).FirstOrDefault(),
        //                mail = _context.Customers.Where(x => x.Id.Equals(contract.CustomerId)).Select(x => x.Mail).FirstOrDefault(),
        //            },
        //            start_date = contract.StartDate,
        //            end_date = contract.EndDate,
        //            is_delete = contract.IsDelete,
        //            create_date = contract.CreateDate,
        //            update_date = contract.UpdateDate,
        //            contract_price = contract.ContractPrice,
        //            description = contract.Description,
        //            is_expire = contract.IsExpire,
        //            attachment = contract.Attachment,
        //            service = _context.ContractServices.Where(x => x.ContractId.Equals(contract.Id)).Select(x => new ServiceViewResponse
        //            {
        //                id = x.ServiceId,
        //                code = x.Service!.Code,
        //                service_name = x.Service!.ServiceName,
        //                description = x.Service!.Description,
        //                frequency_maintain = _context.ContractServices.Where(a => a.ContractId.Equals(contract.Id) && a.ServiceId.Equals(x.ServiceId)).Select(a => a.FrequencyMaintain).FirstOrDefault(),
        //            }).ToList(),
        //        };
        //    }



        //    return new ObjectModelResponse(data)
        //    {
        //        Message = message,
        //        Status = status,
        //        Type = "Contract"
        //    };
        //}

        public async Task<ObjectModelResponse> CreateContract(ContractRequest model)
        {
            var contract_id = Guid.NewGuid();
            while (true)
            {
                var contract_dup = await _context.Contracts.Where(x => x.Id.Equals(contract_id)).FirstOrDefaultAsync();
                if (contract_dup == null)
                {
                    break;
                }
                else
                {
                    contract_id = Guid.NewGuid();
                }
            }
            var code_number = await GetLastCode();
            var code = CodeHelper.GeneratorCode("CON", code_number + 1);
            while (true)
            {
                var code_dup = await _context.Contracts.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                if (code_dup == null)
                {
                    break;
                }
                else
                {
                    code = "CON-" + code_number++.ToString();
                }
            }
            var contract = new Contract
            {
                Id = contract_id,
                Code = code,
                CustomerId = model.customer_id,
                ContractName = model.contract_name,
                StartDate = model.start_date!.Value.AddHours(7),
                EndDate = model.end_date!.Value.AddHours(7),
                IsDelete = false,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7),
                ContractPrice = model.contract_price,
                Attachment = model.attachment,
                Description = model.description!,
                TerminalTime = null,
                TerminalContent = null,
                IsExpire = false,
                RejectReason = null,
                IsAccepted = false,
                FrequencyMaintainTime = model.frequency_maintain_time,
            };
            var data = new ContractResponse();
            var message = "blank";
            var status = 500;
            if (model.service.Count <= 0)
            {
                status = 400;
                message = "Service must be required!";
            }
            else if (model.frequency_maintain_time <= 0)
            {
                status = 400;
                message = "Frequency maintain must be greater than zero!";
            }
            else
            {
                foreach (var item in model.service)
                {
                    var contract_service_id = Guid.NewGuid();
                    while (true)
                    {
                        var contract_service_dup = await _context.ContractServices.Where(x => x.Id.Equals(contract_service_id)).FirstOrDefaultAsync();
                        if (contract_service_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            contract_service_id = Guid.NewGuid();
                        }
                    }
                    var contract_service = new ContractService
                    {
                        Id = contract_service_id,
                        ContractId = contract.Id,
                        ServiceId = item.service_id,
                        IsDelete = false
                    };
                    _context.ContractServices.Add(contract_service);
                }

                if (model.img!.Count > 0)
                {
                    foreach (var item in model.img!)
                    {
                        var img_id = Guid.NewGuid();
                        while (true)
                        {
                            var img_dup = await _context.Images.Where(x => x.Id.Equals(img_id)).FirstOrDefaultAsync();
                            if (img_dup == null)
                            {
                                break;
                            }
                            else
                            {
                                img_id = Guid.NewGuid();
                            }
                        }
                        var imgTicket = new Image
                        {
                            Id = img_id,
                            Link = item,
                            CurrentObject_Id = contract.Id,
                            ObjectName = ObjectName.CON.ToString(),
                        };
                        await _context.Images.AddAsync(imgTicket);
                    }
                }
                var contract_name = await _context.Contracts.Where(x => x.ContractName!.Equals(contract.ContractName) && x.IsDelete == false && x.IsExpire == false).FirstOrDefaultAsync();
                if (contract_name != null)
                {
                    status = 400;
                    message = "Contract Name is already exists!";
                }
                else if (model.end_date!.Value.Date <= model.start_date!.Value.Date)
                {
                    status = 400;
                    message = "End date must be longer than start end!";
                }
                else if (model.contract_price <= 0)
                {
                    status = 400;
                    message = "Contract price must be greater than zero!";
                }
                else
                {
                    message = "Successfully";
                    status = 200;
                    await _context.Contracts.AddAsync(contract);
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.CON.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a contract need to approve!",
                        CurrentObject_Id = contract.Id,
                        UserId = contract.CustomerId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", contract.CustomerId);
                    var rs = await _context.SaveChangesAsync();
                    if (rs > 0)
                    {
                        data = new ContractResponse
                        {
                            id = contract.Id,
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
                            start_date = contract.StartDate,
                            end_date = contract.EndDate,
                            is_delete = contract.IsDelete,
                            create_date = contract.CreateDate,
                            update_date = contract.UpdateDate,
                            contract_price = contract.ContractPrice,
                            description = contract.Description,
                            is_expire = contract.IsExpire,
                            attachment = contract.Attachment,
                            reject_reason = contract.RejectReason,
                            is_accepted = contract.IsAccepted,
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
            }

            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Contract"
            };
        }

        private async Task<int> GetLastCode()
        {
            var contract = await _context.Contracts.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(contract!.Code!);
        }

        private async Task<int> GetLastCode1()
        {
            var area = await _context.MaintenanceSchedules.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(area!.Code!);
        }
    }
}
