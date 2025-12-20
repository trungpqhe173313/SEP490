using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
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
    public class ProductionWeightLogService : Service<ProductionWeightLog>, IProductionWeightLogService
    {
        private readonly IRepository<ProductionOrder> _productionOrderRepository;

        public ProductionWeightLogService(
            IRepository<ProductionWeightLog> repository,
            IRepository<ProductionOrder> productionOrderRepository) : base(repository)
        {
            _productionOrderRepository = productionOrderRepository;
        }

        public async Task<ApiResponse<ProductionWeightLogSummaryResponseDto>> GetSummaryByProductionIdAsync(int productionId)
        {
            // Validation: productionId phải > 0
            if (productionId <= 0)
            {
                return ApiResponse<ProductionWeightLogSummaryResponseDto>.Fail("ProductionId must be greater than 0", 400);
            }

            // Validation: kiểm tra ProductionOrder có tồn tại không
            var productionExists = await _productionOrderRepository.GetQueryable()
                .AnyAsync(p => p.Id == productionId);

            if (!productionExists)
            {
                return ApiResponse<ProductionWeightLogSummaryResponseDto>.Fail("Production order not found", 404);
            }

            var productSummaries = await GetQueryable()
                .Where(log => log.ProductionId == productionId)
                .GroupBy(log => new { log.ProductId, log.Product.ProductName })
                .Select(g => new ProductWeightSummaryDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalBags = g.Count(),
                    TotalWeight = g.Sum(log => log.ActualWeight)
                })
                .ToListAsync();

            var response = new ProductionWeightLogSummaryResponseDto
            {
                ProductionId = productionId,
                Products = productSummaries
            };

            return ApiResponse<ProductionWeightLogSummaryResponseDto>.Ok(response);
        }
    }
}
