using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class RequestDetailsResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? request_name { get; set; }
        public string? request_status { get; set; }
        public string? reject_reason { get; set; }
        public string? cancel_reason { get; set; }
        public string? description { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public string? duration_time { get; set; }
        public DateTime? start_time { get; set; }
        public DateTime? end_time { get; set; }
        public bool? is_system { get; set; }
        public CreateByViewModel create_by { get; set; } = null!;
        public ContractViewResponse contract { get; set; } = null!;
        public CustomerViewResponse customer { get; set; } = null!;
        public AgencyViewResponse agency { get; set; } = null!;
        public ServiceViewResponse service { get; set; } = null!;
        public TechnicianViewResponse technicican { get; set; } = null!;

    }
}
