﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponeModels
{
    public class RoleResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? role_name { get; set; }
        //public bool? is_delete { get; set; }
        //public DateTime? create_date { get; set; }
        //public DateTime? update_date { get; set; }
    }
}