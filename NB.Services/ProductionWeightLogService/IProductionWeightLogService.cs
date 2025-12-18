using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.ProductionWeightLogService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionWeightLogService
{
    public interface IProductionWeightLogService : IService<ProductionWeightLog>
    {
        /// <summary>
        /// Tổng hợp ProductionWeightLog theo ProductionId, nhóm theo từng sản phẩm
        /// </summary>
        /// <param name="productionId">ID của Production Order</param>
        /// <returns>Tổng số lượng bao và tổng khối lượng theo từng sản phẩm</returns>
        Task<ApiResponse<ProductionWeightLogSummaryResponseDto>> GetSummaryByProductionIdAsync(int productionId);
    }
}
