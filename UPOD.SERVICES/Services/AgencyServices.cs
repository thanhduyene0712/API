using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

namespace UPOD.SERVICES.Services
{

    public interface IAgencyService
    {
        Task<ResponseModel<AgencyResponse>> GetListAgencies(PaginationRequest model, SearchRequest value);
        Task<ResponseModel<AgencyResponse>> GetListAgenciesByTechnician(PaginationRequest model, Guid id, SearchRequest value);
        Task<ObjectModelResponse> GetDetailsAgency(Guid id);
        Task<ObjectModelResponse> CreateAgency(AgencyRequest model);
        Task<ObjectModelResponse> UpdateAgency(Guid id, AgencyUpdateRequest model);
        Task<ObjectModelResponse> DisableAgency(Guid id);
        Task<ResponseModel<TechnicianViewResponse>> GetTechnicianByAgencyId(PaginationRequest model, Guid id);
    }

    public class AgencyServices : IAgencyService
    {
        private readonly Database_UPODContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotifyHub> _notifyHub;

        public AgencyServices(Database_UPODContext context, INotificationService notificationService, IHubContext<NotifyHub> notifyHub)
        {
            _context = context;
            _notificationService = notificationService;
            _notifyHub = notifyHub;
        }
        public async Task<ResponseModel<AgencyResponse>> GetListAgenciesByTechnician(PaginationRequest model, Guid id, SearchRequest value)
        {
            var total = await _context.Agencies.Where(a => a.IsDelete == false).ToListAsync();
            var agencies = new List<AgencyResponse>();
            if (value.search == null)
            {
                total = await _context.Agencies.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)).ToListAsync();
                agencies = await _context.Agencies.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)).Include(c => c.Customer).Include(a => a.Area).Select(a => new AgencyResponse

                {
                    id = a.Id,
                    code = a.Code,
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = a.Customer!.Code,
                        cus_name = a.Customer.Name,
                        address = a.Customer.Address,
                        phone = a.Customer.Phone,
                        description = a.Customer.Description,
                        mail = a.Customer.Mail,
                    },
                    area = new AreaViewResponse
                    {
                        id = a.AreaId,
                        code = a.Area!.Code,
                        area_name = a.Area.AreaName,
                        description = a.Area.Description
                    },
                    technician = new TechnicianViewResponse
                    {
                        id = a.TechnicianId,
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    manager_name = a.ManagerName,
                    agency_name = a.AgencyName,
                    address = a.Address,
                    telephone = a.Telephone,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    device = _context.Devices.Where(x => x.AgencyId.Equals(a.Id)).Select(x => new DeviceViewResponse
                    {
                        id = x.Id,
                        code = x.Code,
                        device_name = x.DeviceName
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                var customer = await _context.Customers.Where(a => a.IsDelete == false && a.Name!.Contains(value!.search)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.Agencies.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)
                && (a.Code!.Contains(value.search)
                || a.AgencyName!.Contains(value.search)
                || a.CustomerId!.Equals(customer)
                || a.Address!.Contains(value.search)
                || a.Telephone!.Contains(value.search))).ToListAsync();
                agencies = await _context.Agencies.Where(a => a.IsDelete == false && a.TechnicianId.Equals(id)
                && (a.Code!.Contains(value.search)
                || a.AgencyName!.Contains(value.search)
                || a.CustomerId!.Equals(customer)
                || a.Address!.Contains(value.search)
                || a.Telephone!.Contains(value.search))).Include(c => c.Customer).Include(a => a.Area).Select(a => new AgencyResponse

                {
                    id = a.Id,
                    code = a.Code,
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = a.Customer!.Code,
                        cus_name = a.Customer.Name,
                        address = a.Customer.Address,
                        phone = a.Customer.Phone,
                        description = a.Customer.Description,
                        mail = a.Customer.Mail,
                    },
                    area = new AreaViewResponse
                    {
                        id = a.AreaId,
                        code = a.Area!.Code,
                        area_name = a.Area.AreaName,
                        description = a.Area.Description
                    },
                    technician = new TechnicianViewResponse
                    {
                        id = a.TechnicianId,
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    manager_name = a.ManagerName,
                    agency_name = a.AgencyName,
                    address = a.Address,
                    telephone = a.Telephone,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    device = _context.Devices.Where(x => x.AgencyId.Equals(a.Id)).Select(x => new DeviceViewResponse
                    {
                        id = x.Id,
                        code = x.Code,
                        device_name = x.DeviceName
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<AgencyResponse>(agencies)
            {
                Total = total.Count,
                Type = "Agencies"
            };
        }
        public async Task<ResponseModel<AgencyResponse>> GetListAgencies(PaginationRequest model, SearchRequest value)
        {
            var total = await _context.Agencies.Where(a => a.IsDelete == false).ToListAsync();
            var agencies = new List<AgencyResponse>();
            if (value.search == null)
            {
                total = await _context.Agencies.Where(a => a.IsDelete == false).ToListAsync();
                agencies = await _context.Agencies.Where(a => a.IsDelete == false).Include(c => c.Customer).Include(a => a.Area).Select(a => new AgencyResponse

                {
                    id = a.Id,
                    code = a.Code,
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = a.Customer!.Code,
                        cus_name = a.Customer.Name,
                        address = a.Customer.Address,
                        phone = a.Customer.Phone,
                        description = a.Customer.Description,
                        mail = a.Customer.Mail,
                    },
                    area = new AreaViewResponse
                    {
                        id = a.AreaId,
                        code = a.Area!.Code,
                        area_name = a.Area.AreaName,
                        description = a.Area.Description
                    },
                    technician = new TechnicianViewResponse
                    {
                        id = a.TechnicianId,
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    manager_name = a.ManagerName,
                    agency_name = a.AgencyName,
                    address = a.Address,
                    telephone = a.Telephone,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    device = _context.Devices.Where(x => x.AgencyId.Equals(a.Id)).Select(x => new DeviceViewResponse
                    {
                        id = x.Id,
                        code = x.Code,
                        device_name = x.DeviceName
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                var customer = await _context.Customers.Where(a => a.Name!.Contains(value!.search)).Select(a => a.Id).FirstOrDefaultAsync();
                total = await _context.Agencies.Where(a => a.IsDelete == false
               && (a.Code!.Contains(value.search)
               || a.AgencyName!.Contains(value.search)
               || a.CustomerId!.Equals(customer)
               || a.Address!.Contains(value.search)
               || a.Telephone!.Contains(value.search))).ToListAsync();
                agencies = await _context.Agencies.Where(a => a.IsDelete == false
                && (a.Code!.Contains(value.search)
                || a.AgencyName!.Contains(value.search)
                || a.CustomerId!.Equals(customer)
                || a.Address!.Contains(value.search)
                || a.Telephone!.Contains(value.search))).Include(c => c.Customer).Include(a => a.Area).Select(a => new AgencyResponse

                {
                    id = a.Id,
                    code = a.Code,
                    customer = new CustomerViewResponse
                    {
                        id = a.CustomerId,
                        code = a.Customer!.Code,
                        cus_name = a.Customer.Name,
                        address = a.Customer.Address,
                        phone = a.Customer.Phone,
                        description = a.Customer.Description,
                        mail = a.Customer.Mail,
                    },
                    area = new AreaViewResponse
                    {
                        id = a.AreaId,
                        code = a.Area!.Code,
                        area_name = a.Area.AreaName,
                        description = a.Area.Description
                    },
                    technician = new TechnicianViewResponse
                    {
                        id = a.TechnicianId,
                        email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    manager_name = a.ManagerName,
                    agency_name = a.AgencyName,
                    address = a.Address,
                    telephone = a.Telephone,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    device = _context.Devices.Where(x => x.AgencyId.Equals(a.Id)).Select(x => new DeviceViewResponse
                    {
                        id = x.Id,
                        code = x.Code,
                        device_name = x.DeviceName
                    }).ToList(),
                }).OrderByDescending(a => a.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<AgencyResponse>(agencies)
            {
                Total = total.Count,
                Type = "Agencies"
            };
        }
        public async Task<ResponseModel<TechnicianViewResponse>> GetTechnicianByAgencyId(PaginationRequest model, Guid id)
        {
            var agency = await _context.Agencies.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var area = await _context.Areas.Where(a => a.Id.Equals(agency!.AreaId)).FirstOrDefaultAsync();
            var total = await _context.Technicians.Where(a => a.IsDelete == false && a.AreaId.Equals(area!.Id)).ToListAsync();
            var technician = await _context.Technicians.Where(a => a.IsDelete == false && a.AreaId.Equals(area!.Id)).Select(a => new TechnicianViewResponse
            {
                id = a.Id,
                code = a.Code,
                tech_name = a.TechnicianName,
                email = a.Email,
                phone = a.Telephone,
            }).ToListAsync();
            return new ResponseModel<TechnicianViewResponse>(technician)
            {
                Total = total.Count,
                Type = "Technicians"
            };
        }
        public async Task<ObjectModelResponse> GetDetailsAgency(Guid id)
        {
            var agency = await _context.Agencies.Where(a => a.IsDelete == false && a.Id.Equals(id)).Select(a => new AgencyResponse
            {
                id = a.Id,
                code = a.Code,
                customer = new CustomerViewResponse
                {
                    id = a.CustomerId,
                    code = a.Customer!.Code,
                    cus_name = a.Customer.Name,
                    address = a.Customer.Address,
                    phone = a.Customer.Phone,
                    description = a.Customer.Description,
                },
                area = new AreaViewResponse
                {
                    id = a.AreaId,
                    code = a.Area!.Code,
                    area_name = a.Area.AreaName,
                    description = a.Area.Description
                },
                technician = new TechnicianViewResponse
                {
                    id = a.TechnicianId,
                    email = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                    phone = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                    code = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                    tech_name = _context.Technicians.Where(x => x.Id.Equals(a.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                },
                manager_name = a.ManagerName,
                agency_name = a.AgencyName,
                address = a.Address,
                telephone = a.Telephone,
                is_delete = a.IsDelete,
                create_date = a.CreateDate,
                update_date = a.UpdateDate,
                device = _context.Devices.Where(x => x.AgencyId.Equals(a.Id)).Select(x => new DeviceViewResponse
                {
                    id = x.Id,
                    code = x.Code,
                    device_name = x.DeviceName
                }).ToList(),

            }).FirstOrDefaultAsync();
            return new ObjectModelResponse(agency!)
            {
                Type = "Agency"
            };
        }


        public async Task<ObjectModelResponse> CreateAgency(AgencyRequest model)
        {
            var agency_id = Guid.NewGuid();
            while (true)
            {
                var agency_dup = await _context.Agencies.Where(x => x.Id.Equals(agency_id)).FirstOrDefaultAsync();
                if (agency_dup == null)
                {
                    break;
                }
                else
                {
                    agency_id = Guid.NewGuid();
                }
            }
            var code_number = await GetLastCode();
            var code = CodeHelper.GeneratorCode("AG", code_number + 1);
            while (true)
            {
                var code_dup = await _context.Agencies.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                if (code_dup == null)
                {
                    break;
                }
                else
                {
                    code = "AG-" + code_number++.ToString();
                }
            }
            var agency = new Agency
            {
                Id = agency_id,
                Code = code,
                CustomerId = model.customer_id,
                AgencyName = model.agency_name,
                TechnicianId = model.technician_id,
                AreaId = model.area_id,
                ManagerName = model.manager_name,
                Address = model.address,
                Telephone = model.telephone,
                IsDelete = false,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7)

            };
            var data = new AgencyResponse();
            var message = "blank";
            var status = 500;
            var agency_phone = await _context.Agencies.Where(x => x.Telephone!.Equals(agency.Telephone) && x.IsDelete == false).FirstOrDefaultAsync();
            if (agency_phone != null)
            {
                status = 400;
                message = "Phone is already exists!";
            }
            else
            {
                message = "Successfully";
                status = 200;
                await _context.Agencies.AddAsync(agency);
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.AG.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have become the main technician of the agency!",
                    CurrentObject_Id = agency.Id,
                    UserId = agency.TechnicianId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", agency.TechnicianId);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new AgencyResponse
                    {
                        id = agency!.Id,
                        code = agency.Code,
                        customer = new CustomerViewResponse
                        {
                            id = _context.Customers.Where(x => x.Id.Equals(agency.CustomerId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Customers.Where(x => x.Id.Equals(agency.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                            cus_name = _context.Customers.Where(x => x.Id.Equals(agency.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                            address = _context.Customers.Where(x => x.Id.Equals(agency.CustomerId)).Select(a => a.Address).FirstOrDefault(),
                            phone = _context.Customers.Where(x => x.Id.Equals(agency.CustomerId)).Select(a => a.Phone).FirstOrDefault(),
                            description = _context.Customers.Where(x => x.Id.Equals(agency.CustomerId)).Select(a => a.Description).FirstOrDefault(),
                        },
                        area = new AreaViewResponse
                        {
                            id = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.Id).FirstOrDefault(),
                            code = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.Code).FirstOrDefault(),
                            area_name = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.AreaName).FirstOrDefault(),
                            description = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.Description).FirstOrDefault(),
                        },
                        technician = new TechnicianViewResponse
                        {
                            id = agency.TechnicianId,
                            email = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        },
                        manager_name = agency.ManagerName,
                        agency_name = agency.AgencyName,
                        address = agency.Address,
                        telephone = agency.Telephone,
                        is_delete = agency.IsDelete,
                        create_date = agency.CreateDate,
                        update_date = agency.UpdateDate,
                        device = _context.Devices.Where(x => x.AgencyId.Equals(agency.Id)).Select(x => new DeviceViewResponse
                        {
                            id = x.Id,
                            code = x.Code,
                            device_name = x.DeviceName
                        }).ToList(),

                    };

                }
            }

            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Agency"
            };
        }


        public async Task<ObjectModelResponse> DisableAgency(Guid id)
        {
            var agency = await _context.Agencies.Where(x => x.Id.Equals(id)).Include(x => x.Customer).Include(x => x.Area).FirstOrDefaultAsync();
            agency!.IsDelete = true;
            var devices = await _context.Devices.Where(x => x.AgencyId.Equals(agency.Id)).ToListAsync();
            foreach (var item in devices)
            {
                item.IsDelete = true;
            }
            var maintenance_schedules = await _context.MaintenanceSchedules.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).ToListAsync();
            foreach (var item in maintenance_schedules)
            {
                if (item!.Status!.Equals("SCHEDULED") || item!.Status!.Equals("NOTIFIED"))
                {
                    item.IsDelete = true;
                }
            }
            var data = new AgencyResponse();
            _context.Agencies.Update(agency);
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new AgencyResponse
                {
                    id = agency!.Id,
                    code = agency.Code,
                    customer = new CustomerViewResponse
                    {
                        id = agency.CustomerId,
                        code = agency.Customer!.Code,
                        cus_name = agency.Customer.Name,
                        address = agency.Customer.Address,
                        phone = agency.Customer.Phone,
                        description = agency.Customer.Description,
                    },
                    area = new AreaViewResponse
                    {
                        id = agency.AreaId,
                        code = agency.Area!.Code,
                        area_name = agency.Area.AreaName,
                        description = agency.Area.Description
                    },
                    technician = new TechnicianViewResponse
                    {
                        id = agency.TechnicianId,
                        email = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                    },
                    manager_name = agency.ManagerName,
                    agency_name = agency.AgencyName,
                    address = agency.Address,
                    telephone = agency.Telephone,
                    is_delete = agency.IsDelete,
                    create_date = agency.CreateDate,
                    update_date = agency.UpdateDate,
                    device = _context.Devices.Where(x => x.AgencyId.Equals(agency.Id)).Select(x => new DeviceViewResponse
                    {
                        id = x.Id,
                        code = x.Code,
                        device_name = x.DeviceName
                    }).ToList(),
                };
            }
            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Agency"
            };
        }
        public async Task<ObjectModelResponse> UpdateAgency(Guid id, AgencyUpdateRequest model)
        {
            var agency = await _context.Agencies.Where(a => a.Id.Equals(id)).Include(x => x.Area).Include(x => x.Customer).Include(x => x.Devices).FirstOrDefaultAsync();
            if (agency!.TechnicianId != model.technician_id)
            {
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.AG.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have become the main technician of the agency!",
                    CurrentObject_Id = agency.Id,
                    UserId = model.technician_id,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", model.technician_id);
            }
            var data = new AgencyResponse();
            var message = "blank";
            var status = 500;
            var agency_phone = await _context.Agencies.Where(x => x.Telephone!.Equals(model!.telephone) && x.IsDelete == false).FirstOrDefaultAsync();
            if (agency_phone != null && agency!.Telephone != model.telephone)
            {
                status = 400;
                message = "Phone is already exists!";
            }
            else
            {
                message = "Successfully";
                status = 200;
                agency!.TechnicianId = model.technician_id;
                agency!.AgencyName = model.agency_name;
                agency!.AreaId = model.area_id;
                agency!.ManagerName = model.manager_name;
                agency!.Address = model.address;
                agency!.Telephone = model.telephone;
                agency!.UpdateDate = DateTime.UtcNow.AddHours(7);
                var maintain_techs = await _context.MaintenanceSchedules.Where(a => a.AgencyId.Equals(agency.Id) && a.TechnicianId == null).ToListAsync();
                foreach (var maintain_tech in maintain_techs)
                {
                    maintain_tech.TechnicianId = model.technician_id;
                }
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new AgencyResponse
                    {
                        id = agency!.Id,
                        code = agency.Code,
                        customer = new CustomerViewResponse
                        {
                            id = agency.CustomerId,
                            code = agency.Customer!.Code,
                            cus_name = agency.Customer.Name,
                            mail = agency.Customer.Mail,
                            address = agency.Customer.Address,
                            phone = agency.Customer.Phone,
                            description = agency.Customer.Description,
                        },
                        area = new AreaViewResponse
                        {
                            id = agency.AreaId,
                            code = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.Code).FirstOrDefault(),
                            area_name = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.AreaName).FirstOrDefault(),
                            description = _context.Areas.Where(x => x.Id.Equals(agency.AreaId)).Select(a => a.Description).FirstOrDefault(),
                        },
                        technician = new TechnicianViewResponse
                        {
                            id = agency.TechnicianId,
                            email = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Email).FirstOrDefault(),
                            phone = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                            code = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                            tech_name = _context.Technicians.Where(x => x.Id.Equals(agency.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        },
                        manager_name = agency.ManagerName,
                        agency_name = agency.AgencyName,
                        address = agency.Address,
                        telephone = agency.Telephone,
                        is_delete = agency.IsDelete,
                        create_date = agency.CreateDate,
                        update_date = agency.UpdateDate,
                        device = _context.Devices.Where(x => x.AgencyId.Equals(agency.Id)).Select(x => new DeviceViewResponse
                        {
                            id = x.Id,
                            code = x.Code,
                            device_name = x.DeviceName
                        }).ToList(),
                    };
                }
            }

            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "Agency"
            };
        }
        private async Task<int> GetLastCode()
        {
            var agency = await _context.Agencies.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(agency!.Code!);
        }
    }
}
