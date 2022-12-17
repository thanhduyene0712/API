using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
    public interface IRequestService
    {
        Task<ResponseModel<RequestResponse>> GetListRequests(PaginationRequest model, FilterStatusRequest value);
        Task<ObjectModelResponse> GetDetailsRequest(Guid id);
        Task<ObjectModelResponse> CreateRequest(RequestRequest model);
        Task<ObjectModelResponse> UpdateRequest(Guid id, RequestUpdateRequest model);
        Task<ObjectModelResponse> UpdateRequestAdmin(Guid id, RequestUpdateAdminRequest model);
        Task<ObjectModelResponse> DisableRequest(Guid id);
        Task<ResponseModel<TechnicianOfRequestResponse>> GetTechnicianRequest(Guid id);
        Task<ResponseModel<TechnicianOfRequestResponse>> GetTechnicianRequestAdmin(Guid agency_id, Guid service_id);
        Task<ObjectModelResponse> MappingTechnicianRequest(Guid request_id, Guid technician_id);
        Task<ResponseModel<DeviceResponse>> GetDeviceRequest(PaginationRequest model, Guid id);
        Task<ObjectModelResponse> CreateRequestByAdmin(RequestAdminRequest model);
        Task<ObjectModelResponse> RejectRequest(Guid id, RejectRequest value);
        Task<ObjectModelResponse> CancelRequest(Guid id, RejectRequest model);
        Task<ObjectModelResponse> AutoFillRequestAdmin(Guid id);
        Task<ObjectModelResponse> GetTicketDetails(Guid id);
        Task<ObjectModelResponse> GetContractByCustomerSerivce(Guid cus_id, Guid service_id);
        Task WarningRequest();
        Task CompletedRequest();
        Task<ResponseModel<RequestResponse>> GetListRequestsOfAgency(PaginationRequest model, Guid id, FilterStatusRequest value);
    }
    public class RequestServices : IRequestService
    {

        private readonly Database_UPODContext _context;
        private readonly IHubContext<NotifyHub> _notifyHub;
        private readonly INotificationService _notificationService;

        public RequestServices(Database_UPODContext context, IHubContext<NotifyHub> notifyHub, INotificationService notificationService)
        {
            _context = context;
            _notifyHub = notifyHub;
            _notificationService = notificationService;
        }
        public async Task<ObjectModelResponse> GetTicketDetails(Guid id)
        {
            var ticket = await _context.RequestDevices.Where(a => a.Id.Equals(id) && a.IsDelete == false).Select(a => new TicketResponse
            {
                id = a.Id,
                create_by = _context.Technicians.Where(x => x.Id.Equals(a.CreateBy) && a.IsDelete == false).Select(a => a.TechnicianName).FirstOrDefault(),
                create_date = a.CreateDate,
                update_date = a.UpdateDate,
                description = a.Description,
                solution = a.Solution,
                device_name = _context.Devices.Where(x => x.Id.Equals(a.DeviceId) && a.IsDelete == false).Select(a => a.DeviceName).FirstOrDefault(),
                is_delete = a.IsDelete,
                request_id = a.RequestId,
                img = _context.Images.Where(x => x.CurrentObject_Id.Equals(a.Id) && x.ObjectName!.Equals(ObjectName.RE.ToString())).Select(x => x.Link).ToList()!,
            }).FirstOrDefaultAsync();

            return new ObjectModelResponse(ticket!)
            {
                Type = "Ticket",
            };
        }
        public async Task<ObjectModelResponse> AutoFillRequestAdmin(Guid id)
        {
            var reportSchedule = await _context.MaintenanceReportServices.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var maintenance_report = await _context.MaintenanceReports.Where(a => a.Id.Equals(reportSchedule!.MaintenanceReportId)).FirstOrDefaultAsync();
            var agency = await _context.Agencies.Where(a => a.Id.Equals(reportSchedule!.MaintenanceReport!.AgencyId)).FirstOrDefaultAsync();
            var customer = await _context.Customers.Where(a => a.Id.Equals(agency!.CustomerId)).FirstOrDefaultAsync();
            var service = await _context.Services.Where(a => a.Id.Equals(reportSchedule!.ServiceId)).FirstOrDefaultAsync();
            var request = new AutoFillRequestResponse
            {
                report_service_id = reportSchedule!.Id,
                customer = new CustomerViewResponse
                {
                    id = customer!.Id,
                    code = customer!.Code,
                    cus_name = customer!.Name,
                    mail = customer!.Mail,
                    address = customer!.Address,
                    phone = customer!.Phone,
                    description = customer.Description,
                },
                agency = new AgencyViewResponse
                {
                    id = agency!.Id,
                    code = agency!.Code,
                    address = agency!.Address,
                    phone = agency!.Telephone,
                    agency_name = agency.AgencyName
                },
                service = new ServiceNotInContractViewResponse
                {
                    id = service!.Id,
                    code = service!.Code,
                    service_name = service!.ServiceName
                },
                request_description = reportSchedule!.Description,
            };
            return new ObjectModelResponse(request!)
            {
                Type = "Request"
            };
        }
        public async Task<ResponseModel<RequestResponse>> GetListRequestsOfAgency(PaginationRequest model, Guid id, FilterStatusRequest value)
        {
            var total = await _context.Requests.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).ToListAsync();
            var requests = new List<RequestResponse>();
            if (value.search == null && value.status == null)
            {
                total = await _context.Requests.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).ToListAsync();
                requests = await _context.Requests.Where(a => a.IsDelete == false && a.AgencyId.Equals(id)).Select(a => new RequestResponse
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
                    contract = new ContractViewResponse
                    {
                        id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
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
                    && a.AgencyId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.AdminId != null))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.AgencyId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.AdminId != null))).Select(a => new RequestResponse
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
                    var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && a.AgencyId.Equals(id)
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.RequestName!.Contains(value.search!)
                    || a.Code!.Contains(value.search!)
                    || a.RequestDesciption!.Contains(value.search!)
                    || a.CustomerId!.Equals(customer_name)
                    || a.ContractId!.Equals(contract_name)
                    || a.ServiceId!.Equals(service_name)))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.AgencyId.Equals(id)
                    && (a.RequestName!.Contains(value.search!)
                    || a.Code!.Contains(value.search)
                    || a.RequestDesciption!.Contains(value.search!)
                    || a.CustomerId!.Equals(customer_name)
                    || a.ContractId!.Equals(contract_name)
                    || a.ServiceId!.Equals(service_name))
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

            }
            return new ResponseModel<RequestResponse>(requests)
            {
                Total = total.Count,
                Type = "Request"
            };
        }
        public async Task<ResponseModel<RequestResponse>> GetListRequests(PaginationRequest model, FilterStatusRequest value)
        {
            var total = await _context.Requests.Where(a => a.IsDelete == false).ToListAsync();
            var requests = new List<RequestResponse>();

            if (value.search == null && value.status == null)
            {
                total = await _context.Requests.Where(a => a.IsDelete == false).ToListAsync();
                requests = await _context.Requests.Where(a => a.IsDelete == false).Select(a => new RequestResponse
                {
                    id = a.Id,
                    code = a.Code,
                    request_name = a.RequestName,
                    admin_id = a.AdminId,
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
                    contract = new ContractViewResponse
                    {
                        id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
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
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.AdminId != null))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.AdminId != null))).Select(a => new RequestResponse
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
                else if (value.search.ToLower().Contains("system"))
                {
                    var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && a.IsSystem == true
                    && (a.RequestStatus!.Contains(value.status!))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.IsSystem == true
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
                    var agency_name = await _context.Agencies.Where(a => a.AgencyName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var customer_name = await _context.Customers.Where(a => a.Name!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var contract_name = await _context.Contracts.Where(a => a.ContractName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    var service_name = await _context.Services.Where(a => a.ServiceName!.Contains(value.search!)).Select(a => a.Id).FirstOrDefaultAsync();
                    total = await _context.Requests.Where(a => a.IsDelete == false
                    && (a.RequestStatus!.Contains(value.status!)
                    && (a.RequestName!.Contains(value.search!)
                    || a.Code!.Contains(value.search!)
                    || a.RequestDesciption!.Contains(value.search!)
                    || a.AgencyId!.Equals(agency_name)
                    || a.CustomerId!.Equals(customer_name)
                    || a.ContractId!.Equals(contract_name)
                    || a.ServiceId!.Equals(service_name)))).ToListAsync();
                    requests = await _context.Requests.Where(a => a.IsDelete == false
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

            }

            return new ResponseModel<RequestResponse>(requests)
            {
                Total = total.Count,
                Type = "Requests"
            };
        }

        public async Task<ResponseModel<DeviceResponse>> GetDeviceRequest(PaginationRequest model, Guid id)
        {
            var request = await _context.Requests.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var agency = await _context.Agencies.Where(a => a.Id.Equals(request!.AgencyId) && a.IsDelete == false).FirstOrDefaultAsync();
            var total = await _context.Devices.Where(a => a.AgencyId.Equals(agency!.Id) && a.IsDelete == false).ToListAsync();
            var device = await _context.Devices.Where(a => a.AgencyId.Equals(agency!.Id) && a.IsDelete == false).Select(a => new DeviceResponse
            {
                id = a.Id,
                code = a.Code,
                agency = new AgencyViewResponse
                {
                    id = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Id).FirstOrDefault(),
                    code = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Code).FirstOrDefault(),
                    agency_name = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                    address = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Address).FirstOrDefault(),
                    phone = _context.Agencies.Where(x => x.Id.Equals(a.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                },
                devicetype = new DeviceTypeViewResponse
                {
                    id = _context.DeviceTypes.Where(x => x.Id.Equals(a.DeviceTypeId)).Select(x => x.Id).FirstOrDefault(),
                    service_id = _context.DeviceTypes.Where(x => x.Id.Equals(a.DeviceTypeId)).Select(x => x.ServiceId).FirstOrDefault(),
                    device_type_name = _context.DeviceTypes.Where(x => x.Id.Equals(a.DeviceTypeId)).Select(x => x.DeviceTypeName).FirstOrDefault(),
                    code = _context.DeviceTypes.Where(x => x.Id.Equals(a.DeviceTypeId)).Select(x => x.Code).FirstOrDefault(),
                },
                device_name = a.DeviceName,
                guaranty_start_date = a.GuarantyStartDate,
                guaranty_end_date = a.GuarantyEndDate,
                setting_date = a.SettingDate,
                other = a.Other,
                is_delete = a.IsDelete,
                create_date = a.CreateDate,
                update_date = a.UpdateDate,


            }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            return new ResponseModel<DeviceResponse>(device)
            {
                Total = total.Count,
                Type = "Devices"
            };
        }
        public async Task<ResponseModel<TechnicianOfRequestResponse>> GetTechnicianRequest(Guid id)
        {

            var request = await _context.Requests.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            var agency = await _context.Agencies.Where(a => a.Id.Equals(request!.AgencyId)).FirstOrDefaultAsync();
            var area = await _context.Areas.Where(a => a.Id.Equals(agency!.AreaId)).FirstOrDefaultAsync();
            var service = await _context.Services.Where(a => a.Id.Equals(request!.ServiceId)).FirstOrDefaultAsync();
            var technicians = new List<TechnicianOfRequestResponse>();
            var technicianList = new List<TechnicianOfRequestResponse>();
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
                foreach (var item in total)
                {
                    date = date.AddDays((-date.Day) + 1).Date;
                    var requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CurrentTechnicianId.Equals(item.TechnicianId)
                    && a.RequestStatus.Equals("COMPLETED")
                    && a.CreateDate!.Value.Date >= date
                    && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                    var count = requests.Count;
                    technicians.Add(new TechnicianOfRequestResponse
                    {
                        id = item.TechnicianId,
                        code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        number_of_requests = count,
                        area = area!.AreaName,
                        skills = _context.Skills.Where(a => a.TechnicianId.Equals(item!.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                    });
                }
            }
            else
            {
                total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
                && a.Technician.IsBusy == false
                && a.Technician.IsDelete == false).ToListAsync();
                foreach (var item in total)
                {
                    date = date.AddDays((-date.Day) + 1).Date;
                    var requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CurrentTechnicianId.Equals(item.TechnicianId)
                    && a.RequestStatus.Equals("COMPLETED")
                    && a.CreateDate!.Value.Date >= date
                    && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                    var count = requests.Count;
                    technicians.Add(new TechnicianOfRequestResponse
                    {
                        id = item.TechnicianId,
                        code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        number_of_requests = count,
                        area = area!.AreaName,
                        skills = _context.Skills.Where(a => a.TechnicianId.Equals(item!.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                    });
                }

            }

            technicianList = technicians.OrderBy(x => x.number_of_requests).ToList();
            return new ResponseModel<TechnicianOfRequestResponse>(technicianList)
            {
                Total = total.Count,
                Type = "Technicians"
            };
        }
        public async Task<ResponseModel<TechnicianOfRequestResponse>> GetTechnicianRequestAdmin(Guid agency_id, Guid service_id)
        {

            var agency = await _context.Agencies.Where(a => a.Id.Equals(agency_id)).FirstOrDefaultAsync();
            var area = await _context.Areas.Where(a => a.Id.Equals(agency!.AreaId)).FirstOrDefaultAsync();
            var service = await _context.Services.Where(a => a.Id.Equals(service_id)).FirstOrDefaultAsync();
            var technicians = new List<TechnicianOfRequestResponse>();
            var technicianList = new List<TechnicianOfRequestResponse>();
            var total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
            && a.Technician.AreaId.Equals(area!.Id)
            && a.Technician.IsBusy == false
            && a.Technician.IsDelete == false
            && a.TechnicianId.Equals(agency!.TechnicianId)).Include(a => a.Technician).ToListAsync();
            DateTime date = DateTime.UtcNow.AddHours(7);

            total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
            && a.Technician.AreaId.Equals(area!.Id)
            && a.Technician.IsBusy == false
            && a.Technician.IsDelete == false).ToListAsync();
            if (total.Count > 0)
            {
                total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
                && a.Technician.AreaId.Equals(area!.Id)
                && a.Technician.IsBusy == false
                && a.Technician.IsDelete == false).ToListAsync();
                foreach (var item in total)
                {
                    date = date.AddDays((-date.Day) + 1).Date;
                    var requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CurrentTechnicianId.Equals(item.TechnicianId)
                    && a.RequestStatus.Equals("COMPLETED")
                    && a.CreateDate!.Value.Date >= date
                    && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                    var count = requests.Count;
                    technicians.Add(new TechnicianOfRequestResponse
                    {
                        id = item.TechnicianId,
                        code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        number_of_requests = count,
                        area = area!.AreaName,
                        skills = _context.Skills.Where(a => a.TechnicianId.Equals(item!.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                    });
                }
            }
            else
            {
                total = await _context.Skills.Where(a => a.ServiceId.Equals(service!.Id)
                && a.Technician.IsBusy == false
                && a.Technician.IsDelete == false).ToListAsync();
                foreach (var item in total)
                {
                    date = date.AddDays((-date.Day) + 1).Date;
                    var requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CurrentTechnicianId.Equals(item.TechnicianId)
                    && a.RequestStatus.Equals("COMPLETED")
                    && a.CreateDate!.Value.Date >= date
                    && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                    var count = requests.Count;
                    technicians.Add(new TechnicianOfRequestResponse
                    {
                        id = item.TechnicianId,
                        code = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        technician_name = _context.Technicians.Where(a => a.IsDelete == false && a.Id.Equals(item.TechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        number_of_requests = count,
                        area = area!.AreaName,
                        skills = _context.Skills.Where(a => a.TechnicianId.Equals(item!.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                    });
                }

            }



            technicianList = technicians.OrderBy(x => x.number_of_requests).ToList();
            return new ResponseModel<TechnicianOfRequestResponse>(technicianList)
            {
                Total = total.Count,
                Type = "Technicians"
            };
        }
        public async Task<ObjectModelResponse> GetDetailsRequest(Guid id)
        {
            var request = await _context.Requests.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var request_details = new RequestDetailsResponse();
            TimeSpan? durationTime;
            var result = "";
            if (request!.EndTime != null && request.StartTime != null)
            {
                durationTime = request!.EndTime.Value.Subtract(request!.StartTime.Value);
                result = string.Format("{0:D2}:{1:D2}:{2:D2}", durationTime.Value.Days, durationTime.Value.Hours, durationTime.Value.Minutes);
            }
            else
            {
                durationTime = null;
            }
            if (request!.AdminId == null)
            {
                request_details = await _context.Requests.Where(a => a.Id.Equals(id) && a.IsDelete == false).Select(a => new RequestDetailsResponse
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
                    is_system = a.IsSystem,
                    start_time = a.StartTime,
                    end_time = a.EndTime,
                    duration_time = result,
                    cancel_reason = a.CancelReason,
                    reject_reason = a.ReasonReject,
                    description = a.RequestDesciption,
                    request_status = a.RequestStatus,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    create_by = new CreateByViewModel
                    {
                        id = a.CustomerId,
                        code = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Customers.Where(x => x.Id.Equals(a.CustomerId)).Select(a => a.Name).FirstOrDefault(),
                        role = "Customer",
                    },
                    technicican = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                    },
                    contract = new ContractViewResponse
                    {
                        id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                    }
                }).FirstOrDefaultAsync();
            }
            else
            {
                request_details = await _context.Requests.Where(a => a.Id.Equals(id) && a.IsDelete == false).Select(a => new RequestDetailsResponse
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
                    duration_time = result,
                    start_time = a.StartTime,
                    end_time = a.EndTime,
                    cancel_reason = a.CancelReason,
                    reject_reason = a.ReasonReject,
                    description = a.RequestDesciption,
                    request_status = a.RequestStatus,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate,
                    create_by = new CreateByViewModel
                    {
                        id = a.AdminId,
                        code = _context.Admins.Where(x => x.Id.Equals(a.AdminId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Admins.Where(x => x.Id.Equals(a.AdminId)).Select(a => a.Name).FirstOrDefault(),
                        role = "Admin",
                    },
                    technicican = new TechnicianViewResponse
                    {
                        id = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Code).FirstOrDefault(),
                        tech_name = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.TechnicianName).FirstOrDefault(),
                        email = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Email).FirstOrDefault(),
                        phone = _context.Technicians.Where(x => x.Id.Equals(a.CurrentTechnicianId)).Select(a => a.Telephone).FirstOrDefault(),
                    },
                    contract = new ContractViewResponse
                    {
                        id = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Id).FirstOrDefault(),
                        code = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.Code).FirstOrDefault(),
                        name = _context.Contracts.Where(x => x.Id.Equals(a.ContractId)).Select(a => a.ContractName).FirstOrDefault(),
                    }
                }).FirstOrDefaultAsync();
            }




            return new ObjectModelResponse(request_details!)
            {
                Type = "Request"
            };

        }
        public async Task<ObjectModelResponse> MappingTechnicianRequest(Guid request_id, Guid technician_id)
        {
            var request = await _context.Requests.Where(a => a.Id.Equals(request_id) && a.IsDelete == false).FirstOrDefaultAsync();
            var technician_current = await _context.Technicians.Where(a => a.Id.Equals(request!.CurrentTechnicianId) && a.IsDelete == false).FirstOrDefaultAsync();
            if (technician_current != null)
            {
                technician_current!.IsBusy = false;
            }
            var technician = await _context.Technicians.Where(a => a.Id.Equals(technician_id) && a.IsDelete == false).FirstOrDefaultAsync();
            request!.CurrentTechnicianId = technician_id;
            request!.StartTime = DateTime.UtcNow.AddHours(7);
            request!.UpdateDate = DateTime.UtcNow.AddHours(7);
            request.RequestStatus = ProcessStatus.PREPARING.ToString();
            var data = new MappingTechnicianResponse();
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.RE.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "You have a request need to resolve!",
                CurrentObject_Id = request.Id,
                UserId = technician_id,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage", technician_id);
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new MappingTechnicianResponse
                {
                    id = request_id,
                    technician_id = technician_id,
                    request_status = request.RequestStatus,
                };
            }

            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Request"
            };
        }
        public async Task<ObjectModelResponse> CreateRequest(RequestRequest model)
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
            var num = await GetLastCode();
            var code = CodeHelper.GeneratorCode("RE", num + 1);
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
            var customer_id = await _context.Agencies.Where(a => a.Id.Equals(model.agency_id)).Select(a => a.CustomerId).FirstOrDefaultAsync();
            var contracts = await _context.Contracts.Where(a => a.CustomerId.Equals(customer_id)).ToListAsync();
            Guid? contract_id = null;
            foreach (var item in contracts)
            {
                var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(item.Id)).ToListAsync();
                foreach (var item1 in contract_services)
                {
                    if (item1.ServiceId.Equals(model.service_id))
                    {
                        contract_id = item1.ContractId;
                    }
                }
            }
            var request = new Request
            {
                Id = request_id,
                Code = code,
                RequestName = model.request_name,
                CustomerId = customer_id,
                ServiceId = model.service_id,
                AgencyId = model.agency_id,
                RequestDesciption = model.request_description,
                RequestStatus = ProcessStatus.PENDING.ToString(),
                ReasonReject = null,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7),
                IsDelete = false,
                Feedback = null,
                Rating = 0,
                CurrentTechnicianId = null,
                StartTime = null,
                EndTime = null,
                AdminId = null,
                IsSystem = false,
                ContractId = contract_id
            };
            var data = new RequestCreateResponse();
            var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
            foreach (var item in admins)
            {
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    CurrentObject_Id = request.Id,
                    NotificationContent = "You have a new request!",
                    UserId = item.Id,
                    ObjectName = ObjectName.RE.ToString(),
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.Id);
            }
            await _context.Requests.AddAsync(request);
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new RequestCreateResponse
                {
                    id = request.Id,
                    code = request.Code,
                    request_name = request.RequestName,
                    request_description = request.RequestDesciption,
                    phone = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                    agency_name = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                    service_name = _context.Services.Where(x => x.Id.Equals(request.ServiceId)).Select(x => x.ServiceName).FirstOrDefault(),
                    customer_name = _context.Customers.Where(x => x.Id.Equals(request.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                };
            }
            return new ObjectModelResponse(data)
            {
                Type = "Request"
            };
        }
        public async Task<ObjectModelResponse> CreateRequestByAdmin(RequestAdminRequest model)
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
            var num = await GetLastCode();
            var code = CodeHelper.GeneratorCode("RE", num + 1);
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
            var contracts = await _context.Contracts.Where(a => a.CustomerId.Equals(model.customer_id)).ToListAsync();
            Guid? contract_id = null;
            foreach (var item in contracts)
            {
                var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(item.Id)).ToListAsync();
                foreach (var item1 in contract_services)
                {
                    if (item1.ServiceId.Equals(model.service_id))
                    {
                        contract_id = item1.ContractId;
                    }
                }
            }
            var request = new Request
            {
                Id = request_id,
                Code = code,
                RequestName = model.request_name,
                CustomerId = model.customer_id,
                ServiceId = model.service_id,
                AgencyId = model.agency_id,
                RequestDesciption = model.request_description,
                RequestStatus = ProcessStatus.PREPARING.ToString(),
                ReasonReject = null,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7),
                IsDelete = false,
                Feedback = null,
                Rating = 0,
                CurrentTechnicianId = model.technician_id,
                StartTime = DateTime.UtcNow.AddHours(7),
                EndTime = null,
                AdminId = model.admin_id,
                ContractId = contract_id,
                IsSystem = false,

            };
            var data = new RequestCreateResponse();
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.RE.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "You have a request need to resolve!",
                CurrentObject_Id = request.Id,
                UserId = request.CurrentTechnicianId,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CurrentTechnicianId);
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.RE.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "You have a new request created by Admin!",
                CurrentObject_Id = request.Id,
                UserId = request.CustomerId,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CustomerId);
            await _context.Requests.AddAsync(request);
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new RequestCreateResponse
                {
                    id = request.Id,
                    code = request.Code,
                    request_name = request.RequestName,
                    request_description = request.RequestDesciption,
                    phone = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                    agency_name = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                    customer_name = _context.Customers.Where(x => x.Id.Equals(request.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                    service_name = _context.Services.Where(x => x.Id.Equals(request.ServiceId)).Select(x => x.ServiceName).FirstOrDefault(),
                    technician_name = _context.Technicians.Where(x => x.Id.Equals(request.CurrentTechnicianId)).Select(x => x.TechnicianName).FirstOrDefault(),
                };


            }
            return new ObjectModelResponse(data)
            {
                Type = "Request"
            };
        }

        private async Task<int> GetLastCode()
        {
            var request = await _context.Requests.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(request!.Code!);
        }
        public async Task<ObjectModelResponse> GetContractByCustomerSerivce(Guid cus_id, Guid service_id)
        {
            var contracts = await _context.Contracts.Where(a => a.CustomerId.Equals(cus_id)).ToListAsync();
            Guid? contract_id = null;
            foreach (var item in contracts)
            {
                var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(item.Id)).ToListAsync();
                foreach (var item1 in contract_services)
                {
                    if (item1.ServiceId.Equals(service_id))
                    {
                        contract_id = item1.ContractId;
                    }
                }
            }
            var contract = await _context.Contracts.Where(a => a.IsDelete == false && a.Id.Equals(contract_id)).FirstOrDefaultAsync();
            return new ObjectModelResponse(contract!)
            {
                Type = "Contract"
            };
        }
        public async Task<ObjectModelResponse> UpdateRequest(Guid id, RequestUpdateRequest model)
        {
            var customer_id = await _context.Agencies.Where(a => a.Id.Equals(model.agency_id)).Select(a => a.CustomerId).FirstOrDefaultAsync();
            var contracts = await _context.Contracts.Where(a => a.CustomerId.Equals(customer_id)).ToListAsync();
            Guid? contract_id = null;
            foreach (var item in contracts)
            {
                var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(item.Id)).ToListAsync();
                foreach (var item1 in contract_services)
                {
                    if (item1.ServiceId.Equals(model.service_id))
                    {
                        contract_id = item1.ContractId;
                    }
                }
            }
            var request = await _context.Requests.Where(a => a.Id.Equals(id)).Select(x => new Request
            {
                Id = id,
                Code = x.Code,
                RequestName = model.request_name,
                CustomerId = _context.Agencies.Where(a => a.Id.Equals(model.agency_id)).Select(a => a.CustomerId).FirstOrDefault(),
                ServiceId = model.service_id,
                AgencyId = model.agency_id,
                RequestDesciption = model.request_description,
                RequestStatus = x.RequestStatus,
                CreateDate = x.CreateDate,
                ContractId = contract_id,
                UpdateDate = DateTime.UtcNow.AddHours(7),
                IsDelete = _context.Requests.Where(a => a.Id.Equals(id)).Select(x => x.IsDelete).FirstOrDefault(),
                Feedback = _context.Requests.Where(a => a.Id.Equals(id)).Select(x => x.Feedback).FirstOrDefault(),
                Rating = _context.Requests.Where(a => a.Id.Equals(id)).Select(x => x.Rating).FirstOrDefault(),
                CurrentTechnicianId = _context.Requests.Where(a => a.Id.Equals(id)).Select(x => x.CurrentTechnicianId).FirstOrDefault(),
                StartTime = _context.Requests.Where(a => a.Id.Equals(id)).Select(x => x.StartTime).FirstOrDefault(),
                EndTime = _context.Requests.Where(a => a.Id.Equals(id)).Select(x => x.EndTime).FirstOrDefault(),
            }).FirstOrDefaultAsync();
            var message = "blank";
            var status = 500;
            var data = new RequestCreateResponse();
            if (request!.RequestStatus != ProcessStatus.PENDING.ToString())
            {
                status = 400;
                message = "You just update when request status is pending";
            }
            else
            {
                status = 201;
                message = "Successfully";
                _context.Requests.Update(request);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new RequestCreateResponse
                    {
                        id = request.Id,
                        code = request.Code,
                        request_name = request.RequestName,
                        request_description = request.RequestDesciption,
                        phone = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                        technician_name = _context.Technicians.Where(x => x.Id.Equals(request.CurrentTechnicianId)).Select(x => x.TechnicianName).FirstOrDefault(),
                        customer_name = _context.Customers.Where(x => x.Id.Equals(request.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                        agency_name = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                        service_name = _context.Services.Where(x => x.Id.Equals(request.ServiceId)).Select(x => x.ServiceName).FirstOrDefault(),
                    };
                }

            }
            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Request"
            };
        }
        public async Task<ObjectModelResponse> UpdateRequestAdmin(Guid id, RequestUpdateAdminRequest model)
        {
            var contracts = await _context.Contracts.Where(a => a.CustomerId.Equals(model.customer_id)).ToListAsync();
            Guid? contract_id = null;
            foreach (var item in contracts)
            {
                var contract_services = await _context.ContractServices.Where(a => a.ContractId.Equals(item.Id)).ToListAsync();
                foreach (var item1 in contract_services)
                {
                    if (item1.ServiceId.Equals(model.service_id))
                    {
                        contract_id = item1.ContractId;
                    }
                }
            }
            var request = await _context.Requests.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            if (request!.CurrentTechnicianId != model.technician_id)
            {
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.RE.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a request need to resolve!",
                    CurrentObject_Id = request.Id,
                    UserId = model.technician_id,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", model.technician_id);
            }
            var data = new RequestCreateResponse();
            request!.UpdateDate = DateTime.UtcNow.AddHours(7);
            request!.CustomerId = model.customer_id;
            request!.AgencyId = model.agency_id;
            request!.ContractId = contract_id;
            request!.CurrentTechnicianId = model.technician_id;
            request!.RequestDesciption = model.request_description;
            request!.ServiceId = model.service_id;
            request!.RequestName = model.request_name;
            request!.StartTime = DateTime.UtcNow.AddHours(7);
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new RequestCreateResponse
                {
                    id = request.Id,
                    code = request.Code,
                    request_name = request.RequestName,
                    request_description = request.RequestDesciption,
                    phone = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.Telephone).FirstOrDefault(),
                    technician_name = _context.Technicians.Where(x => x.Id.Equals(request.CurrentTechnicianId)).Select(x => x.TechnicianName).FirstOrDefault(),
                    customer_name = _context.Customers.Where(x => x.Id.Equals(request.CustomerId)).Select(x => x.Name).FirstOrDefault(),
                    agency_name = _context.Agencies.Where(x => x.Id.Equals(request.AgencyId)).Select(x => x.AgencyName).FirstOrDefault(),
                    service_name = _context.Services.Where(x => x.Id.Equals(request.ServiceId)).Select(x => x.ServiceName).FirstOrDefault(),
                };
            }


            return new ObjectModelResponse(data)
            {
                Type = "Request"
            };
        }

        public async Task<ObjectModelResponse> CancelRequest(Guid id, RejectRequest model)
        {
            var request = await _context.Requests.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            var technician = await _context.Technicians.Where(a => a.Id.Equals(request!.CurrentTechnicianId)).FirstOrDefaultAsync();
            if (technician != null)
            {
                technician!.IsBusy = false;
            }
            request!.CancelReason = model.reason;
            request!.RequestStatus = ProcessStatus.CANCELED.ToString();
            request!.UpdateDate = DateTime.UtcNow.AddHours(7);
            var data = new ResolvingRequestResponse();
            if (request.AdminId == null && request.IsSystem == false)
            {
                var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                foreach (var item in admins)
                {
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.RE.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a request canceled!",
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
                    NotificationContent = "You have a request canceled!",
                    CurrentObject_Id = request.Id,
                    UserId = request.CurrentTechnicianId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CurrentTechnicianId);
            }
            else
            {
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.RE.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a request canceled!",
                    CurrentObject_Id = request.Id,
                    UserId = request.CustomerId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CustomerId);
                await _notificationService.createNotification(new Notification
                {
                    isRead = false,
                    ObjectName = ObjectName.RE.ToString(),
                    CreatedTime = DateTime.UtcNow.AddHours(7),
                    NotificationContent = "You have a request canceled!",
                    CurrentObject_Id = request.Id,
                    UserId = request.CurrentTechnicianId,
                });
                await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CurrentTechnicianId);
            }
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new ResolvingRequestResponse
                {
                    id = id,
                    code = request.Code,
                    name = request.RequestName,
                    status = request.RequestStatus,
                    technician = request.CurrentTechnician!.TechnicianName,
                };
            }

            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Request"
            };
        }
        public async Task CompletedRequest()
        {
            var requests = await _context.Requests.Where(a => a.IsDelete == false
            && a.RequestStatus!.Equals("RESOLVED")
            && a.EndTime!.Value.AddHours(12) <= DateTime.UtcNow.AddHours(7)).ToListAsync();
            if (requests.Count > 0)
            {
                foreach (var item in requests)
                {
                    item!.UpdateDate = DateTime.UtcNow.AddHours(7);
                    item!.RequestStatus = ProcessStatus.COMPLETED.ToString();
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.RE.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a request is completed!",
                        CurrentObject_Id = item.Id,
                        UserId = item.CustomerId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CustomerId);
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.RE.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "You have a request is completed!",
                        CurrentObject_Id = item.Id,
                        UserId = item.CurrentTechnicianId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CurrentTechnicianId);
                    var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                    foreach (var item1 in admins)
                    {
                        await _notificationService.createNotification(new Notification
                        {
                            isRead = false,
                            ObjectName = ObjectName.RE.ToString(),
                            CreatedTime = DateTime.UtcNow.AddHours(7),
                            NotificationContent = "You have a request is completed!",
                            CurrentObject_Id = item.Id,
                            UserId = item1.Id,
                        });
                        await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item1.Id);
                    }
                }
                await _context.SaveChangesAsync();
            }
            //send notify to admin, customer
        }
        public async Task WarningRequest()
        {
            var requests = await _context.Requests.Where(a => a.IsDelete == false
            && a.RequestStatus!.Equals("PREPARING")
            && a.UpdateDate!.Value.AddMinutes(1) <= DateTime.UtcNow.AddHours(7)).ToListAsync();
            if (requests.Count > 0)
            {

                foreach (var item in requests)
                {
                    item!.UpdateDate = DateTime.UtcNow.AddHours(7);
                    item!.RequestStatus = ProcessStatus.WARNING.ToString();
                    var technician = await _context.Technicians.Where(a => a.Id.Equals(item.CurrentTechnicianId)).FirstOrDefaultAsync();
                    if (technician!.Breach >= 3)
                    {
                        technician!.Breach = 3;
                    }
                    else
                    {
                        technician!.Breach = technician!.Breach + 1;
                    }
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.RE.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "Request warning because the technician have no action!",
                        CurrentObject_Id = item.Id,
                        UserId = item.CustomerId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CustomerId);
                    await _notificationService.createNotification(new Notification
                    {
                        isRead = false,
                        ObjectName = ObjectName.RE.ToString(),
                        CreatedTime = DateTime.UtcNow.AddHours(7),
                        NotificationContent = "Request warning because the technician have no action!",
                        CurrentObject_Id = item.Id,
                        UserId = item.CurrentTechnicianId,
                    });
                    await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item.CurrentTechnicianId);
                    var admins = await _context.Admins.Where(a => a.IsDelete == false).ToListAsync();
                    foreach (var item1 in admins)
                    {
                        await _notificationService.createNotification(new Notification
                        {
                            isRead = false,
                            ObjectName = ObjectName.RE.ToString(),
                            CreatedTime = DateTime.UtcNow.AddHours(7),
                            NotificationContent = "Request warning because the technician have no action!",
                            CurrentObject_Id = item.Id,
                            UserId = item1.Id,
                        });
                        await _notifyHub.Clients.All.SendAsync("ReceiveMessage", item1.Id);
                    }
                }

                await _context.SaveChangesAsync();
            }
            //send notify to admin
        }
        public async Task<ObjectModelResponse> DisableRequest(Guid id)
        {
            var request = await _context.Requests.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();
            request!.IsDelete = true;
            _context.Requests.Update(request);
            var data = new RequestDisableResponse();
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new RequestDisableResponse
                {
                    id = request.Id,
                    isDelete = request.IsDelete,
                };
            }

            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Request"
            };
        }
        public async Task<ObjectModelResponse> RejectRequest(Guid id, RejectRequest value)
        {
            var request = await _context.Requests.Where(a => a.Id.Equals(id) && a.IsDelete == false).FirstOrDefaultAsync();
            request!.RequestStatus = ProcessStatus.REJECTED.ToString();
            request!.UpdateDate = DateTime.UtcNow.AddHours(7);
            request!.ReasonReject = value.reason;
            var data = new RejectResponse();
            await _notificationService.createNotification(new Notification
            {
                isRead = false,
                ObjectName = ObjectName.RE.ToString(),
                CreatedTime = DateTime.UtcNow.AddHours(7),
                NotificationContent = "You have a request rejected by Admin!",
                CurrentObject_Id = request.Id,
                UserId = request.CustomerId,
            });
            await _notifyHub.Clients.All.SendAsync("ReceiveMessage", request.CustomerId);
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {
                data = new RejectResponse
                {
                    id = request.Id,
                    code = request.Code,
                    name = request.RequestName!,
                    status = request.RequestStatus,
                };
            }

            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Request"
            };
        }
    }
}
