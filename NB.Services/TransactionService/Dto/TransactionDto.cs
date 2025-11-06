using NB.Model.Entities;
using NB.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.Dto
{
    public class TransactionDto : Transaction
    {
        public string? WarehouseName { get; set; }
        public string? StatusName { get; set; }

        public string? FullName { get; set; }
    }
}
