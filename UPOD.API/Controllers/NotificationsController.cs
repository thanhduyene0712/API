using Microsoft.AspNetCore.Mvc;
using UPOD.REPOSITORIES.Models;
using UPOD.REPOSITORIES.RequestModels;
using UPOD.REPOSITORIES.ResponseModels;
using UPOD.SERVICES.Services;

namespace UPOD.API.Controllers
{

    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationSv;

        public NotificationsController(IConfiguration configuration, INotificationService notificationSv)
        {
            _configuration = configuration;
            _notificationSv = notificationSv;
        }
        [HttpGet]
        [Route("get_notifications")]
        public async Task<ActionResult<ResponseModel<Notification>>> GetAll([FromQuery] PaginationRequest model, Guid id)
        {
            try
            {
                return await _notificationSv.GetAll(model,id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("read_notifications")]
        public async Task<ActionResult<ObjectModelResponse>> UpdateNoti(Guid id)
        {
            try
            {
                return await _notificationSv.UpdateNoti(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        [Route("read_all_notifications")]
        public async Task<ActionResult<ResponseModel<Notification>>> UpdateAllNoti(Guid user_id)
        {
            try
            {
                return await _notificationSv.UpdateAllNoti(user_id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
