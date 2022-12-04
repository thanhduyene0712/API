using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.Models
{
    public class MaintenanceReportDevice
    {
        public Guid Id { get; set; }
        public Guid? MaintenanceReportId { get; set; }
        public Guid? ServiceId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Description { get; set; }
        public string? Solution { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Service? Service { get; set; }
        public virtual MaintenanceReport? MaintenanceReport { get; set; }
    }
}
