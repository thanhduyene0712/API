﻿using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class Contract
    {
        public Contract()
        {
            ContractServices = new HashSet<ContractService>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            Requests = new HashSet<Request>();
        }

        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public string? ContractName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public double? ContractPrice { get; set; }
        public DateTime? TerminalTime { get; set; }
        public string? Attachment { get; set; }
        public string? Description { get; set; }
        public string? Code { get; set; }
        public string? TerminalContent { get; set; }
        public string? RejectReason { get; set; }
        public bool? IsExpire { get; set; }
        public bool? IsAccepted { get; set; }
        public int? FrequencyMaintainTime { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual ICollection<ContractService> ContractServices { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public virtual ICollection<Request> Requests { get; set; }
    }
}
