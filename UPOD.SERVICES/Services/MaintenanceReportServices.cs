using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
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
    }

    public class MaintenanceReportServices : IMaintenanceReportService
    {
        private readonly Database_UPODContext _context;
        public MaintenanceReportServices(Database_UPODContext context)
        {
            _context = context;
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
                    img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName!.Equals(ObjectName.MRS.ToString())).Select(a => a.Link).ToList()!
                }).ToList(),
                device = _context.MaintenanceReportDevices.Where(x => x.MaintenanceReportId.Equals(a.Id)).Select(a => new DeviceReportResponse
                {
                    service_name = a.Service!.ServiceName,
                    device_name = a.Device!.DeviceName,
                    description = a.Description,
                    solution = a.Solution,
                    img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName!.Equals(ObjectName.MRD.ToString())).Select(a => a.Link).ToList()!
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
            maintenanceReport!.Name = model.name;
            maintenanceReport!.MaintenanceScheduleId = model.maintenance_schedule_id;
            maintenanceReport!.UpdateDate = DateTime.UtcNow.AddHours(7);
            maintenanceReport!.Status = ReportStatus.STABILIZED.ToString();
            var maintenanceReportDevices = await _context.MaintenanceReportDevices.Where(a => a.MaintenanceReportId.Equals(id)).ToListAsync();
            if (model.device.Count == 0)
                foreach (var item in maintenanceReportDevices)
                {
                    var imgs = await _context.Images.Where(a => a.CurrentObject_Id.Equals(item.Id) && a.ObjectName.Equals(ObjectName.MRD.ToString())).ToListAsync();
                    foreach (var item1 in imgs)
                    {
                        _context.Images.Remove(item1);
                    }
                    _context.MaintenanceReportDevices.Remove(item);
                }
            else
            {
                foreach (var item in maintenanceReportDevices)
                {
                    var imgs = await _context.Images.Where(a => a.CurrentObject_Id.Equals(item.Id) && a.ObjectName.Equals(ObjectName.MRD.ToString())).ToListAsync();
                    foreach (var item1 in imgs)
                    {
                        _context.Images.Remove(item1);
                    }
                    _context.MaintenanceReportDevices.Remove(item);
                }
                foreach (var item in model.device)
                {
                    var maintenanceReportDevice_id = Guid.NewGuid();
                    while (true)
                    {
                        var maintenanceReportDevice_dup = await _context.MaintenanceReportDevices.Where(x => x.Id.Equals(maintenanceReportDevice_id)).FirstOrDefaultAsync();
                        if (maintenanceReportDevice_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            maintenanceReportDevice_id = Guid.NewGuid();
                        }
                    }
                    var maintenanceReportDevice = new MaintenanceReportDevice
                    {
                        Id = maintenanceReportDevice_id,
                        MaintenanceReportId = maintenanceReport.Id,
                        ServiceId = item.service_id,
                        DeviceId = item.device_id,
                        Description = item.description,
                        Solution = item.solution,
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

                            var imgMaintenanceReportDevice = new Image
                            {
                                Id = img_id,
                                Link = item1,
                                CurrentObject_Id = maintenanceReportDevice.Id,
                                ObjectName = ObjectName.MRD.ToString(),
                            };
                            await _context.Images.AddAsync(imgMaintenanceReportDevice);
                        }
                    }
                    await _context.MaintenanceReportDevices.AddAsync(maintenanceReportDevice);
                }
            }
            if (model.service.Count == 0)
            {
                var report_service_removes = await _context.MaintenanceReportServices.Where(a => a.MaintenanceReportId.Equals(maintenanceReport.Id)).ToListAsync();
                foreach (var report_service in report_service_removes)
                {
                    var imgs = await _context.Images.Where(a => a.CurrentObject_Id.Equals(report_service.Id) && a.ObjectName.Equals(ObjectName.MRS.ToString())).ToListAsync();
                    foreach (var item1 in imgs)
                    {
                        _context.Images.Remove(item1);
                    }
                    _context.MaintenanceReportServices.Remove(report_service);
                }
            }
            else
            {
                maintenanceReport!.Status = ReportStatus.TROUBLED.ToString();
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
                        img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName.Equals(ObjectName.MRS.ToString())).Select(a => a.Link).ToList()!
                    }).ToList(),
                    device = _context.MaintenanceReportDevices.Where(x => x.MaintenanceReportId.Equals(maintenanceReport.Id)).Select(a => new DeviceReportResponse
                    {
                        service_name = a.Service!.ServiceName,
                        device_name = a.Device!.DeviceName,
                        description = a.Description,
                        solution = a.Solution,
                        img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName.Equals(ObjectName.MRD.ToString())).Select(a => a.Link).ToList()!
                    }).ToList(),
                };
            }
            return new ObjectModelResponse(data)
            {
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
                Status = ReportStatus.STABILIZED.ToString(),
            };
            if (model.device.Count > 0)
            {
                foreach (var item in model.device)
                {
                    var maintenanceReportDevice_id = Guid.NewGuid();
                    while (true)
                    {
                        var maintenanceReportDevice_dup = await _context.MaintenanceReportDevices.Where(x => x.Id.Equals(maintenanceReportDevice_id)).FirstOrDefaultAsync();
                        if (maintenanceReportDevice_dup == null)
                        {
                            break;
                        }
                        else
                        {
                            maintenanceReportDevice_id = Guid.NewGuid();
                        }
                    }
                    var maintenanceReportDevice = new MaintenanceReportDevice
                    {
                        Id = maintenanceReportDevice_id,
                        MaintenanceReportId = maintenanceReport.Id,
                        ServiceId = item.service_id,
                        DeviceId = item.device_id,
                        Description = item.description,
                        Solution = item.solution,
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

                            var imgMaintenanceReportDevice = new Image
                            {
                                Id = img_id,
                                Link = item1,
                                CurrentObject_Id = maintenanceReportDevice.Id,
                                ObjectName = ObjectName.MRD.ToString(),
                            };
                            await _context.Images.AddAsync(imgMaintenanceReportDevice);
                        }
                    }
                    await _context.MaintenanceReportDevices.AddAsync(maintenanceReportDevice);
                }
            }
            if (model.service.Count == 0)
            {
                var maintenanceScheduleStatus = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(model.maintenance_schedule_id)).FirstOrDefaultAsync();
                maintenanceScheduleStatus!.Status = ScheduleStatus.COMPLETED.ToString();
                maintenanceScheduleStatus!.EndDate = DateTime.UtcNow.AddHours(7);
                var technician = await _context.Technicians.Where(x => x.Id.Equals(maintenanceReport!.CreateBy)).FirstOrDefaultAsync();
                await _context.MaintenanceReports.AddAsync(maintenanceReport);
                technician!.IsBusy = false;
            }
            else
            {
                var maintenanceScheduleStatus = await _context.MaintenanceSchedules.Where(a => a.Id.Equals(model.maintenance_schedule_id)).FirstOrDefaultAsync();
                maintenanceScheduleStatus!.Status = ScheduleStatus.COMPLETED.ToString();
                maintenanceScheduleStatus!.EndDate = DateTime.UtcNow.AddHours(7);
                await _context.MaintenanceReports.AddAsync(maintenanceReport);
                var technician = await _context.Technicians.Where(x => x.Id.Equals(maintenanceReport!.CreateBy)).FirstOrDefaultAsync();
                maintenanceReport!.Status = ReportStatus.TROUBLED.ToString();
                technician!.IsBusy = false;
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
                        img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName.Equals(ObjectName.MRS.ToString())).Select(a => a.Link).ToList()!
                    }).ToList(),
                    device = _context.MaintenanceReportDevices.Where(x => x.MaintenanceReportId.Equals(maintenanceReport.Id)).Select(a => new DeviceReportResponse
                    {
                        service_name = a.Service!.ServiceName,
                        device_name = a.Device!.DeviceName,
                        description = a.Description,
                        solution = a.Solution,
                        img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName.Equals(ObjectName.MRD.ToString())).Select(a => a.Link).ToList()!
                    }).ToList(),
                };
            }
            return new ObjectModelResponse(data)
            {
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
