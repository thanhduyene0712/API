using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class MaintenanceReportServiceRequest
    {
        public Guid? service_id { get; set; }
        public string? Description { get; set; }
        public bool? is_resolved { get; set; }
        public List<string>? img { get; set; }
    }
}
