using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseViewModel
{
    public class ServiceReportResponse
    {
        public Guid? report_service_id { get; set; }
        public string? code { get; set; }
        public Guid? service_id { get; set; }
        public Guid? request_id { get; set; }
        public string? service_name { get; set; }
        public string? description { get; set; }
        public bool? created { get; set; }
        public bool? is_resolved { get; set; }
        public List<string>? img { get; set; }
    }
}
