﻿using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class Ticket
    {
        public Guid Id { get; set; }
        public Guid? RequestId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Desciption { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? CreateBy { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Request? Request { get; set; }
    }
}
