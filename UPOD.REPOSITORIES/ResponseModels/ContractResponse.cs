using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class ContractResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? contract_name { get; set; }
        public double? contract_price { get; set; }
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public bool? is_delete { get; set; }
        public bool? is_expire { get; set; }
        public bool? is_accepted { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public string? attachment { get; set; }
        public string? description { get; set; }
        public string? reject_reason { get; set; }
        public string? terminal_content { get; set; }
        public DateTime? terminal_time { get; set; }
        public int? frequency_maintain_time { get; set; }
        public List<string>? img { get; set; }
        public CustomerViewResponse customer { get; set; } = null!;
        public List<ServiceViewResponse> service { get; set; } = null!;
    }
}
