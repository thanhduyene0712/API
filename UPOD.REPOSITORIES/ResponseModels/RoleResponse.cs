﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class RoleResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? role_name { get; set; }
    }
}
