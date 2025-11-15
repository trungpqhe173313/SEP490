using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListDetailService.Dto
{
    public class PriceListDetailSearch : SearchBase
    {
        public int? PriceListId { get; set; }
        public decimal? Price { get; set; }
        public decimal? RangeFrom { get; set; }
        public decimal? RangeTo { get; set; }

    }
}
