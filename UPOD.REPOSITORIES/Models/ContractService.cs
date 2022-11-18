﻿using System;
using System.Collections.Generic;

namespace UPOD.REPOSITORIES.Models
{
    public partial class ContractService
    {
        public Guid Id { get; set; }
        public Guid? ContractId { get; set; }
        public Guid? ServiceId { get; set; }
        public bool? IsDelete { get; set; }

        public virtual Contract? Contract { get; set; }
        public virtual Service? Service { get; set; }
    }
}
