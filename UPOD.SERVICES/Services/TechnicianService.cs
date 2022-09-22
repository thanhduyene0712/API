﻿using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponeModels;
using UPOD.REPOSITORIES.Services;

namespace UPOD.SERVICES.Services
{

    public interface ITechnicianService
    {
        Task<ResponseModel<TechnicianResponse>> GetListTechnicians(PaginationRequest model);
        Task<ResponseModel<TechnicianResponse>> GetDetailTechnician(Guid id);
        Task<ResponseModel<TechnicianResponse>> CreateTechnician(TechnicianRequest model);
        Task<ResponseModel<TechnicianResponse>> UpdateTechnician(Guid id, TechnicianRequest model);
        Task<ResponseModel<TechnicianResponse>> DisableTechnician(Guid id);
    }

    public class TechnicianService : ITechnicianService
    {
        private readonly Database_UPODContext _context;
        public TechnicianService(Database_UPODContext context)
        {
            _context = context;
        }

        public async Task<ResponseModel<TechnicianResponse>> GetListTechnicians(PaginationRequest model)
        {
            var technicians = await _context.Technicans.Where(a => a.IsDelete == false).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).Select(a => new TechnicianResponse
            {
                id = a.Id,
                area_id = a.AreaId,
                technican_name = a.TechnicanName,
                account_id = a.AccountId,
                telephone = a.Telephone,
                email = a.Email,
                gender = a.Gender,
                address = a.Address,
                rating_avg = a.RatingAvg,
                is_busy = a.IsBusy,
                is_delete = a.IsDelete,
                create_date = a.CreateDate,
                update_date = a.UpdateDate,
                service_id = _context.Skills.Where(x => x.TechnicanId.Equals(a.Id)).Select(a => a.ServiceId).ToList(),
            }).ToListAsync();

            return new ResponseModel<TechnicianResponse>(technicians)
            {
                Total = technicians.Count,
                Type = "Technicians"
            };
        }
        public async Task<ResponseModel<TechnicianResponse>> GetDetailTechnician(Guid id)
        {
            var technician = await _context.Technicans.Where(a => a.IsDelete == false && a.Id.Equals(id)).Select(a => new TechnicianResponse
            {
                id = a.Id,
                area_id = a.AreaId,
                technican_name = a.TechnicanName,
                account_id = a.AccountId,
                telephone = a.Telephone,
                email = a.Email,
                gender = a.Gender,
                address = a.Address,
                rating_avg = a.RatingAvg,
                is_busy = a.IsBusy,
                is_delete = a.IsDelete,
                create_date = a.CreateDate,
                update_date = a.UpdateDate,
                service_id = _context.Skills.Where(x => x.TechnicanId.Equals(a.Id)).Select(a => a.ServiceId).ToList(),
            }).ToListAsync();
            return new ResponseModel<TechnicianResponse>(technician)
            {
                Total = technician.Count,
                Type = "Technician"
            };
        }

        //public async Task<ResponseModel<Tick>> CreateTicket(TechnicianRequest model)
        //{

        //    var technician = new Technican
        //    {
        //        Id = Guid.NewGuid(),
        //        AreaId = model.area_id,
        //        TechnicanName = model.technican_name,
        //        AccountId = model.account_id,
        //        Telephone = model.telephone,
        //        Address = model.address,
        //        Email = model.email,
        //        Gender = model.gender,
        //        RatingAvg = model.rating_avg,
        //        IsBusy = false,
        //        IsDelete = false,
        //        CreateDate = DateTime.Now,
        //        UpdateDate = DateTime.Now

        //    };
        //    foreach (var item in model.service_id)
        //    {
        //        var skill = new Skill
        //        {
        //            Id = Guid.NewGuid(),
        //            TechnicanId = technician.Id,
        //            ServiceId = item,
        //            IsDelete = false,
        //            CreateDate = DateTime.Now,
        //            UpdateDate = DateTime.Now,
        //        };
        //        _context.Skills.Add(skill);
        //    }
        //    var list = new List<TechnicianResponse>();
        //    var message = "blank";
        //    var status = 500;
        //    var technician_id = await _context.Technicans.Where(x => x.Id.Equals(technician.Id)).FirstOrDefaultAsync();
        //    if (technician_id != null)
        //    {
        //        status = 400;
        //        message = "TechnicianId is already exists!";
        //    }
        //    else
        //    {
        //        message = "Successfully";
        //        status = 201;
        //        await _context.Technicans.AddAsync(technician);
        //        await _context.SaveChangesAsync();
        //        list.Add(new TechnicianResponse
        //        {
        //            id = technician.Id,
        //            area_id = technician.AreaId,
        //            technican_name = technician.TechnicanName,
        //            account_id = technician.AccountId,
        //            telephone = technician.Telephone,
        //            email = technician.Email,
        //            gender = technician.Gender,
        //            address = technician.Address,
        //            rating_avg = technician.RatingAvg,
        //            is_busy = technician.IsBusy,
        //            is_delete = technician.IsDelete,
        //            create_date = technician.CreateDate,
        //            update_date = technician.UpdateDate,
        //            service_id = _context.Skills.Where(x => x.TechnicanId.Equals(technician.Id)).Select(a => a.ServiceId).ToList(),

