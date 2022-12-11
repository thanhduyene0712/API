using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;

namespace UPOD.SERVICES.Services
{

    public interface INotificationService
    {
        Task createNotification(Notification model);
        Task<ResponseModel<Notification>> GetAll(PaginationRequest model, Guid id);
        Task<ObjectModelResponse> UpdateNoti(Guid id);
        Task<ResponseModel<Notification>> UpdateAllNoti(Guid user_id);

    }

    public class NotificationServices : INotificationService
    {
        private readonly Database_UPODContext _context;
        public NotificationServices(Database_UPODContext context)
        {
            _context = context;
        }

        public async Task createNotification(Notification model)
        {
            var noti_id = Guid.NewGuid();
            while (true)
            {
                var noti_dup = await _context.Notifications.Where(x => x.Id.Equals(noti_id)).FirstOrDefaultAsync();
                if (noti_dup == null)
                {
                    break;
                }
                else
                {
                    noti_id = Guid.NewGuid();
                }
            }
            model.Id = noti_id;
            model.CreatedTime = DateTime.UtcNow.AddHours(7);
            await _context.Notifications.AddAsync(model);
            await _context.SaveChangesAsync();
        }
        public async Task<ResponseModel<Notification>> GetAll(PaginationRequest model, Guid id)
        {
            var noti = await _context.Notifications.Where(a => a.UserId.Equals(id)).OrderByDescending(a => a.CreatedTime).Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
            var total = await _context.Notifications.Where(a => a.UserId.Equals(id)).CountAsync();

            return new ResponseModel<Notification>(noti)
            {
                Total = total,
                Type = "Notifications"
            };
        }
        public async Task<ObjectModelResponse> UpdateNoti(Guid id)
        {
            var noti = await _context.Notifications.Where(a => a.Id.Equals(id) && a.isRead == false).FirstOrDefaultAsync();
            noti!.isRead = true;
            await _context.SaveChangesAsync();
            return new ObjectModelResponse(noti)
            {
                Type = "Notification"
            };
        }
        public async Task<ResponseModel<Notification>> UpdateAllNoti(Guid user_id)
        {
            var notis = await _context.Notifications.Where(a => a.UserId.Equals(user_id) && a.isRead == false).ToListAsync();
            foreach (var item in notis)
            {
                item!.isRead = true;
            }
            await _context.SaveChangesAsync();
            return new ResponseModel<Notification>(notis)
            {
                Type = "Notifications"
            };
        }
    }
}
