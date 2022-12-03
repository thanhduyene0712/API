﻿using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class Technician
    {
        public Technician()
        {
            Agencies = new HashSet<Agency>();
            MaintenanceReports = new HashSet<MaintenanceReport>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            Requests = new HashSet<Request>();
            Skills = new HashSet<Skill>();
        }

        public Guid Id { get; set; }
        public Guid? AreaId { get; set; }
        public string? TechnicianName { get; set; }
        public Guid? AccountId { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public int? Gender { get; set; }
        public string? Address { get; set; }
        public bool? IsBusy { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? Code { get; set; }
        public int? Breach { get; set; }

        public virtual Account? Account { get; set; }
        public virtual Area? Area { get; set; }
        public virtual ICollection<Agency> Agencies { get; set; }
        public virtual ICollection<MaintenanceReport> MaintenanceReports { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public virtual ICollection<Request> Requests { get; set; }
        public virtual ICollection<Skill> Skills { get; set; }
    }
}
