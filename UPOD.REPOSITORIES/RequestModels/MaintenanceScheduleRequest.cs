using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class MaintenanceScheduleRequest
    {
        public string? description { get; set; }
        public Guid? technician_id { get; set; }
        public DateTime? maintain_time { get; set; }
    }
}
