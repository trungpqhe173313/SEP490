using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionWeightLogService.Dto
{
    /// <summary>
    /// DTO cho tổng hợp theo từng sản phẩm
    /// </summary>
    public class ProductWeightSummaryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalBags { get; set; }
        public decimal TotalWeight { get; set; }
    }

    /// <summary>
    /// DTO cho response tổng hợp ProductionWeightLog
    /// </summary>
    public class ProductionWeightLogSummaryResponseDto
    {
        public int ProductionId { get; set; }
        public List<ProductWeightSummaryDto> Products { get; set; } = new List<ProductWeightSummaryDto>();
    }
}
