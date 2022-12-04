using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class Device
    {
        public Device()
        {
            RequestDevices = new HashSet<RequestDevice>();
        }

        public Guid Id { get; set; }
        public Guid? AgencyId { get; set; }
        public Guid? DeviceTypeId { get; set; }
        public string? DeviceName { get; set; }
        public string? Code { get; set; }
        public DateTime? GuarantyStartDate { get; set; }
        public DateTime? GuarantyEndDate { get; set; }
        public DateTime? SettingDate { get; set; }
        public string? Other { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public Guid? CreateBy { get; set; }

        public virtual Agency? Agency { get; set; }
        public virtual DeviceType? DeviceType { get; set; }
        public virtual ICollection<RequestDevice> RequestDevices { get; set; }
    }
}
