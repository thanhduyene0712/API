using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Net.Sockets;
using UPOD.API.HubService;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using UPOD.SERVICES.Enum;
using UPOD.SERVICES.Helpers;

namespace UPOD.SERVICES.Services
{
    public interface IMaintenanceReportService
    {
        Task<ResponseModel<MaintenanceReportResponse>> GetListMaintenanceReports(PaginationRequest model, FilterStatusRequest value);
        Task<ObjectModelResponse> CreateMaintenanceReport(MaintenanceReportRequest model);
        Task<ObjectModelResponse> GetDetailsMaintenanceReport(Guid id);
        Task<ObjectModelResponse> UpdateMaintenanceReport(Guid id, MaintenanceReportRequest model);
        Task<ResponseModel<RequestCreateResponse>> ProcessMaintainReport(Guid report_id);
        Task<ResponseModel<MaintenanceReportResponse>> GetListMaintenanceReportsByCustomer(Guid id, PaginationRequest model, FilterStatusRequest value);
        Task CheckMaintenanceReport();
    }

    public class MaintenanceReportServices : IMaintenanceReportService
    {
        private readonly Database_UPODContext _context;
        private readonly IHubContext<NotifyHub> _notifyHub;
        private readonly INotificationService _notificationService;

        public MaintenanceReportServices(Database_UPODContext context, IHubContext<NotifyHub> notifyHub, INotificationService notificationService)
        {
            _context = context;
            _notifyHub = notifyHub;
            _notificationService = notificationService;
        }

