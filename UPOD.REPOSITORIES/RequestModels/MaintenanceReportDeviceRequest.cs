using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class MaintenanceReportDeviceRequest
    {
        public Guid? device_id { get; set; }
        public Guid? service_id { get; set; }
        public string? description { get; set; }
        public string? solution { get; set; }
        public List<string>? img { get; set; }
    }
}
