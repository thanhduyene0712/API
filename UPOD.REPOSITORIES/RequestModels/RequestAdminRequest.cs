using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class RequestAdminRequest
    {
        public Guid? customer_id { get; set; }
        public Guid? agency_id { get; set; }
        public Guid? service_id { get; set; }
        public Guid? admin_id { get; set; }
        public Guid? technician_id { get; set; }
        public string? request_description { get; set; }
        public string? request_name { get; set; }
    }
}