        //        });
        //    }

        //    return new ResponseModel<TechnicianResponse>(list)
        //    {
        //        Message = message,
        //        Status = status,
        //        Total = list.Count,
        //        Type = "Technician"
        //    };
        //}
        public async Task<ResponseModel<TechnicianResponse>> CreateTechnician(TechnicianRequest model)
        {

            var technician = new Technican
            {
                Id = Guid.NewGuid(),
                AreaId = model.area_id,
                TechnicanName = model.technican_name,
                AccountId = model.account_id,
                Telephone = model.telephone,
                Address = model.address,
                Email = model.email,
                Gender = model.gender,
                RatingAvg = model.rating_avg,
                IsBusy = false,
                IsDelete = false,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now

            };
            foreach (var item in model.service_id)
            {
                var skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    TechnicanId = technician.Id,
                    ServiceId = item,
                    IsDelete = false,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                };
                _context.Skills.Add(skill);
            }
            var list = new List<TechnicianResponse>();
            var message = "blank";
            var status = 500;
            var technician_id = await _context.Technicans.Where(x => x.Id.Equals(technician.Id)).FirstOrDefaultAsync();
            if (technician_id != null)
            {
                status = 400;
                message = "TechnicianId is already exists!";
            }
            else
            {
                message = "Successfully";
                status = 201;
                await _context.Technicans.AddAsync(technician);
                await _context.SaveChangesAsync();
                list.Add(new TechnicianResponse
                {
                    id = technician.Id,
                    area_id = technician.AreaId,
                    technican_name = technician.TechnicanName,
                    account_id = technician.AccountId,
                    telephone = technician.Telephone,
                    email = technician.Email,
                    gender = technician.Gender,
                    address = technician.Address,
                    rating_avg = technician.RatingAvg,
                    is_busy = technician.IsBusy,
                    is_delete = technician.IsDelete,
                    create_date = technician.CreateDate,
                    update_date = technician.UpdateDate,
                    service_id = _context.Skills.Where(x => x.TechnicanId.Equals(technician.Id)).Select(a => a.ServiceId).ToList(),

                });
            }

            return new ResponseModel<TechnicianResponse>(list)
            {
                Message = message,
                Status = status,
                Total = list.Count,
                Type = "Technician"
            };
        }


        public async Task<ResponseModel<TechnicianResponse>> DisableTechnician(Guid id)
        {
            var technician = await _context.Technicans.Where(x => x.Id.Equals(id)).FirstOrDefaultAsync();
            technician.IsDelete = true;
            technician.UpdateDate = DateTime.Now;
            _context.Technicans.Update(technician);
            await _context.SaveChangesAsync();
            var list = new List<TechnicianResponse>();
            list.Add(new TechnicianResponse
            {
                is_delete = technician.IsDelete,
            });
            return new ResponseModel<TechnicianResponse>(list)
            {
                Status = 201,
                Total = list.Count,
                Type = "Technician"
            };
        }
        public async Task<ResponseModel<TechnicianResponse>> UpdateTechnician(Guid id, TechnicianRequest model)
        {
            var technician = await _context.Technicans.Where(a => a.Id.Equals(id)).Select(x => new Technican
            {
                Id = id,
                AreaId = model.area_id,
                TechnicanName = model.technican_name,
                AccountId = model.account_id,
                Telephone = model.telephone,
                Address = model.address,
                Email = model.email,
                Gender = model.gender,
                RatingAvg = model.rating_avg,
                IsBusy = x.IsBusy,
                IsDelete = x.IsDelete,
                CreateDate = x.CreateDate,
                UpdateDate = DateTime.Now
            }).FirstOrDefaultAsync();
            var skill_remove = await _context.Skills.Where(a => a.TechnicanId.Equals(id)).ToListAsync();
            foreach (var item in skill_remove)
            {
                _context.Skills.Remove(item);
            }
            foreach (var item in model.service_id)
            {
                var skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    TechnicanId = technician.Id,
                    ServiceId = item,
                    IsDelete = false,
                    CreateDate = technician.CreateDate,
                    UpdateDate = DateTime.Now,
                };
                _context.Skills.Add(skill);
            }
            _context.Technicans.Update(technician);
            await _context.SaveChangesAsync();
            var list = new List<TechnicianResponse>();
            list.Add(new TechnicianResponse
            {
                id = technician.Id,
                area_id = technician.AreaId,
                technican_name = technician.TechnicanName,
                account_id = technician.AccountId,
                telephone = technician.Telephone,
                email = technician.Email,
                gender = technician.Gender,
                address = technician.Address,
                rating_avg = technician.RatingAvg,
                is_busy = technician.IsBusy,
                is_delete = technician.IsDelete,
                create_date = technician.CreateDate,
                update_date = technician.UpdateDate,
                service_id = _context.Skills.Where(a => a.TechnicanId.Equals(technician.Id)).Select(a => a.ServiceId).ToList(),
            });
            return new ResponseModel<TechnicianResponse>(list)
            {
                Status = 201,
                Total = list.Count,
                Type = "Technician"
            };
        }

    }
}
