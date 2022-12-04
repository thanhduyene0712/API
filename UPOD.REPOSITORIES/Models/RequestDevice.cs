using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class RequestDevice
    {
        public Guid Id { get; set; }
        public Guid? RequestId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Description { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public Guid? CreateBy { get; set; }
        public string? Solution { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Request? Request { get; set; }
    }
}
