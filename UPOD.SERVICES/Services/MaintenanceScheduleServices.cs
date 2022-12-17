using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;
using UPOD.API.HubService;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using UPOD.SERVICES.Enum;

namespace UPOD.SERVICES.Services
{
    public interface IMaintenanceScheduleService
    {
        Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedules(PaginationRequest model, FilterStatusRequest value);
        Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedulesByCustomer(Guid id, PaginationRequest model, FilterStatusRequest value);
        Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedulesTechnician(PaginationRequest model, Guid id, FilterStatusRequest value);
        Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedulesAgency(PaginationRequest model, Guid id, FilterStatusRequest value);
        Task<ObjectModelResponse> UpdateMaintenanceSchedule(Guid id, MaintenanceScheduleRequest model);
        Task<ObjectModelResponse> AcceptMaintenanceSchedule(Guid id, Guid tech_id);
        Task<ObjectModelResponse> MaintainingSchedule(Guid id, Guid tech_id);
        Task<ObjectModelResponse> DisableMaintenanceSchedule(Guid id);
        Task<ObjectModelResponse> MaintenanceScheduleDetails(Guid id);
        Task<ResponseModel<TechnicianOfRequestResponse>> GetTechnicianSchedule(Guid id);
        Task SetMaintenanceSchedulesNotify();
        Task SetMaintenanceSchedulesNotifyWarning();
        Task SetMaintenanceSchedulesNotifyMissing();
    }

    public class MaintenanceScheduleServices : IMaintenanceScheduleService
    {
        private readonly Database_UPODContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotifyHub> _notifyHub;

