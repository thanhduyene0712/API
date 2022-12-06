using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class SearchRequest
    {
        public string? search { get; set; }
    }
    public class FilterRequest
    {
        public string? task { get; set; }
    }
}
