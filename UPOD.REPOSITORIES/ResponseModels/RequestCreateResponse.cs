﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseModels
{
    public class RequestCreateResponse
    {
        public Guid id { get; set; }
        public string? code { get; set; }
        public string? request_name { get; set; }
        public string? customer_name { get; set; }
        public string? technician_name { get; set; }
        public string? request_description { get; set; }
        public string? phone { get; set; }
        public string? agency_name { get; set; }
        public string? service_name { get; set; }
    }
}
