using NB.Service.PriceListDetailService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListService.ViewModels
{
    public class PriceListOutputVM
    {
        public int PriceListId { get; set; }

        public string PriceListName { get; set; } = null!;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public List<PriceListDetailOutputVM?>? PriceListDetails { get; set; }
    }
}
