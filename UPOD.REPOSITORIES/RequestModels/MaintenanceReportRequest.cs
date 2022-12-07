using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class MaintenanceReportRequest
    {
        public string? name { get; set; }
        public Guid? maintenance_schedule_id { get; set; }
        public List<MaintenanceReportServiceRequest> service { get; set; } = null!;
    }
}
