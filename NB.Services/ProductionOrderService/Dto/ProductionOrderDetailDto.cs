using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.Dto
{
    public class ProductionOrderDetailDto
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Status { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<FinishProductDetailDto> FinishProducts { get; set; } = new();
        public List<MaterialDetailDto> Materials { get; set; } = new();
    }
}

