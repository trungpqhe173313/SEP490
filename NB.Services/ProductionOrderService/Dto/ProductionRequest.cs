using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.Dto
{
    public class ProductionRequest
    {
        public List<ProductProductionDto> ListFinishProduct { get; set; } = new();
        // ID của nguyên liệu
        public int MaterialProductId { get; set; }
        // Số lượng nguyên liệu sử dụng
        public int MaterialQuantity { get; set; }

        public string? Note { get; set; }
    }
}
