using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class MaintenanceReport
    {
        public MaintenanceReport()
        {
            MaintenanceReportServices = new HashSet<MaintenanceReportService>();
        }

        public Guid Id { get; set; }
        public Guid? AgencyId { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public Guid? MaintenanceScheduleId { get; set; }
        public string? Code { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? CreateBy { get; set; }
        public string? Status { get; set; }
        public string? Name { get; set; }
        public bool? IsProcessed { get; set; }

        public virtual Agency? Agency { get; set; }
        public virtual Technician? CreateByNavigation { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual MaintenanceSchedule? MaintenanceSchedule { get; set; }
        public virtual ICollection<MaintenanceReportService> MaintenanceReportServices { get; set; }
    }
}
