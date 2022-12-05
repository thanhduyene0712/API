using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class TechnicianResponse
    {
        public Guid? id { get; set; }
        public string? code { get; set; }
        public AreaViewResponse area { get; set; } = null!;
        public string? technician_name { get; set; }
        public AccountViewResponse account { get; set; } = null!;
        public string? telephone { get; set; }
        public string? email { get; set; }
        public int? gender { get; set; }
        public string? address { get; set; }
        public bool? is_busy { get; set; }
        public bool? is_delete { get; set; }
        public int? breach { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public List<ServiceViewResponse> service { get; set; } = null!;
    }
}
