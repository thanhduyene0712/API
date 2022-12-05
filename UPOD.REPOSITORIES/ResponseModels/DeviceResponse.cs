using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class DeviceResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? device_name { get; set; }
        public DateTime? guaranty_start_date { get; set; }
        public DateTime? guaranty_end_date { get; set; }
        public DateTime? setting_date { get; set; }
        public string? other { get; set; }
        public bool? is_delete { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public List<string>? img { get; set; } = null!;
        public CustomerViewResponse customer { get; set; } = null!;
        public ServiceNotInContractViewResponse service { get; set; } = null!;
        public TechnicianViewResponse technician { get; set; } = null!;
        public AgencyViewResponse agency { get; set; } = null!;
        public DeviceTypeViewResponse devicetype { get; set; } = null!;
    }
}
