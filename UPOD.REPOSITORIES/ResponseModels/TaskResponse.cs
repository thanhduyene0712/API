using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class TaskResponse
    {
        public List<RequestResponse> request { get; set; } = null!;
        public List<MaintenanceScheduleResponse> maintain { get; set; } = null!;
    }
}
