using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class MaintenanceReportResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? name { get; set; }
        public bool? is_delete { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public string? status { get; set; }
        public bool? is_processed { get; set; }
        public AgencyViewResponse agency { get; set; } = null!;
        public CustomerViewResponse customer { get; set; } = null!;
        public TechnicianViewResponse create_by { get; set; } = null!;
        public MaintenanceReportViewResponse maintenance_schedule { get; set; } = null!;
        public List<ServiceReportResponse> service { get; set; } = null!;
    }
}
