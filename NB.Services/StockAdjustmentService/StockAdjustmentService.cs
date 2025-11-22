using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.StockAdjustmentService.ViewModels;

namespace NB.Service.StockAdjustmentService
{
    public class StockAdjustmentService : Service<StockAdjustment>, IStockAdjustmentService
    {
        private readonly IRepository<StockAdjustment> _stockAdjustmentRepository;
        private readonly IRepository<StockAdjustmentDetail> _stockAdjustmentDetailRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<Product> _productRepository;

        public StockAdjustmentService(
            IRepository<StockAdjustment> stockAdjustmentRepository,
            IRepository<StockAdjustmentDetail> stockAdjustmentDetailRepository,
            IRepository<Warehouse> warehouseRepository,
            IRepository<Product> productRepository) : base(stockAdjustmentRepository)
        {
            _stockAdjustmentRepository = stockAdjustmentRepository;
            _stockAdjustmentDetailRepository = stockAdjustmentDetailRepository;
            _warehouseRepository = warehouseRepository;
            _productRepository = productRepository;
        }

        public async Task<StockAdjustmentDraftResponseVM> CreateDraftAsync(StockAdjustmentDraftCreateVM model)
        {
            // Validate warehouse exists
            var warehouse = await _warehouseRepository.GetByIdAsync(model.WarehouseId);
            if (warehouse == null)
            {
                throw new Exception($"Không tìm thấy kho với Id {model.WarehouseId}");
            }

            // Validate all products exist
            var productIds = model.Details.Select(d => d.ProductId).Distinct().ToList();
            var products = await _productRepository.GetQueryable()
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                var missingIds = productIds.Except(products.Select(p => p.ProductId)).ToList();
                throw new Exception($"Không tìm thấy sản phẩm với Id: {string.Join(", ", missingIds)}");
            }

            // Create stock adjustment entity
            var stockAdjustment = new StockAdjustment
            {
                WarehouseId = model.WarehouseId,
                Status = (int)StockAdjustmentStatus.Draft,
                CreatedAt = DateTime.Now
            };

            _stockAdjustmentRepository.Add(stockAdjustment);
            await _stockAdjustmentRepository.SaveAsync();

            // Create stock adjustment details
            var details = model.Details.Select(d => new StockAdjustmentDetail
            {
                AdjustmentId = stockAdjustment.AdjustmentId,
                ProductId = d.ProductId,
                ActualQuantity = d.ActualQuantity,
                SystemQuantity = d.SystemQuantity,
                Note = d.Note,
                CreatedAt = DateTime.Now
            }).ToList();

            foreach (var detail in details)
            {
                _stockAdjustmentDetailRepository.Add(detail);
            }
            await _stockAdjustmentDetailRepository.SaveAsync();

            // Build response
            var response = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = stockAdjustment.AdjustmentId,
                WarehouseId = stockAdjustment.WarehouseId,
                WarehouseName = warehouse.WarehouseName,
                Status = stockAdjustment.Status,
                StatusDescription = ((StockAdjustmentStatus)stockAdjustment.Status).GetDescription(),
                CreatedAt = stockAdjustment.CreatedAt,
                Details = details.Select(d => new StockAdjustmentDetailResponseVM
                {
                    DetailId = d.DetailId,
                    ProductId = d.ProductId,
                    ProductName = products.First(p => p.ProductId == d.ProductId).ProductName,
                    ActualQuantity = d.ActualQuantity,
                    SystemQuantity = d.SystemQuantity,
                    Note = d.Note,
                    CreatedAt = d.CreatedAt
                }).ToList()
            };

            return response;
        }
    }
}

