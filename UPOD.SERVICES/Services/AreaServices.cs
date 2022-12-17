using Microsoft.EntityFrameworkCore;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.REPOSITORIES.ResponseViewModel;
using UPOD.SERVICES.Helpers;

namespace UPOD.SERVICES.Services
{

    public interface IAreaService
    {
        Task<ResponseModel<AreaResponse>> GetListAreas(PaginationRequest model, SearchRequest value);
        Task<ObjectModelResponse> CreateArea(AreaRequest model);
        Task<ObjectModelResponse> UpdateArea(Guid id, AreaRequest model);
        Task<ObjectModelResponse> DisableArea(Guid id);
        Task<ObjectModelResponse> GetDetailsArea(Guid id);
        Task<ResponseModel<TechnicianViewResponse>> GetListTechniciansByAreaId(PaginationRequest model, Guid id, Guid cus_id);
    }

    public class AreaServices : IAreaService
    {
        private readonly Database_UPODContext _context;
        public AreaServices(Database_UPODContext context)
        {
            _context = context;
        }


        public async Task<ResponseModel<AreaResponse>> GetListAreas(PaginationRequest model, SearchRequest value)
        {
            var total = await _context.Areas.Where(a => a.IsDelete == false).ToListAsync();
            var areas = new List<AreaResponse>();
            if (value.search == null)
            {
                total = await _context.Areas.Where(a => a.IsDelete == false).ToListAsync();
                areas = await _context.Areas.Where(a => a.IsDelete == false).Select(a => new AreaResponse
                {
                    id = a.Id,
                    code = a.Code,
                    area_name = a.AreaName,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate

                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }
            else
            {
                total = await _context.Areas.Where(a => a.IsDelete == false
                && (a.Code!.Contains(value.search)
                || a.AreaName!.Contains(value.search)
                || a.Description!.Contains(value.search))).ToListAsync();
                areas = await _context.Areas.Where(a => a.IsDelete == false
                && (a.Code!.Contains(value.search)
                || a.AreaName!.Contains(value.search)
                || a.Description!.Contains(value.search))).Select(a => new AreaResponse
                {
                    id = a.Id,
                    code = a.Code,
                    area_name = a.AreaName,
                    description = a.Description,
                    is_delete = a.IsDelete,
                    create_date = a.CreateDate,
                    update_date = a.UpdateDate

                }).OrderByDescending(x => x.update_date).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            }

            return new ResponseModel<AreaResponse>(areas)
            {
                Total = total.Count,
                Type = "Areas"
            };
        }
        public async Task<ObjectModelResponse> GetDetailsArea(Guid id)
        {
            var areas = await _context.Areas.Where(a => a.IsDelete == false && a.Id.Equals(id)).Select(a => new AreaResponse
            {
                id = a.Id,
                code = a.Code,
                area_name = a.AreaName,
                description = a.Description,
                is_delete = a.IsDelete,
                create_date = a.CreateDate,
                update_date = a.UpdateDate

            }).FirstOrDefaultAsync();
            return new ObjectModelResponse(areas!)
            {
                Type = "Area"
            };
        }
        public async Task<ResponseModel<TechnicianViewResponse>> GetListTechniciansByAreaId(PaginationRequest model, Guid id, Guid cus_id)
        {
            var customer_services = await _context.ContractServices.Where(x => x.Contract!.CustomerId.Equals(cus_id)
            && x.Contract.IsDelete == false && x.Contract.IsExpire == false && x.IsDelete == false
            && (x.Contract.StartDate!.Value.Date <= DateTime.UtcNow.AddHours(7).Date
            && x.Contract.EndDate!.Value.Date >= DateTime.UtcNow.AddHours(7).Date)).Distinct().ToListAsync();
            var customer_technicians = new List<TechnicianViewResponse>();
            DateTime date = DateTime.UtcNow.AddHours(7);
            foreach (var item in customer_services)
            {

                var skill_techs = await _context.Skills.Where(a => a.IsDelete == false && a.ServiceId.Equals(item.ServiceId)).Select(a => new TechnicianViewResponse
                {
                    id = a.TechnicianId,
                    code = a.Technician!.Code,
                    email = a.Technician.Email,
                    tech_name = a.Technician.TechnicianName,
                    phone = a.Technician.Telephone,
                    area_name = _context.Areas.Where(x => x.Id.Equals(a.Technician.AreaId)).Select(a => a.AreaName).FirstOrDefault(),
                    skills = _context.Skills.Where(x => x.TechnicianId.Equals(a.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
                })/*.Distinct()*/.ToListAsync();
                foreach (var item1 in skill_techs)
                {
                    date = date.AddDays((-date.Day) + 1).Date;
                    var requests = await _context.Requests.Where(a => a.IsDelete == false
                    && a.CurrentTechnicianId.Equals(item1.id)
                    && a.RequestStatus.Equals("COMPLETED")
                    && a.CreateDate!.Value.Date >= date
                    && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                    var count = requests.Count;
                    customer_technicians.Add(new TechnicianViewResponse
                    {
                        id = item1.id,
                        code = item1.code,
                        email = item1.email,
                        tech_name = item1.tech_name,
                        phone = item1.phone,
                        number_of_requests = count,
                        area_name = item1.area_name,
                        skills = item1.skills,
                    });
                }

            }
            var skill_technicians = await _context.Skills.Where(a => a.IsDelete == false).Select(a => new TechnicianViewResponse
            {
                id = a.TechnicianId,
                code = a.Technician!.Code,
                email = a.Technician.Email,
                tech_name = a.Technician.TechnicianName,
                phone = a.Technician.Telephone,
                area_name = _context.Areas.Where(x => x.Id.Equals(a.Technician.AreaId)).Select(a => a.AreaName).FirstOrDefault(),
                skills = _context.Skills.Where(x => x.TechnicianId.Equals(a.TechnicianId)).Select(a => a.Service.ServiceName).ToList()!,
            })/*.Distinct()*/.ToListAsync();
            var skillOfTech = new List<TechnicianViewResponse>();
            var areaOfTech = new List<TechnicianViewResponse>();
            foreach (var item in skill_technicians)
            {
                date = date.AddDays((-date.Day) + 1).Date;
                var requests = await _context.Requests.Where(a => a.IsDelete == false
                && a.CurrentTechnicianId.Equals(item.id)
                && a.RequestStatus.Equals("COMPLETED")
                && a.CreateDate!.Value.Date >= date
                && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                var count = requests.Count;
                skillOfTech.Add(new TechnicianViewResponse
                {
                    id = item.id,
                    code = item.code,
                    email = item.email,
                    tech_name = item.tech_name,
                    phone = item.phone,
                    number_of_requests = count,
                    area_name = item.area_name,
                    skills = item.skills,
                });
            }
            var area_technicians = await _context.Technicians.Where(a => a.IsDelete == false && a.AreaId.Equals(id)).Select(a => new TechnicianViewResponse
            {
                id = a.Id,
                code = a.Code,
                email = a.Email,
                tech_name = a.TechnicianName,
                phone = a.Telephone,
                area_name = _context.Areas.Where(x => x.Id.Equals(a.AreaId)).Select(a => a.AreaName).FirstOrDefault(),
                skills = _context.Skills.Where(x => x.TechnicianId.Equals(a.Id)).Select(a => a.Service.ServiceName).ToList()!,
            })/*.Distinct()*/.ToListAsync();
            foreach (var item in area_technicians)
            {
                date = date.AddDays((-date.Day) + 1).Date;
                var requests = await _context.Requests.Where(a => a.IsDelete == false
                && a.CurrentTechnicianId.Equals(item.id)
                && a.RequestStatus.Equals("COMPLETED")
                && a.CreateDate!.Value.Date >= date
                && a.CreateDate!.Value.Date <= DateTime.UtcNow.AddHours(7)).ToListAsync();
                var count = requests.Count;
                areaOfTech.Add(new TechnicianViewResponse
                {
                    id = item.id,
                    code = item.code,
                    email = item.email,
                    tech_name = item.tech_name,
                    phone = item.phone,
                    number_of_requests = count,
                    area_name = item.area_name,
                    skills = item.skills,
                });
            }
            areaOfTech.Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).OrderBy(x => x.number_of_requests).ToList();
            var technicians = new List<TechnicianViewResponse>();
            var technician_check = new List<TechnicianViewResponse>();
            var technician_compares = customer_technicians.Where(i => skillOfTech.Contains(i)).ToList();
            technician_check = areaOfTech.Where(i => technician_compares.Contains(i)).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).OrderBy(x=>x.number_of_requests).ToList();
            var total = areaOfTech.Where(i => technician_compares.Contains(i)).ToList();
            if (technician_check.Count > 0)
            {
                technicians = technician_check;
            }
            else
            {
                technicians = areaOfTech;
                total = areaOfTech;
            }
            return new ResponseModel<TechnicianViewResponse>(technicians)
            {
                Total = total.Count,
                Type = "Technicians"
            };
        }

        public async Task<ObjectModelResponse> CreateArea(AreaRequest model)
        {
            var area_id = Guid.NewGuid();
            while (true)
            {
                var area_dup = await _context.Areas.Where(x => x.Id.Equals(area_id)).FirstOrDefaultAsync();
                if (area_dup == null)
                {
                    break;
                }
                else
                {
                    area_id = Guid.NewGuid();
                }
            }
            var code_number = await GetLastCode();
            var code = CodeHelper.GeneratorCode("AR", code_number + 1);
            while (true)
            {
                var code_dup = await _context.Areas.Where(a => a.Code.Equals(code)).FirstOrDefaultAsync();
                if (code_dup == null)
                {
                    break;
                }
                else
                {
                    code = "AR-" + code_number++.ToString();
                }
            }
            var area = new Area
            {
                Id = area_id,
                Code = code,
                Description = model.description,
                AreaName = model.area_name,
                IsDelete = false,
                CreateDate = DateTime.UtcNow.AddHours(7),
                UpdateDate = DateTime.UtcNow.AddHours(7)

            };
            var data = new AreaResponse();
            var message = "blank";
            var status = 500;
            var area_name = await _context.Areas.Where(x => x.AreaName!.Equals(area!.AreaName) && x.IsDelete == false).FirstOrDefaultAsync();
            if (area_name != null)
            {
                status = 400;
                message = "Name is already exists!";
            }
            else
            {
                message = "Successfully";
                status = 200;
                await _context.Areas.AddAsync(area);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new AreaResponse
                    {
                        id = area.Id,
                        code = area.Code,
                        area_name = area.AreaName,
                        description = area.Description,
                        is_delete = area.IsDelete,
                        create_date = area.CreateDate,
                        update_date = area.UpdateDate
                    };
                }

            }

            return new ObjectModelResponse(data)
            {
                Message = message,
                Status = status,
                Type = "Area"
            };
        }


        public async Task<ObjectModelResponse> DisableArea(Guid id)
        {
            var area = await _context.Areas.Where(x => x.Id.Equals(id)).FirstOrDefaultAsync();
            area!.IsDelete = true;
            area.UpdateDate = DateTime.UtcNow.AddHours(7);
            _context.Areas.Update(area);
            var data = new AreaResponse();
            var rs = await _context.SaveChangesAsync();
            if (rs > 0)
            {

            }
            data = new AreaResponse
            {
                id = id,
                code = area.Code,
                description = area.Description,
                area_name = area.AreaName,
                is_delete = area.IsDelete,
                create_date = area.CreateDate,
                update_date = area.UpdateDate
            };
            return new ObjectModelResponse(data)
            {
                Status = 201,
                Type = "Area"
            };
        }
        public async Task<ObjectModelResponse> UpdateArea(Guid id, AreaRequest model)
        {
            var area = await _context.Areas.Where(a => a.Id.Equals(id)).FirstOrDefaultAsync();

            var data = new AreaResponse();
            var message = "blank";
            var status = 500;
            var area_name = await _context.Areas.Where(x => x.AreaName!.Equals(model!.area_name) && x.IsDelete == false).FirstOrDefaultAsync();
            if (area!.AreaName != model.area_name && area_name != null)
            {
                status = 400;
                message = "Name is already exists!";
            }
            else
            {
                message = "Successfully";
                status = 200;
                area.AreaName = model.area_name;
                area.Description = model.description;
                area.UpdateDate = DateTime.UtcNow.AddHours(7);
                var rs = await _context.SaveChangesAsync();
                if (rs > 0)
                {
                    data = new AreaResponse
                    {
                        id = area.Id,
                        code = area.Code,
                        area_name = area.AreaName,
                        description = area.Description,
                        is_delete = area.IsDelete,
                        create_date = area.CreateDate,
                        update_date = area.UpdateDate
                    };
                }
            }

            return new ObjectModelResponse(data)
            {
                Status = status,
                Message = message,
                Type = "Area"
            };
        }
        private async Task<int> GetLastCode()
        {
            var area = await _context.Areas.OrderBy(x => x.Code).LastOrDefaultAsync();
            return CodeHelper.StringToInt(area!.Code!);
        }
    }
}