        public MaintenanceScheduleServices(Database_UPODContext context, INotificationService notificationService, IHubContext<NotifyHub> notifyHub)
        {
            _context = context;
            _notificationService = notificationService;
            _notifyHub = notifyHub;
        }
        public async Task<ResponseModel<TechnicianOfRequestResponse>> GetTechnicianSchedule(Guid id)
        {

            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var agency = await _context.Agencies.Where(a => a.Id.Equals(maintenanceSchedule!.AgencyId)).FirstOrDefaultAsync();
            var contractService = await _context.ContractServices.Where(a => a.ContractId.Equals(maintenanceSchedule!.ContractId)).Select(a => a.Service).ToListAsync();
            var area = await _context.Areas.Where(a => a.Id.Equals(agency!.AreaId)).FirstOrDefaultAsync();
            var technicians = new List<TechnicianOfRequestResponse>();
            var technicianList = new List<TechnicianOfRequestResponse>();
            DateTime date = DateTime.UtcNow.AddHours(7);
            var total = await _context.Skills.Where(a => a.Technician.AreaId.Equals(area!.Id)
            && a.Technician.IsBusy == false
            && a.Technician.IsDelete == false).ToListAsync();
            if (total.Count > 0)
            {
                foreach (var item in contractService)
                {
                    total = await _context.Skills.Where(a => a.Technician.AreaId.Equals(area!.Id)
                    && a.ServiceId.Equals(item!.Id)
                    && a.Technician.IsBusy == false
                    && a.Technician.IsDelete == false).ToListAsync();
                    foreach (var item1 in total)
                    {
                        date = date.AddDays((-date.Day) + 1).Date;
                        var requests = await _context.Requests.Where(a => a.IsDelete == false
                        && a.CurrentTechnicianId.Equals(item1.TechnicianId)
                        && a.RequestStatus.Equals("COMPLETED")
                        && a.CreateDate!.Value.Date >= date
                        && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                        var count = requests.Count;
                        technicians.Add(new TechnicianOfRequestResponse
                        {
                            id = item1.TechnicianId,
                            code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item1.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item1.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                            number_of_requests = count,
                            area = area!.AreaName,
                            skills = _context.Skills.Where(a => a.TechnicianId.Equals(item1!.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                        });
                    }
                }

            }
            else
            {
                foreach (var item in contractService)
                {
                    total = await _context.Skills.Where(a => a.Technician.IsBusy == false
                    && a.ServiceId.Equals(item!.Id)
                    && a.Technician.IsDelete == false).ToListAsync();
                    foreach (var item1 in total)
                    {
                        date = date.AddDays((-date.Day) + 1).Date;
                        var requests = await _context.Requests.Where(a => a.IsDelete == false
                        && a.CurrentTechnicianId.Equals(item1.TechnicianId)
                        && a.RequestStatus.Equals("COMPLETED")
                        && a.CreateDate!.Value.Date >= date
                        && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                        var count = requests.Count;
                        technicians.Add(new TechnicianOfRequestResponse
                        {
                            id = item1.TechnicianId,
                            code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item1.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item1.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                            number_of_requests = count,
                            area = area!.AreaName,
                            skills = _context.Skills.Where(a => a.TechnicianId.Equals(item1!.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                        });
                    }
                }

            }
            technicianList = technicians.OrderBy(x => x.number_of_requests).Distinct().ToList();
            return new ResponseModel<TechnicianOfRequestResponse>(technicianList)
            {
                Total = total.Count,
                Type = "Technicians"
            };
        }
        public async Task<ObjectModelResponse> AcceptMaintenanceSchedule(Guid id, Guid tech_id)
        {
            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var technician = await _context.Technicians.Where(a => a.Id.Equals(maintenanceSchedule!.TechnicianId) && a.IsDelete == false).FirstOrDefaultAsync();
            var data = new MaintenanceScheduleResponse();
            var message = "blank";
            var status = 500;
            var request = await _context.Requests.Where(a => a.CurrentTechnicianId.Equals(tech_id) && (a.RequestStatus!.Equals("RESOLVING"))).FirstOrDefaultAsync();
            var maintain = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.TechnicianId.Equals(tech_id) && a.Status!.Equals("MAINTAINING")).FirstOrDefaultAsync();
            if (request != null || maintain != null)
            {
                message = "You have a request or maintenance schedule that needs to solve";
                status = 400;
            }
            else if (maintenanceSchedule!.TechnicianId.Equals(tech_id))
            {
                message = "Successfully";
                status = 200;
                technician!.IsBusy = true;
                maintenanceSchedule!.Status = ScheduleStatus.PREPARING.ToString();
                maintenanceSchedule!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "The technician accept the maintenance schedule!",
                        CurrentObject_Id = maintenanceSchedule!.Id,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                }
                var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.MS.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "The technician accept the maintenance schedule!",
                    CurrentObject_Id = maintenanceSchedule!.Id,
                    UserId = customerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new MaintenanceScheduleResponse
                    {
                        id = maintenanceSchedule.Id,
                        code = maintenanceSchedule.Code,
                        name = maintenanceSchedule.Name,
                        description = maintenanceSchedule.Description,
                        is_delete = maintenanceSchedule.IsDelete,
                        create_date = maintenanceSchedule.CreateDate,
                        update_date = maintenanceSchedule.UpdateDate,
                        maintain_time = maintenanceSchedule.MaintainTime,
                        status = maintenanceSchedule.Status,
                        start_time = maintenanceSchedule.StartDate,
                        end_time = maintenanceSchedule.EndDate,
                        technician = new TechnicianViewResponse
                        {
                            id = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            email = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        },
                        agency = new AgencyViewResponse
                        {
                            id = maintenanceSchedule.AgencyId,
                            code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                            agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                            phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Telephone).FirstOrDefault()
                        }
                    };
                }
            }
            else
            {
                message = "The technician does not own the maintenance schedule";
                status = 400;
            }

            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "MaintenanceSchedule"
            };
        }
        public async Task<ObjectModelResponse> MaintainingSchedule(Guid id, Guid tech_id)
        {
            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var technician = await _context.Technicians.Where(a => a.Id.Equals(maintenanceSchedule!.TechnicianId) && a.IsDelete == false).FirstOrDefaultAsync();

            var data = new MaintenanceScheduleResponse();
            var message = "blank";
            var status = 500;
            var request = await _context.Requests.Where(a => a.CurrentTechnicianId.Equals(tech_id) && (a.RequestStatus!.Equals("RESOLVING"))).FirstOrDefaultAsync();
            var maintain = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.TechnicianId.Equals(tech_id) && a.Status!.Equals("MAINTAINING")).FirstOrDefaultAsync();
            if (request != null || maintain != null)
            {
                message = "You have a request or maintenance schedule that needs to solve";
                status = 400;
            }
            else if (maintenanceSchedule!.TechnicianId.Equals(tech_id))
            {
                message = "Successfully";
                status = 200;
                technician!.IsBusy = true;
                maintenanceSchedule!.Status = ScheduleStatus.MAINTAINING.ToString();
                maintenanceSchedule.StartDate = DateTime.UtcNow.AddHours(7);
                maintenanceSchedule!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a maintenance schedule is maintaining!",
                        CurrentObject_Id = maintenanceSchedule!.Id,
                        UserId = item.Id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
                }
                var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.MS.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a maintenance schedule is maintaining!",
                    CurrentObject_Id = maintenanceSchedule!.Id,
                    UserId = customerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new MaintenanceScheduleResponse
                    {
                        id = maintenanceSchedule.Id,
                        code = maintenanceSchedule.Code,
                        name = maintenanceSchedule.Name,
                        description = maintenanceSchedule.Description,
                        is_delete = maintenanceSchedule.IsDelete,
                        create_date = maintenanceSchedule.CreateDate,
                        update_date = maintenanceSchedule.UpdateDate,
                        maintain_time = maintenanceSchedule.MaintainTime,
                        status = maintenanceSchedule.Status,
                        start_time = maintenanceSchedule.StartDate,
                        end_time = maintenanceSchedule.EndDate,
                        technician = new TechnicianViewResponse
                        {
                            id = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            email = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        },
                        agency = new AgencyViewResponse
                        {
                            id = maintenanceSchedule.AgencyId,
                            code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                            agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                            phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Telephone).FirstOrDefault()
                        }
                    };
                }
            }
            else
            {
                message = "The technician does not own the maintenance schedule";
                status = 400;
            }

            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "MaintenanceSchedule"
            };
        }
        public async Task<ObjectModelResponse> MaintenanceScheduleDetails(Guid id)
        {
            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var data = new MaintenanceScheduleResponse();
            TimeSpan? durationTime;
            var result = "";
            if (maintenanceSchedule!.EndDate != null && maintenanceSchedule.StartDate != null)
            {
                durationTime = maintenanceSchedule!.EndDate.Value.Subtract(maintenanceSchedule!.StartDate.Value);
                result = string.Format("{0:D2}:{1:D2}:{2:D2}", durationTime.Value.Days, durationTime.Value.Hours, durationTime.Value.Minutes);
            }
            else
            {
                durationTime = null;
            }
            data = new MaintenanceScheduleResponse
            {
                id = maintenanceSchedule!.Id,
                code = maintenanceSchedule.Code,
                name = maintenanceSchedule.Name,
                description = maintenanceSchedule.Description,
                is_delete = maintenanceSchedule.IsDelete,
                create_date = maintenanceSchedule.CreateDate,
                update_date = maintenanceSchedule.UpdateDate,
                start_time = maintenanceSchedule.StartDate,
                end_time = maintenanceSchedule.EndDate,
                maintain_time = maintenanceSchedule.MaintainTime,
                status = maintenanceSchedule.Status,
                duration_time = result,
                contract = new ContractViewResponse
                {
                    id = _context.Contracts.Where(x => x.Id.Equals(maintenanceSchedule.ContractId)).Select(a => a.Id).FirstOrDefault(),
                    name = _context.Contracts.Where(x => x.Id.Equals(maintenanceSchedule.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                    code = _context.Contracts.Where(x => x.Id.Equals(maintenanceSchedule.ContractId)).Select(a => a.Code).FirstOrDefault(),
                },
                technician = new TechnicianViewResponse
                {
                    id = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                    phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                    email = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                    code = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                    tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                },
                agency = new AgencyViewResponse
                {
                    id = maintenanceSchedule.AgencyId,
                    code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                    agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                    address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                    phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                    manager_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                },
                customer = new CustomerViewResponse
                {
                    id = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                    code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                    cus_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                    address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                    phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                }
            };


            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "MaintenanceSchedule"
            };
        }
        public async Task SetMaintenanceSchedulesNotify()
        {
            var todaySchedules = await _context.MaintenanceSchedules.Where(a => a.MaintainTime!.Value.Date <= DateTime.UtcNow.AddHours(7).AddDays(2).Date && a.IsDelete == false && a.Status.Equals("SCHEDULED")).ToListAsync();
            if (todaySchedules.Count > 0)
            {
                foreach (var item in todaySchedules)
                {
                    item.UpdateDate = DateTime.UtcNow.AddHours(7);
                    item.Status = ScheduleStatus.NOTIFIED.ToString();
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        CurrentObject_Id = item.Id,
                        NotificationContent = "You have a maintenance schedule coming up!",
                        UserId = item.TechnicianId,
                        ObjectName = ObjectName.MS.ToString(),
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.TechnicianId);
                    var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                    foreach (var item1 in admins)
                    {
                        await _notificationService.createNotification(new Notification
                        {
                            isRead = false,
                            CurrentObject_Id = item.Id,
                            NotificationContent = "You have a maintenance schedule coming up!",
                            UserId = item1.Id,
                            ObjectName = ObjectName.MS.ToString(),
                        });
                        await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item1.Id);
                    }
                    var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(item.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        CurrentObject_Id = item.Id,
                        NotificationContent = "You have a maintenance schedule coming up!",
                        UserId = customerId,
                        ObjectName = ObjectName.MS.ToString(),
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                    await _context.SaveChangesAsync();
                }
            }
        }
        public async Task SetMaintenanceSchedulesNotifyWarning()
        {
            var maintainSchedule = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Status!.Equals("NOTIFIED")).ToListAsync();
            foreach (var item in maintainSchedule)
            {
                if (item.MaintainTime!.Value.Date == DateTime.UtcNow.AddHours(7).AddDays(1).Date)
                {
                    item.UpdateDate = DateTime.UtcNow.AddHours(7);
                    item.Status = ScheduleStatus.WARNING.ToString();
                    var technician = await _context.Technicians.Where(a => a.Id.Equals(item.TechnicianId)).FirstOrDefaultAsync();
                    if (technician!.Breach >= 3)
                    {
                        technician!.Breach = 3;
                    }
                    else
                    {
                        technician!.Breach = technician!.Breach + 1;
                    }
                    var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(item.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "Maintenance Schedule warning because the technician have no action!",
                        CurrentObject_Id = item.Id,
                        UserId = customerId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "Maintenance Schedule warning because the technician have no action!",
                        CurrentObject_Id = item.Id,
                        UserId = item.TechnicianId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.TechnicianId);
                    var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                    foreach (var item1 in admins)
                    {
                        await _notificationService.createNotification(new Notification
                        {
                            isRead = false,
                            ObjectName = ObjectName.MS.ToString(),
                            CreatedTime = DateTime.UtcNow.AddHours(7),
                            NotificationContent = "Maintenance Schedule warning because the technician have no action!",
                            CurrentObject_Id = item.Id,
                            UserId = item1.Id,
                        });
                        await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item1.Id);
                    }
                    await _context.SaveChangesAsync();
                }
            }

        }
        public async Task SetMaintenanceSchedulesNotifyMissing()
        {
            var maintainSchedule = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Status!.Equals("WARNING")).ToListAsync();
            foreach (var item in maintainSchedule)
            {
                if (item.MaintainTime!.Value.AddDays(7).Date == DateTime.UtcNow.AddHours(7).Date)
                {
                    item.UpdateDate = DateTime.UtcNow.AddHours(7);
                    item.Status = ScheduleStatus.MISSED.ToString();
                    var customerId = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(item.AgencyId)).Select(a => a.CustomerId).FirstOrDefaultAsync();
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have maintenance schedule is missed!",
                        CurrentObject_Id = item.Id,
                        UserId = customerId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", customerId);
                    var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                    foreach (var item1 in admins)
                    {
                        await _notificationService.createNotification(new Notification
                        {
                            isRead = false,
                            ObjectName = ObjectName.MS.ToString(),
                            CreatedTime = DateTime.UtcNow.AddHours(7),
                            NotificationContent = "You have maintenance schedule is missed",
                            CurrentObject_Id = item.Id,
                            UserId = item1.Id,
                        });
                        await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item1.Id);
                    }
                    await _context.SaveChangesAsync();
                }
            }

        }

        public async Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedules(PaginationRequest model, FilterStatusRequest value)
        {
            var total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false).ToListAsync();
            var maintenanceSchedules = new List<MaintenanceScheduleResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false).Select(a => new MaintenanceScheduleResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    maintain_time = a.MaintainTime,
                    status = a.Status,
                    start_time = a.StartDate,
                    end_time = a.EndDate,
                    technician = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                        cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                    }
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
                var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var technician_name = await _context.Technicians.Where(a => a.TechnicianName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                 && (a.Status!.Contains(value.status)
                 && (a.Name!.Contains(value.search)
                 || a.Code!.Contains(value.search)
                 || a.AgencyId!.Equals(agency_name)
                 || a.TechnicianId!.Equals(technician_name)
                 || a.ContractId!.Equals(contract_name)))).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                 && (a.Status!.Contains(value.status)
                 && (a.Name!.Contains(value.search)
                 || a.Code!.Contains(value.search)
                 || a.AgencyId!.Equals(agency_name)
                 || a.TechnicianId!.Equals(technician_name)
                 || a.ContractId!.Equals(contract_name)))).Select(a => new MaintenanceScheduleResponse
                 {
                     id = a.Id,
                     code = a.Code,
                     name = a.Name,
                     description = a.Description,
                     is_delete = a.IsDelete,
                     create_date = a.CreateDate,
                     update_date = a.UpdateDate,
                     maintain_time = a.MaintainTime,
                     status = a.Status,
                     start_time = a.StartDate,
                     end_time = a.EndDate,
                     technician = new TechnicianViewResponse
                     {
                         id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                         phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                         email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                         code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                         tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                     },
                     agency = new AgencyViewResponse
                     {
                         id = a.AgencyId,
                         code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                         agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                         address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                         phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                         manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                     },
                     customer = new CustomerViewResponse
                     {
                         id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                         code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                         cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                         address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                         phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                     }
                 }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<MaintenanceScheduleResponse>(maintenanceSchedules)
            {
                Total = total.Count,
                Type = "MaintenanceSchedules"
            };

        }
        public async Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedulesByCustomer(Guid id, PaginationRequest model, FilterStatusRequest value)
        {
            var total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Agency!.CustomerId.Equals(id)).ToListAsync();
            var maintenanceSchedules = new List<MaintenanceScheduleResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Agency!.CustomerId.Equals(id)).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.Agency!.CustomerId.Equals(id)).Select(a => new MaintenanceScheduleResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    maintain_time = a.MaintainTime,
                    status = a.Status,
                    start_time = a.StartDate,
                    end_time = a.EndDate,
                    technician = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                        cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                    }
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
                var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                var technician_name = await _context.Technicians.Where(a => a.TechnicianName!.Contains(value.search)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                && a.Agency!.CustomerId.Equals(id)
                && (a.Status!.Contains(value.status)
                && (a.Name!.Contains(value.search)
                || a.Code!.Contains(value.search)
                || a.AgencyId!.Equals(agency_name)
                || a.TechnicianId!.Equals(technician_name)
                || a.ContractId!.Equals(contract_name)))).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                && a.Agency!.CustomerId.Equals(id)
                && (a.Status!.Contains(value.status)
                && (a.Name!.Contains(value.search)
                || a.Code!.Contains(value.search)
                || a.AgencyId!.Equals(agency_name)
                || a.TechnicianId!.Equals(technician_name)
                || a.ContractId!.Equals(contract_name)))).Select(a => new MaintenanceScheduleResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    maintain_time = a.MaintainTime,
                    status = a.Status,
                    start_time = a.StartDate,
                    end_time = a.EndDate,
                    technician = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                        cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                    }
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<MaintenanceScheduleResponse>(maintenanceSchedules)
            {
                Total = total.Count,
                Type = "MaintenanceSchedules"
            };

        }
        public async Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedulesTechnician(PaginationRequest model, Guid id, FilterStatusRequest value)
        {
            var total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)).ToListAsync();
            var maintenanceSchedules = new List<MaintenanceScheduleResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)).Select(a => new MaintenanceScheduleResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    maintain_time = a.MaintainTime,
                    status = a.Status,
                    start_time = a.StartDate,
                    end_time = a.EndDate,
                    technician = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                        cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                    }
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
                var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var technician_name = await _context.Technicians.Where(a => a.TechnicianName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                 && a.TechnicianId.Equals(id)
                 && (a.Status!.Contains(value.status)
                 && (a.Name!.Contains(value.search)
                 || a.Code!.Contains(value.search)
                 || a.AgencyId!.Equals(agency_name)
                 || a.TechnicianId!.Equals(technician_name)
                 || a.ContractId!.Equals(contract_name)))).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                 && a.TechnicianId.Equals(id)
                 && (a.Status!.Contains(value.status)
                 && (a.Name!.Contains(value.search)
                 || a.Code!.Contains(value.search)
                 || a.AgencyId!.Equals(agency_name)
                 || a.TechnicianId!.Equals(technician_name)
                 || a.ContractId!.Equals(contract_name)))).Select(a => new MaintenanceScheduleResponse
                 {
                     id = a.Id,
                     code = a.Code,
                     name = a.Name,
                     description = a.Description,
                     is_delete = a.IsDelete,
                     create_date = a.CreateDate,
                     update_date = a.UpdateDate,
                     maintain_time = a.MaintainTime,
                     status = a.Status,
                     start_time = a.StartDate,
                     end_time = a.EndDate,
                     technician = new TechnicianViewResponse
                     {
                         id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                         phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                         email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                         code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                         tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                     },
                     agency = new AgencyViewResponse
                     {
                         id = a.AgencyId,
                         code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                         agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                         address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                         phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                         manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                     },
                     customer = new CustomerViewResponse
                     {
                         id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                         code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                         cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                         address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                         phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                     }
                 }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();

            }
            return new ResponseModel<MaintenanceScheduleResponse>(maintenanceSchedules)
            {
                Total = total.Count,
                Type = "MaintenanceSchedules"
            };
        }
        public async Task<ResponseModel<MaintenanceScheduleResponse>> GetListMaintenanceSchedulesAgency(PaginationRequest model, Guid id, FilterStatusRequest value)
        {
            var total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).ToListAsync();
            var maintenanceSchedules = new List<MaintenanceScheduleResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).Select(a => new MaintenanceScheduleResponse
                {
                    id = a.Id,
                    code = a.Code,
                    name = a.Name,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    maintain_time = a.MaintainTime,
                    status = a.Status,
                    start_time = a.StartDate,
                    end_time = a.EndDate,
                    technician = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = a.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                        cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                    }

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
                var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                var technician_name = await _context.Technicians.Where(a => a.TechnicianName!.Contains(value.search!.Trim())).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                 && a.AgencyId.Equals(id)
                 && (a.Status!.Contains(value.status)
                 && (a.Name!.Contains(value.search)
                 || a.Code!.Contains(value.search)
                 || a.AgencyId!.Equals(agency_name)
                 || a.TechnicianId!.Equals(technician_name)
                 || a.ContractId!.Equals(contract_name)))).ToListAsync();
                maintenanceSchedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false
                 && a.AgencyId.Equals(id)
                 && (a.Status!.Contains(value.status)
                 && (a.Name!.Contains(value.search)
                 || a.Code!.Contains(value.search)
                 || a.AgencyId!.Equals(agency_name)
                 || a.TechnicianId!.Equals(technician_name)
                 || a.ContractId!.Equals(contract_name)))).Select(a => new MaintenanceScheduleResponse
                 {
                     id = a.Id,
                     code = a.Code,
                     name = a.Name,
                     description = a.Description,
                     is_delete = a.IsDelete,
                     create_date = a.CreateDate,
                     update_date = a.UpdateDate,
                     maintain_time = a.MaintainTime,
                     status = a.Status,
                     start_time = a.StartDate,
                     end_time = a.EndDate,
                     technician = new TechnicianViewResponse
                     {
                         id = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                         phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                         email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                         code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                         tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                     },
                     agency = new AgencyViewResponse
                     {
                         id = a.AgencyId,
                         code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                         agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                         address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                         phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                         manager_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                     },
                     customer = new CustomerViewResponse
                     {
                         id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                         code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                         cus_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                         address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                         phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                     }
                 }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();

            }
            return new ResponseModel<MaintenanceScheduleResponse>(maintenanceSchedules)
            {
                Total = total.Count,
                Type = "MaintenanceSchedules"
            };
        }
        public async Task<ObjectModelResponse> UpdateMaintenanceSchedule(Guid id, MaintenanceScheduleRequest model)
        {
            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var date = maintenanceSchedule!.MaintainTime!.Value;
            var message = "blank";
            var status = 500;
            var data = new MaintenanceScheduleResponse();
            var con = await _context.Contracts.Where(a => a.IsAccepted == true
            && a.IsExpire == false
            && a.IsDelete == false
            && a.Id.Equals(maintenanceSchedule.ContractId)).FirstOrDefaultAsync();
            if (model.maintain_time!.Value.AddHours(7).Date < DateTime.UtcNow.AddHours(7).AddDays(2).Date)
            {
                message = "The new maintenance time must be 2 days older than the current date";
                status = 400;
            }else if (model.maintain_time!.Value.AddHours(7).Date < con!.StartDate!.Value.AddDays(2).Date)
            {
                message = "The new maintenance time must be 2 days older than the start date of contract";
                status = 400;
            }
            else
            {
                if (maintenanceSchedule!.TechnicianId != model.technician_id)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You are scheduled a new maintenance schedule!",
                        CurrentObject_Id = maintenanceSchedule.Id,
                        UserId = model.technician_id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", model.technician_id);
                }
                if (maintenanceSchedule!.MaintainTime.Value.Date != model.maintain_time.Value.AddHours(7).Date)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "Your maintenance schedule have been update maintenance time!",
                        CurrentObject_Id = maintenanceSchedule.Id,
                        UserId = model.technician_id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", model.technician_id);
                }
                message = "Successfully";
                status = 200;
                if (model.maintain_time.Value.AddHours(7).Date > DateTime.UtcNow.AddHours(7).AddDays(2).Date)
                {
                    maintenanceSchedule!.Status = ScheduleStatus.SCHEDULED.ToString();
                }
                else if ((model.maintain_time.Value.AddHours(7).Date == DateTime.UtcNow.AddHours(7).AddDays(2).Date) && (maintenanceSchedule!.MaintainTime.Value.Date != model.maintain_time.Value.AddHours(7).Date))
                {
                    maintenanceSchedule!.Status = ScheduleStatus.NOTIFIED.ToString();
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.MS.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a maintenance schedule coming up!",
                        CurrentObject_Id = maintenanceSchedule.Id,
                        UserId = model.technician_id,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", model.technician_id);
                }
                maintenanceSchedule!.Description = model.description;
                maintenanceSchedule!.MaintainTime = model.maintain_time.Value.AddHours(7);
                maintenanceSchedule!.TechnicianId = model.technician_id;
                maintenanceSchedule.UpdateDate = DateTime.UtcNow.AddHours(7);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new MaintenanceScheduleResponse
                    {
                        id = maintenanceSchedule.Id,
                        code = maintenanceSchedule.Code,
                        name = maintenanceSchedule.Name,
                        description = maintenanceSchedule.Description,
                        is_delete = maintenanceSchedule.IsDelete,
                        create_date = maintenanceSchedule.CreateDate,
                        update_date = maintenanceSchedule.UpdateDate,
                        maintain_time = maintenanceSchedule.MaintainTime,
                        status = maintenanceSchedule.Status,
                        start_time = maintenanceSchedule.StartDate,
                        end_time = maintenanceSchedule.EndDate,
                        technician = new TechnicianViewResponse
                        {
                            id = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            email = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        },
                        agency = new AgencyViewResponse
                        {
                            id = maintenanceSchedule.AgencyId,
                            code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                            agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                            phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                            manager_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                        },
                        customer = new CustomerViewResponse
                        {
                            id = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                            code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                            cus_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                            address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                            phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                        }
                    };
                }
            }
            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "MaintenanceSchedule"
            };
        }
        public async Task<ObjectModelResponse> DisableMaintenanceSchedule(Guid id)
        {
            var maintenanceSchedule = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            maintenanceSchedule!.IsDelete = true;
            _context.MaintenanceSchedules.Update(maintenanceSchedule);
            var data = new MaintenanceScheduleResponse();
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new MaintenanceScheduleResponse
                {
                    id = maintenanceSchedule.Id,
                    code = maintenanceSchedule.Code,
                    name = maintenanceSchedule.Name,
                    description = maintenanceSchedule.Description,
                    is_delete = maintenanceSchedule.IsDelete,
                    create_date = maintenanceSchedule.CreateDate,
                    update_date = maintenanceSchedule.UpdateDate,
                    maintain_time = maintenanceSchedule.MaintainTime,
                    status = maintenanceSchedule.Status,
                    start_time = maintenanceSchedule.StartDate,
                    end_time = maintenanceSchedule.EndDate,
                    technician = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(maintenanceSchedule.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    agency = new AgencyViewResponse
                    {
                        id = maintenanceSchedule.AgencyId,
                        code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Code).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.AgencyName).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Telephone).FirstOrDefault(),
                        manager_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.ManagerName).FirstOrDefault(),
                    },
                    customer = new CustomerViewResponse
                    {
                        id = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.CustomerId).FirstOrDefault(),
                        code = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Code).FirstOrDefault(),
                        cus_name = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Name).FirstOrDefault(),
                        address = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Address).FirstOrDefault(),
                        phone = _context.Agencies.Where(x => x.Id.Equals(maintenanceSchedule.AgencyId)).Select(a => a.Customer!.Phone).FirstOrDefault(),
                    }
                };
            }
            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "MaintenanceSchedule"
            };
        }


    }
}
