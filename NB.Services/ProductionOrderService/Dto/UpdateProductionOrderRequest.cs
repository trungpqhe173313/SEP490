using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService.Dto
{
    public class UpdateProductionOrderRequest
    {
        /// <summary>
        /// Danh sách thành phẩm. Nếu null hoặc rỗng thì giữ nguyên danh sách cũ
        /// </summary>
        public List<ProductProductionDto>? ListFinishProduct { get; set; }
        
        /// <summary>
        /// ID của nguyên liệu. Nếu null hoặc <= 0 thì giữ nguyên nguyên liệu cũ
        /// </summary>
        public int? MaterialProductId { get; set; }
        
        /// <summary>
        /// Số lượng nguyên liệu sử dụng. Nếu null hoặc <= 0 thì giữ nguyên số lượng cũ
        /// </summary>
        public int? MaterialQuantity { get; set; }

        /// <summary>
        /// Ghi chú. Nếu null thì giữ nguyên ghi chú cũ
        /// </summary>
        public string? Note { get; set; }
    }
}

