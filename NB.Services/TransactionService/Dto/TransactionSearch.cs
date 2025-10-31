﻿using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.Dto
{
    public class TransactionSearch : SearchBase
    {
        public int? Status { get; set; }

        public string? Type { get; set; }

        public DateTime? TransactionFromDate { get; set; }
        public DateTime? TransactionToDate { get; set; } 
    }
}
