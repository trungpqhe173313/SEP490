using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListService.Dto
{
    public class PriceListSearch : SearchBase
    {
        public int? PriceListId { get; set; }

        public string? PriceListName { get; set; }

        public DateTime? StartDate { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
