using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionDetailService.Dto
{
    public class TransactionDetailSearch : SearchBase
    {
       public int? TransactionId { get; set; }
    }
}
