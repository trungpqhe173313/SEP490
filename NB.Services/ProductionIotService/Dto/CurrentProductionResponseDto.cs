using System;
using System.Collections.Generic;

namespace NB.Service.ProductionIotService.Dto
{
    public class CurrentProductionResponseDto
    {
        public string Status { get; set; }
        public int ProductionId { get; set; }
        public List<ProductItemDto> Products { get; set; } = new List<ProductItemDto>();
    }

    public class ProductItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TargetWeight { get; set; }
    }
}
