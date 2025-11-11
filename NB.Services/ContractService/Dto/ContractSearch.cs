using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService.Dto
{
    public class ContractSearch : SearchBase
    {
        public int? CustomerId { get; set; }

        public int? SupplierId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