        public async Task CheckMaintenanceReport()
        {
            var maintenanceReports = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.Status.Equals("PROCESSING") && a.IsProcessed == false).ToListAsync();
            var count = 0;
            if (maintenanceReports.Count > 0)
            {
                foreach (var item in maintenanceReports)
                {
                    var reportServices = await _context.MaintenanceReportServices.Where(a => a.MaintenanceReportId.Equals(item.Id) && a.IsResolved == false).ToListAsync();
                    if (reportServices.Count > 0)
                    {
                        foreach (var item1 in reportServices)
                        {
                            if (item1.RequestId != null)
                            {
                                var request = await _context.Requests.Where(a => a.IsDelete == false && a.Id.Equals(item1.RequestId)).FirstOrDefaultAsync();
                                if (request!.RequestStatus!.Equals("RESOLVED")
                                    || request!.RequestStatus!.Equals("COMPLETED")
                                    || request!.RequestStatus!.Equals("CANCELED")
                                    || request!.RequestStatus!.Equals("REJECTED"))
                                {
                                    count = count + 1;
                                }
                                if (count == reportServices.Count)
                                {
                                    count = 0;
                                    item.IsProcessed = true;
                                    await _notificationService.createNotification(new Notification
                                    {
                                        isRead = false,
                                        ObjectName = ObjectName.MR.ToString(),
                                        CreatedTime = DateTime.UtcNow.AddHours(7),
                                        NotificationContent = "You have a maintenance report need to approve!",
                                        CurrentObject_Id = item.MaintenanceScheduleId,
                                        UserId = item.CustomerId,
                                    });
                                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CustomerId);
                                    await _notificationService.createNotification(new Notification
                                    {
                                        isRead = false,
                                        ObjectName = ObjectName.MR.ToString(),
                                        CreatedTime = DateTime.UtcNow.AddHours(7),
                                        NotificationContent = "You have a maintenance report need the customer approve!",
                                        CurrentObject_Id = item.MaintenanceScheduleId,
                                        UserId = item.CreateBy,
                                    });
                                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CreateBy);
                                    var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                                    foreach (var item2 in admins)
                                    {
                                        await _notificationService.createNotification(new Notification
                                        {
                                            isRead = false,
                                            CurrentObject_Id = item.MaintenanceScheduleId,
                                            NotificationContent = "You have a maintenance report need the customer approve!",
                                            UserId = item2.Id,
                                            ObjectName = ObjectName.MR.ToString(),
                                        });
                                        await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item2.Id);
                                    }
                                }
                            }

                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        private async Task<int> GetLastCode2()
        {
            var request = await _context.Requests.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(request!.Code!);
        }
        public async Task<ResponseModel<RequestCreateResponse>> ProcessMaintainReport(Guid report_id)
        {
            var reportSchedule = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.Id.Equals(report_id)).FirstOrDefaultAsync();
            var reportServices = await _context.MaintenanceReportServices.Where(a => a.MaintenanceReportId.Equals(report_id) && a.IsResolved == false).ToListAsync();
            reportSchedule!.Status = ReportStatus.PROCESSING.ToString();
            reportSchedule!.IsProcessed = true;
            reportSchedule.UpdateDate = DateTime.UtcNow.AddHours(7);
            var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(reportSchedule.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.MR.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "Your maintenance report is processing by admin!",
                CurrentObject_Id = reportSchedule.MaintenanceScheduleId,
                UserId = customerId,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage",customerId);
            var requests = new List<RequestCreateResponse>();
            if (reportServices.Count > 0)
            {
                reportSchedule!.IsProcessed = false;
                var num = await GetLastCode2();
                foreach (var item in reportServices)
                {
                    var request_id = Guid.NewGuid();
                    while (true)
                    {
                        var request_dup = await _context.Requests.Where(x => x.Id.Equals(request_id)).FirstOrDefaultAsync();
                        if (request_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            request_id = Guid.NewGuid();
                        }
                    }
                    var code = CodeHelper.GeneratorCode("RE", num++);
                    while (true)
                    {
                        var code_dup = await _context.Requests.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                        if (code_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            code = "RE-" + num++.ToString();
                        }
                    }
                    var contracts = await _context.Contracts.Where(a => a.CustomerId.Equals(reportSchedule.CustomerId)).ToListAsync();
                    Guid? contract_id = null;
                    foreach (var item1 in contracts)
                    {
                        var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(item1.Id)).ToListAsync();
                        foreach (var item2 in contract_services)
                        {
                            if (item2.ServiceId.Equals(item.ServiceId))
                            {
                                contract_id = item2.ContractId;
                            }
                        }
                    }
                    var agency = await _context.Agencies.Where(a => a.Id.Equals(reportSchedule!.AgencyId)).FirstOrDefaultAsync();
                    var area = await _context.Areas.Where(a => a.Id.Equals(agency!.AreaId)).FirstOrDefaultAsync();
                    var service = await _context.Services.Where(a => a.Id.Equals(item!.ServiceId)).FirstOrDefaultAsync();
                    var technicians = new List<TechnicianOfRequestResponse>();
                    DateTime date = DateTime.UtcNow.AddHours(7);
                    var total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
                    && a.Technician.AreaId.Equals(area!.Id)
                    && a.Technician.IsBusy == false
                    && a.Technician.IsDelete == false).ToListAsync();
                    if (total.Count > 0)
                    {
                        total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
                        && a.Technician.AreaId.Equals(area!.Id)
                        && a.Technician.IsBusy == false
                        && a.Technician.IsDelete == false).ToListAsync();
                        foreach (var item3 in total)
                        {
                            date = date.AddDays((-date.Day) + 1).Date;
                            var requestsOfTechnician = await _context.Requests.Where(a => a.IsDelete == false
                            && a.CurrentTechnicianId.Equals(item3.TechnicianId)
                            && a.RequestStatus.Equals("COMPLETED")
                            && a.CreateDate!.Value.Date >= date
                            && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                            var count = requestsOfTechnician.Count;
                            technicians.Add(new TechnicianOfRequestResponse
                            {
                                id = item3.TechnicianId,
                                code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item3.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                                technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item3.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                                number_of_requests = count,
                            });
                        }
                    }
                    else
                    {
                        total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
                        && a.Technician.IsBusy == false
                        && a.Technician.IsDelete == false).ToListAsync();
                        foreach (var item3 in total)
                        {
                            date = date.AddDays((-date.Day) + 1).Date;
                            var requestsOfTechnician = await _context.Requests.Where(a => a.IsDelete == false
                            && a.CurrentTechnicianId.Equals(item3.TechnicianId)
                            && a.RequestStatus.Equals("COMPLETED")
                            && a.CreateDate!.Value.Date >= date
                            && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                            var count = requestsOfTechnician.Count;
                            technicians.Add(new TechnicianOfRequestResponse
                            {
                                id = item3.TechnicianId,
                                code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item3.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                                technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item3.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                                number_of_requests = count,
                            });
                        }

                    }
                    technicians.OrderBy(a => a.number_of_requests).ToList();
                    item!.Created = true;
                    item!.RequestId = request_id!;
                    var requestNew = new Request
                    {
                        Id = request_id,
                        Code = code,
                        RequestName = "Request auto: " + reportSchedule.Code,
                        CustomerId = reportSchedule.CustomerId,
                        ServiceId = item.ServiceId,
                        AgencyId = reportSchedule.AgencyId,
                        RequestDesciption = item.Description,
                        RequestStatus = ProcessStatus.PREPARING.ToString(),
                        ReasonReject = null,
                        CreateDate = DateTime.UtcNow.AddHours(7),
                        UpdateDate = DateTime.UtcNow.AddHours(7),
                        IsDelete = false,
                        Feedback = null,
                        Rating = 0,
                        CurrentTechnicianId = technicians.Select(a => a.id).FirstOrDefault(),
                        StartTime = DateTime.UtcNow.AddHours(7),
                        EndTime = null,
                        AdminId = null,
                        ContractId = contract_id,
                        IsSystem = true,
                    };
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.RE.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a new request need to resolve!",
                        CurrentObject_Id = requestNew.Id,
                        UserId = requestNew.CurrentTechnicianId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", requestNew.CurrentTechnicianId);
                    await _context.Requests.AddAsync(requestNew);
                    requests.Add(new RequestCreateResponse
                    {
                        id = requestNew.Id,
                        code = requestNew.Code,
                        request_name = requestNew.RequestName,
                        request_description = requestNew.RequestDesciption,
                        phone = _context.Agencies.Where(x => x.Id.Equals(requestNew.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(requestNew.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                        customer_name = _context.Customers.Where(x => x.Id.Equals(requestNew.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                        service_name = _context.Services.Where(x => x.Id.Equals(requestNew.ServiceId)).Select(x => x.ServiceName).FirstOrDefault(),
                        technician_name = _context.Technicians.Where(x => x.Id.Equals(requestNew.CurrentTechnicianId)).Select(x => x.TechnicianName).FirstOrDefault(),
                    });
                }
            }
            await _context.SaveChangesAsync();
            return new ResponseModel<RequestCreateResponse>(requests)
            {
                Type = "Requests",
                Total = requests.Count,

            };
        }
        public async Task<ResponseModel<MaintenanceReportResponse>> GetListMaintenanceReports(PaginationRequest model, FilterStatusRequest value)
        {
            var total = await _context.MaintenanceReports.Where(a => a.IsDelete == false).ToListAsync();
            var maintenanceReports = new List<MaintenanceReportResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.MaintenanceReports.Where(a => a.IsDelete == false).ToListAsync();
                maintenanceReports = await _context.MaintenanceReports.Where(a => a.IsDelete == false).Select(a => new MaintenanceReportResponse
                {
                    id = a.Id,
                    name = a.Name,
                    code = a.Code,
                    update_date = a.UpdateDate,
                    create_date = a.CreateDate,
                    is_delete = a.IsDelete,
                    status = a.Status,
                    is_processed = a.IsProcessed,
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                    },

                    maintenance_schedule = new MaintenanceReportViewResponse
                    {
                        id = a.MaintenanceScheduleId,
                        code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                        description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                        maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                        sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                    },
                    create_by = new TechnicianViewResponse
                    {
                        id = a.CreateBy,
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(a.Id)).Select(a => new ServiceReportResponse
                    {
                        report_service_id = a.Id,
                        service_id = a.ServiceId,
                        code = a.Service!.Code,
                        service_name = a.Service!.ServiceName,
                        description = a.Description,
                        created = a.Created,
                        is_resolved = a.IsResolved,
                        request_id = a.RequestId,
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
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
                var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.MaintenanceReports.Where(a => a.IsDelete == false
                && (a.Status!.Contains(value.status!)
                && (a.Name!.Contains(value.search!)
                || a.AgencyId!.Equals(agency_name)
                || a.CustomerId!.Equals(customer_name)
                || a.Code!.Contains(value.search!)))).ToListAsync();
                maintenanceReports = await _context.MaintenanceReports.Where(a => a.IsDelete == false
                && (a.Status!.Contains(value.status!)
                && (a.Name!.Contains(value.search!)
                || a.AgencyId!.Equals(agency_name)
                || a.CustomerId!.Equals(customer_name)
                || a.Code!.Contains(value.search!)))).Select(a => new MaintenanceReportResponse
                {
                    id = a.Id,
                    name = a.Name,
                    code = a.Code,
                    update_date = a.UpdateDate,
                    create_date = a.CreateDate,
                    is_delete = a.IsDelete,
                    status = a.Status,
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                    },

                    maintenance_schedule = new MaintenanceReportViewResponse
                    {
                        id = a.MaintenanceScheduleId,
                        code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                        description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                        maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                        sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                    },
                    create_by = new TechnicianViewResponse
                    {
                        id = a.CreateBy,
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(a.Id)).Select(a => new ServiceReportResponse
                    {
                        report_service_id = a.Id,
                        service_id = a.ServiceId,
                        code = a.Service!.Code,
                        service_name = a.Service!.ServiceName,
                        description = a.Description,
                        created = a.Created,
                        is_resolved = a.IsResolved,
                        request_id = a.RequestId,
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            return new ResponseModel<MaintenanceReportResponse>(maintenanceReports)
            {
                Total = total.Count,
                Type = "MaintenanceReports"
            };

        }
        public async Task<ResponseModel<MaintenanceReportResponse>> GetListMaintenanceReportsByCustomer(Guid id, PaginationRequest model, FilterStatusRequest value)
        {
            var total = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).ToListAsync();
            var maintenanceReports = new List<MaintenanceReportResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).ToListAsync();
                maintenanceReports = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.CustomerId.Equals(id)).Select(a => new MaintenanceReportResponse
                {
                    id = a.Id,
                    name = a.Name,
                    code = a.Code,
                    update_date = a.UpdateDate,
                    create_date = a.CreateDate,
                    is_delete = a.IsDelete,
                    status = a.Status,
                    is_processed = a.IsProcessed,
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                    },

                    maintenance_schedule = new MaintenanceReportViewResponse
                    {
                        id = a.MaintenanceScheduleId,
                        code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                        description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                        maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                        sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                    },
                    create_by = new TechnicianViewResponse
                    {
                        id = a.CreateBy,
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(a.Id)).Select(a => new ServiceReportResponse
                    {
                        report_service_id = a.Id,
                        service_id = a.ServiceId,
                        code = a.Service!.Code,
                        service_name = a.Service!.ServiceName,
                        description = a.Description,
                        created = a.Created,
                        is_resolved = a.IsResolved,
                        request_id = a.RequestId,
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
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
                var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.MaintenanceReports.Where(a => a.IsDelete == false
                && a.CustomerId.Equals(id)
                && (a.Status!.Contains(value.status!)
                && (a.Name!.Contains(value.search!)
                || a.AgencyId!.Equals(agency_name)
                || a.CustomerId!.Equals(customer_name)
                || a.Code!.Contains(value.search!)))).ToListAsync();
                maintenanceReports = await _context.MaintenanceReports.Where(a => a.IsDelete == false
                && a.CustomerId.Equals(id)
                && (a.Status!.Contains(value.status!)
                && (a.Name!.Contains(value.search!)
                || a.AgencyId!.Equals(agency_name)
                || a.CustomerId!.Equals(customer_name)
                || a.Code!.Contains(value.search!)))).Select(a => new MaintenanceReportResponse
                {
                    id = a.Id,
                    name = a.Name,
                    code = a.Code,
                    update_date = a.UpdateDate,
                    create_date = a.CreateDate,
                    is_delete = a.IsDelete,
                    status = a.Status,
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                    },

                    maintenance_schedule = new MaintenanceReportViewResponse
                    {
                        id = a.MaintenanceScheduleId,
                        code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                        description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                        maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                        sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                    },
                    create_by = new TechnicianViewResponse
                    {
                        id = a.CreateBy,
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(a.Id)).Select(a => new ServiceReportResponse
                    {
                        report_service_id = a.Id,
                        service_id = a.ServiceId,
                        code = a.Service!.Code,
                        service_name = a.Service!.ServiceName,
                        description = a.Description,
                        created = a.Created,
                        is_resolved = a.IsResolved,
                        request_id = a.RequestId,
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            return new ResponseModel<MaintenanceReportResponse>(maintenanceReports)
            {
                Total = total.Count,
                Type = "MaintenanceReports"
            };

        }
        public async Task<ObjectModelResponse> GetDetailsMaintenanceReport(Guid id)
        {
            var maintenanceReports = new MaintenanceReportResponse();
            maintenanceReports = await _context.MaintenanceReports.Where(a => a.IsDelete == false && a.MaintenanceScheduleId.Equals(id)).Select(a => new MaintenanceReportResponse
            {
                id = a.Id,
                name = a.Name,
                code = a.Code,
                update_date = a.UpdateDate,
                create_date = a.CreateDate,
                is_delete = a.IsDelete,
                status = a.Status,
                is_processed = a.IsProcessed,
                agency = new AgencyViewResponse
                {
                    id = a.AgencyId,
                    code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                    agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                    phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                    address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                },
                customer = new CustomerViewResponse
                {
                    id = a.CustomerId,
                    code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                    cus_name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                    address = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                    mail = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                    phone = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                    description = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                },

                maintenance_schedule = new MaintenanceReportViewResponse
                {
                    id = a.MaintenanceScheduleId,
                    code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                    description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                    maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                    sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(a.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                },
                create_by = new TechnicianViewResponse
                {
                    id = a.CreateBy,
                    code = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                    phone = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                    email = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                    tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                },
                service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(a.Id)).Select(a => new ServiceReportResponse
                {
                    report_service_id = a.Id,
                    service_id = a.ServiceId,
                    code = a.Service!.Code,
                    service_name = a.Service!.ServiceName,
                    description = a.Description,
                    created = a.Created,
                    request_id = a.RequestId,
                    is_resolved = a.IsResolved,
                    img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName!.Equals(ObjectName.MRS.ToString())).Select(a => a.Link).ToList()!
                }).ToList(),
            }).FirstOrDefaultAsync();

            return new ObjectModelResponse(maintenanceReports!)
            {
                Type = "MaintenanceReport"
            };

        }
        public async Task<ObjectModelResponse> UpdateMaintenanceReport(Guid id, MaintenanceReportRequest model)
        {
            var maintenanceReport = await _context.MaintenanceReports.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var maintainSchedule = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Id.Equals(model.maintenance_schedule_id)).FirstOrDefaultAsync();
            var contract = await _context.Contracts.Where(a => a.IsDelete == false && a.IsAccepted == true && a.IsExpire == false && a.Id.Equals(maintainSchedule!.ContractId)).FirstOrDefaultAsync();
            var count = new List<ContractService>();
            var message = "blank";
            var status = 500;
            var services = await _context.ContractServices.Where(x => x.ContractId.Equals(contract!.Id)
            && x.Contract.IsDelete == false && x.Contract.IsExpire == false && x.Contract.IsAccepted == true && x.IsDelete == false
            && (x.Contract.StartDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date
            && x.Contract.EndDate!.Value.Date >= DateTime.UtcNow.AddHours(7).Date)).ToListAsync();
            foreach (var item1 in services)
            {
                count.Add(item1);
            }
            if (model.service.Count != count.Count)
            {
                message = "You need to add all services of this contract";
                status = 400;
            }
            else
            {
                message = "Successfully";
                status = 200;
                maintenanceReport!.Name = model.name;
                maintenanceReport!.MaintenanceScheduleId = model.maintenance_schedule_id;
                maintenanceReport!.UpdateDate = DateTime.UtcNow.AddHours(7);
                maintenanceReport!.Status = ReportStatus.PENDING.ToString();
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MR.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "The technician have been updated the maintenance report!",
                        CurrentObject_Id = maintenanceReport.MaintenanceScheduleId,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                }
                var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(maintainSchedule!.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.MS.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "The technician have been updated the maintenance report!",
                    CurrentObject_Id = maintenanceReport.MaintenanceScheduleId,
                    UserId = customerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                var report_service_removes = await _context.MaintenanceReportServices.Where(a => a.MaintenanceReportId.Equals(maintenanceReport.Id)).ToListAsync();
                foreach (var item in report_service_removes)
                {
                    var imgs = await _context.Images.Where(a => a.CurrentObject_Id.Equals(item.Id) && a.ObjectName.Equals(ObjectName.MRS.ToString())).ToListAsync();
                    foreach (var item1 in imgs)
                    {
                        _context.Images.Remove(item1);
                    }
                    _context.MaintenanceReportServices.Remove(item);
                }
                foreach (var item1 in model.service)
                {

                    var maintenanceReportService_id = Guid.NewGuid();
                    while (true)
                    {
                        var maintenanceReportService_dup = await _context.MaintenanceReportServices.Where(x => x.Id.Equals(maintenanceReportService_id)).FirstOrDefaultAsync();
                        if (maintenanceReportService_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            maintenanceReportService_id = Guid.NewGuid();
                        }
                    }
                    var maintenanceReportService = new MaintenanceReportService
                    {
                        Id = maintenanceReportService_id,
                        Description = item1.Description,
                        MaintenanceReportId = maintenanceReport.Id,
                        ServiceId = item1.service_id,
                        Created = false,
                        IsResolved = item1.is_resolved,
                        RequestId = null,
                    };
                    if (item1.img!.Count > 0)
                    {
                        foreach (var item2 in item1.img!)
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

                            var imgMaintenanceReportService = new Image
                            {
                                Id = img_id,
                                Link = item2,
                                CurrentObject_Id = maintenanceReportService.Id,
                                ObjectName = ObjectName.MRS.ToString(),
                            };
                            await _context.Images.AddAsync(imgMaintenanceReportService);
                        }
                    }
                    await _context.MaintenanceReportServices.AddAsync(maintenanceReportService);

                }
            }
            var data = new MaintenanceReportResponse();
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new MaintenanceReportResponse

                {
                    id = maintenanceReport!.Id,
                    name = maintenanceReport.Name,
                    code = maintenanceReport.Code,
                    update_date = maintenanceReport.UpdateDate,
                    create_date = maintenanceReport.CreateDate,
                    is_delete = maintenanceReport.IsDelete,
                    status = maintenanceReport.Status,
                    agency = new AgencyViewResponse
                    {
                        id = maintenanceReport.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = maintenanceReport.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                    },

                    maintenance_schedule = new MaintenanceReportViewResponse
                    {
                        id = maintenanceReport.MaintenanceScheduleId,
                        code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                        description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                        maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                        sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                    },
                    create_by = new TechnicianViewResponse
                    {
                        id = maintenanceReport.CreateBy,
                        code = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(maintenanceReport.Id)).Select(a => new ServiceReportResponse
                    {
                        report_service_id = a.Id,
                        service_id = a.ServiceId,
                        code = a.Service!.Code,
                        service_name = a.Service!.ServiceName,
                        description = a.Description,
                        created = a.Created,
                        request_id = a.RequestId,
                        is_resolved = a.IsResolved,
                        img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName.Equals(ObjectName.MRS.ToString())).Select(a => a.Link).ToList()!
                    }).ToList(),
                };
            }
            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "MaintenanceReport"
            };
        }
        public async Task<ObjectModelResponse> CreateMaintenanceReport(MaintenanceReportRequest model)
        {
            var data = new MaintenanceReportResponse();
            var maintenanceReport_id = Guid.NewGuid();
            while (true)
            {
                var maintenanceReport_dup = await _context.MaintenanceReports.Where(x => x.Id.Equals(maintenanceReport_id)).FirstOrDefaultAsync();
                if (maintenanceReport_dup == null)
                {
                    break;
                }
                else
                {
                    maintenanceReport_id = Guid.NewGuid();
                }
            }
            var agencyId = _context.MaintenanceSchedules.Where(a => a.Id.Equals(model.maintenance_schedule_id)).Select(a => a.AgencyId).FirstOrDefault();
            var num = await GetLastCode();
            var code = CodeHelper.GeneratorCode("MR", num + 1);
            while (true)
            {
                var code_dup = await _context.MaintenanceReports.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                if (code_dup == null)
                {
                    break;
                }
                else
                {
                    code = "MR-" + num++.ToString();
                }
            }
            var maintenanceReport = new MaintenanceReport
            {
                Id = maintenanceReport_id,
                Code = code,
                Name = model.name,
                IsDelete = false,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7),
                AgencyId = agencyId,
                CustomerId = _context.Agencies.Where(a => a.Id.Equals(agencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                CreateBy = _context.MaintenanceSchedules.Where(a => a.Id.Equals(model.maintenance_schedule_id)).Select(a => a.TechnicianId).FirstOrDefault(),
                MaintenanceScheduleId = model.maintenance_schedule_id,
                IsProcessed = false,
                Status = ReportStatus.PENDING.ToString(),
            };
            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Id.Equals(model.maintenance_schedule_id)).FirstOrDefaultAsync();
            var contract = await _context.Contracts.Where(a => a.IsDelete == false && a.IsAccepted == true && a.IsExpire == false && a.Id.Equals(maintenanceSchedule!.ContractId)).FirstOrDefaultAsync();
            var count = new List<ContractService>();
            var message = "blank";
            var status = 500;
            var services = await _context.ContractServices.Where(x => x.ContractId.Equals(contract!.Id)
            && x.Contract.IsDelete == false && x.Contract.IsExpire == false && x.Contract.IsAccepted == true && x.IsDelete == false
            && (x.Contract.StartDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date
            && x.Contract.EndDate!.Value.Date >= DateTime.UtcNow.AddHours(7).Date)).ToListAsync();
            foreach (var item1 in services)
            {
                count.Add(item1);
            }
            if (model.service.Count != count.Count)
            {
                message = "You need to add all services of this contract";
                status = 400;
            }
            else
            {
                message = "Successfully";
                status = 200;
                var maintenanceScheduleStatus = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(model.maintenance_schedule_id)).FirstOrDefaultAsync();
                maintenanceScheduleStatus!.Status = ScheduleStatus.COMPLETED.ToString();
                maintenanceScheduleStatus!.EndDate = DateTime.UtcNow.AddHours(7);
                await _context.MaintenanceReports.AddAsync(maintenanceReport);
                var technician = await _context.Technicians.Where(x => x.Id.Equals(maintenanceReport!.CreateBy)).FirstOrDefaultAsync();
                maintenanceReport!.Status = ReportStatus.PENDING.ToString();
                technician!.IsBusy = false;
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a maintenance schedule completed!",
                        CurrentObject_Id = maintenanceScheduleStatus.Id,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MR.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a new maintenance report!",
                        CurrentObject_Id = maintenanceScheduleStatus.Id,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                }
                var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(maintenanceScheduleStatus.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.MS.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a maintenance schedule completed!",
                    CurrentObject_Id = maintenanceScheduleStatus.Id,
                    UserId = customerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.MR.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a new maintenance report!",
                    CurrentObject_Id = maintenanceScheduleStatus.Id,
                    UserId = customerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                foreach (var item in model.service)
                {
                    var maintenanceReportService_id = Guid.NewGuid();
                    while (true)
                    {
                        var maintenanceReportService_dup = await _context.MaintenanceReportServices.Where(x => x.Id.Equals(maintenanceReportService_id)).FirstOrDefaultAsync();
                        if (maintenanceReportService_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            maintenanceReportService_id = Guid.NewGuid();
                        }
                    }
                    var maintenanceReportService = new MaintenanceReportService
                    {
                        Id = maintenanceReportService_id,
                        Description = item.Description,
                        MaintenanceReportId = maintenanceReport.Id,
                        ServiceId = item.service_id,
                        Created = false,
                        IsResolved = item.is_resolved,
                        RequestId = null,
                    };
                    if (item.img!.Count > 0)
                    {
                        foreach (var item1 in item.img!)
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

                            var imgMaintenanceReportService = new Image
                            {
                                Id = img_id,
                                Link = item1,
                                CurrentObject_Id = maintenanceReportService.Id,
                                ObjectName = ObjectName.MRS.ToString(),
                            };
                            await _context.Images.AddAsync(imgMaintenanceReportService);
                        }
                    }
                    await _context.MaintenanceReportServices.AddAsync(maintenanceReportService);
                }
            }

            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new MaintenanceReportResponse

                {
                    id = maintenanceReport.Id,
                    name = maintenanceReport.Name,
                    code = maintenanceReport.Code,
                    update_date = maintenanceReport.UpdateDate,
                    create_date = maintenanceReport.CreateDate,
                    is_delete = maintenanceReport.IsDelete,
                    status = maintenanceReport.Status,
                    agency = new AgencyViewResponse
                    {
                        id = maintenanceReport.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(maintenanceReport.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = maintenanceReport.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        cus_name = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        address = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                        mail = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Mail).FirstOrDefault(),
                        phone = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                        description = _context.Customers.Where(x => x.Id.Equals(maintenanceReport.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                    },

                    maintenance_schedule = new MaintenanceReportViewResponse
                    {
                        id = maintenanceReport.MaintenanceScheduleId,
                        code = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.Code).FirstOrDefault(),
                        description = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.Description).FirstOrDefault(),
                        maintain_time = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.MaintainTime).FirstOrDefault(),
                        sche_name = _context.MaintenanceSchedules.Where(x => x.Id.Equals(maintenanceReport.MaintenanceScheduleId)).Select(a => a.Name).FirstOrDefault(),
                    },
                    create_by = new TechnicianViewResponse
                    {
                        id = maintenanceReport.CreateBy,
                        code = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.Code).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.Email).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceReport.CreateBy)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    service = _context.MaintenanceReportServices.Where(x => x.MaintenanceReportId.Equals(maintenanceReport.Id)).Select(a => new ServiceReportResponse
                    {
                        report_service_id = a.Id,
                        service_id = a.ServiceId,
                        code = a.Service!.Code,
                        service_name = a.Service!.ServiceName,
                        description = a.Description,
                        created = a.Created,
                        request_id = a.RequestId,
                        is_resolved = a.IsResolved,
                        img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName.Equals(ObjectName.MRS.ToString())).Select(a => a.Link).ToList()!
                    }).ToList(),
                };
            }
            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "MaintenanceReport"
            };
        }


        private async Task<int> GetLastCode()
        {
            var maintenanceReport = await _context.MaintenanceReports.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(maintenanceReport!.Code!);
        }
    }
}
