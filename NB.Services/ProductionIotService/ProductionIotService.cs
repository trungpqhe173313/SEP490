using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.ProductionIotService.Dto;
using NB.Services.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NB.Services.ProductionIotService
{
    public class ProductionIotService : IProductionIotService
    {
        private readonly IRepository<IoTdevice> _iotDeviceRepository;
        private readonly IRepository<ProductionOrder> _productionOrderRepository;
        private readonly IRepository<Finishproduct> _finishProductRepository;
        private readonly IRepository<ProductionWeightLog> _productionWeightLogRepository;
        private readonly IRepository<Product> _productRepository;

        public ProductionIotService(
            IRepository<IoTdevice> iotDeviceRepository,
            IRepository<ProductionOrder> productionOrderRepository,
            IRepository<Finishproduct> finishProductRepository,
            IRepository<ProductionWeightLog> productionWeightLogRepository,
            IRepository<Product> productRepository)
        {
            _iotDeviceRepository = iotDeviceRepository;
            _productionOrderRepository = productionOrderRepository;
            _finishProductRepository = finishProductRepository;
            _productionWeightLogRepository = productionWeightLogRepository;
            _productRepository = productRepository;
        }

        public async Task<ApiResponse<CurrentProductionResponseDto>> GetCurrentProductionAsync(string deviceCode)
        {
            // Tìm IoT device theo deviceCode
            var iotDevice = await _iotDeviceRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);

            if (iotDevice == null)
            {
                return ApiResponse<CurrentProductionResponseDto>.Fail("Device not found", 404);
            }

            // Kiểm tra xem device có gán CurrentProductionId không
            if (!iotDevice.CurrentProductionId.HasValue)
            {
                return ApiResponse<CurrentProductionResponseDto>.Fail("No production order assigned to this device", 404);
            }

            // Lấy thông tin ProductionOrder
            var productionOrder = await _productionOrderRepository.GetQueryable()
                .FirstOrDefaultAsync(po => po.Id == iotDevice.CurrentProductionId.Value);

            if (productionOrder == null)
            {
                return ApiResponse<CurrentProductionResponseDto>.Fail("Production order not found", 404);
            }

            // Lấy danh sách Finishproducts
            var finishProducts = await _finishProductRepository.GetQueryable()
                .Include(fp => fp.Product)
                .Where(fp => fp.ProductionId == productionOrder.Id)
                .Select(fp => new ProductItemDto
                {
                    ProductId = fp.ProductId,
                    ProductName = TextHelper.RemoveDiacritics(fp.Product.ProductName),
                    TargetWeight = fp.Product.WeightPerUnit ?? 0
                })
                .ToListAsync();

            // Map status từ enum sang string
            string status = productionOrder.Status.HasValue 
                ? ((ProductionOrderStatus)productionOrder.Status.Value).ToString().ToUpper()
                : "UNKNOWN";

            var response = new CurrentProductionResponseDto
            {
                Status = status,
                ProductionId = productionOrder.Id,
                Products = finishProducts
            };

            return ApiResponse<CurrentProductionResponseDto>.Ok(response);
        }

        public async Task<ApiResponse<PackageSubmitResponseDto>> SubmitPackageAsync(PackageSubmitRequestDto request)
        {
            // 1. Validation: deviceCode tồn tại trong bảng IoTdevice
            var iotDevice = await _iotDeviceRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.DeviceCode == request.DeviceCode);

            if (iotDevice == null)
            {
                return ApiResponse<PackageSubmitResponseDto>.Fail("Device not found", 400);
            }

            // 2. Validation: productionId tồn tại và có Status = PROCESSING
            var production = await _productionOrderRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == request.ProductionId);

            if (production == null)
            {
                return ApiResponse<PackageSubmitResponseDto>.Fail("Production not found", 400);
            }

            // 2.1. Validation: Kiểm tra production còn đang active (Processing)
            if (!production.Status.HasValue || production.Status.Value != (int)ProductionOrderStatus.Processing)
            {
                return ApiResponse<PackageSubmitResponseDto>.Fail("Production is not active. Please check production status", 409);
            }

            // 2.2. Validation: Kiểm tra device có được gán với production này không
            if (!iotDevice.CurrentProductionId.HasValue || iotDevice.CurrentProductionId.Value != request.ProductionId)
            {
                return ApiResponse<PackageSubmitResponseDto>.Fail(
                    $"Device '{request.DeviceCode}' is not assigned to production #{request.ProductionId}", 403);
            }

            // 3. Validation: productId thuộc production
            var finishProduct = await _finishProductRepository.GetQueryable()
                .Include(fp => fp.Product)
                .FirstOrDefaultAsync(fp => fp.ProductionId == request.ProductionId && fp.ProductId == request.ProductId);

            if (finishProduct == null)
            {
                return ApiResponse<PackageSubmitResponseDto>.Fail("Product does not belong to this production", 400);
            }

            // 4. Validation: weight > 0 (đã validate ở DTO với [Range])
            if (request.Weight <= 0)
            {
                return ApiResponse<PackageSubmitResponseDto>.Fail("Weight must be greater than 0", 400);
            }

            // 5. Lấy TargetWeight từ Product
            decimal targetWeight = finishProduct.Product.WeightPerUnit ?? 0;

            // 6. Tính BagIndex: số bao đã đóng của product trong production + 1
            int currentBagCount = await _productionWeightLogRepository.GetQueryable()
                .CountAsync(log => log.ProductionId == request.ProductionId && log.ProductId == request.ProductId);

            int bagIndex = currentBagCount + 1;

            // 7. Tạo bản ghi ProductionWeightLog
            var weightLog = new ProductionWeightLog
            {
                ProductionId = request.ProductionId,
                ProductId = request.ProductId,
                DeviceCode = request.DeviceCode,
                ActualWeight = request.Weight,
                TargetWeight = targetWeight,
                BagIndex = bagIndex,
                CreatedAt = DateTime.Now,
                Note = null
            };

            // 8. Lưu vào database
            _productionWeightLogRepository.Add(weightLog);
            await _productionWeightLogRepository.SaveAsync();

            // 9. Trả về response
            var responseData = new PackageSubmitResponseDto
            {
                ProductionId = request.ProductionId,
                ProductId = request.ProductId,
                BagIndex = bagIndex,
                ActualWeight = request.Weight,
                TargetWeight = targetWeight
            };

            var response = ApiResponse<PackageSubmitResponseDto>.Ok(responseData);
            response.StatusCode = 201;
            return response;
        }
    }
}
