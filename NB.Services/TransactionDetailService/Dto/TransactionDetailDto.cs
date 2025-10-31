using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionDetailService.Dto
{
    public class TransactionDetailDto : TransactionDetail
    {
        public string? ProductName { get; set; }

        public string? ImageUrl { get; set; }
    }
}
