using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class MaintenanceScheduleResponse
    {
        public Guid id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public DateTime? maintain_time { get; set; }
        public bool? is_delete { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public DateTime? start_time { get; set; }
        public DateTime? end_time { get; set; }
        public string? duration_time { get; set; }
        public string? code { get; set; }
        public string? status { get; set; }
        public ContractViewResponse contract { get; set; } = null!;
        public TechnicianViewResponse technician { get; set; } = null!;
        public AgencyViewResponse agency { get; set; } = null!;
        public CustomerViewResponse customer { get; set; } = null!;
    }
}
