﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.REPOSITORIES.ResponseViewModel
{
    public class CreateByViewModel
    {
        public Guid? id { get; set; }
        public string? role { get; set; }
        public string? code { get; set; }
        public string? name { get; set; }
    }
}
