﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.RequestModels
{
    public class ServiceRequest
    {
        public Guid DepId { get; set; }
        public string ServiceName { get; set; }
        public string? Desciption { get; set; }
    }
}
