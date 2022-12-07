using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class Service
    {
        public Service()
        {
            ContractServices = new HashSet<ContractService>();
            DeviceTypes = new HashSet<DeviceType>();
            MaintenanceReportServices = new HashSet<MaintenanceReportService>();
            Requests = new HashSet<Request>();
            Skills = new HashSet<Skill>();
        }

        public Guid Id { get; set; }
        public string? ServiceName { get; set; }
        public string? Description { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? Code { get; set; }
        public string? Guideline { get; set; }

        public virtual ICollection<ContractService> ContractServices { get; set; }
        public virtual ICollection<DeviceType> DeviceTypes { get; set; }
        public virtual ICollection<MaintenanceReportService> MaintenanceReportServices { get; set; }
        public virtual ICollection<Request> Requests { get; set; }
        public virtual ICollection<Skill> Skills { get; set; }
    }
}
