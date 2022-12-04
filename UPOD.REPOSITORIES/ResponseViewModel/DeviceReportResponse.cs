using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseViewModel
{
    public class DeviceReportResponse
    {
        public string? service_name { get; set; }
        public string? device_name { get; set; }
        public string? description { get; set; }
        public string? solution { get; set; }
        public List<string>? img { get; set; }
    }
}
