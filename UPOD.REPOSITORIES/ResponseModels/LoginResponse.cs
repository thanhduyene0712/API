﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class LoginResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public Guid account_id { get; set; }
        public Guid? role_id { get; set; }
        public string? role_name { get; set; }
        public string? username { get; set; }
        public string token { get; set; }
    }
}
