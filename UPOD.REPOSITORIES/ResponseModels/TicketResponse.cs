﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOD.REPOSITORIES.ResponseViewModel;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class TicketResponse

    {
        public Guid id { get; set; }
        public Guid? request_id { get; set; } = null!;
        public string? device_name { get; set; } = null!;
        public string? description { get; set; }
        public bool? is_delete { get; set; }
        public DateTime? create_date { get; set; }
        public DateTime? update_date { get; set; }
        public string? solution { get; set; }
        public string? create_by { get; set; } = null!;
        public List<string>? img { get; set; }
    }
}
