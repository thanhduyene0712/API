using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseViewModel
{
    public class AgencyViewResponse
    {
        public Guid? id { get; set; }
        public string? code { get; set; }
        public string? agency_name { get; set; }
        public string? manager_name { get; set; }
        public string? phone { get; set; }
        public string? address { get; set; }
    }
}
