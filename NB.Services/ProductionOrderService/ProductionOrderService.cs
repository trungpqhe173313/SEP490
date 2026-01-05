using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinishproductService;
using NB.Service.FinishproductService.ViewModels;
using NB.Service.MaterialService;
using NB.Service.MaterialService.ViewModels;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.ProductionOrderService.ViewModels;
using NB.Service.ProductService;
using NB.Service.UserService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService
{
    public class ProductionOrderService : Service<ProductionOrder>, IProductionOrderService
    {
        private const int RawMaterialWarehouseId = 2;
        private readonly IMapper _mapper;
        private readonly IMaterialService _materialService;
        private readonly IFinishproductService _finishproductService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;

        public ProductionOrderService(
            IRepository<ProductionOrder> repository,
            IMapper mapper,
            IMaterialService materialService,
            IFinishproductService finishproductService,
            IProductService productService,
            IUserService userService) : base(repository)
        {
            _mapper = mapper;
            _materialService = materialService;
            _finishproductService = finishproductService;
            _productService = productService;
            _userService = userService;
        }

        public async Task<PagedList<ProductionOrderDto>> GetData(ProductionOrderSearch search)
        {
            var query = from po in GetQueryable()
                        select new ProductionOrderDto
                        {
                            Id = po.Id,
                            StartDate = po.StartDate,
                            EndDate = po.EndDate,
                            Status = po.Status,
                            Note = po.Note,
                            CreatedAt = po.CreatedAt
                        };

            if (search != null)
            {
                if (search.Status.HasValue)
                {
                    query = query.Where(po => po.Status == search.Status.Value);
                }
                if (search.StartDateFrom.HasValue)
                {
                    query = query.Where(po => po.StartDate.HasValue && po.StartDate >= search.StartDateFrom.Value);
                }
                if (search.StartDateTo.HasValue)
                {
                    query = query.Where(po => po.StartDate.HasValue && po.StartDate <= search.StartDateTo.Value);
                }
            }

            query = query.OrderByDescending(po => po.CreatedAt);
            return await PagedList<ProductionOrderDto>.CreateAsync(query, search);
        }

        public async Task<ApiResponse<ProductionOrder>> CreateProductionOrderAsync(ProductionRequest request)
        {
            try
            {
                // Kiểm tra ResponsibleId có tồn tại và là nhân viên
                var responsibleUser = await _userService.GetByIdAsync(request.responsibleId);
                if (responsibleUser == null)
                {
                    return ApiResponse<ProductionOrder>.Fail("Nhân viên phụ trách không tồn tại", 404);
                }

                if (!responsibleUser.IsActive.HasValue || !responsibleUser.IsActive.Value)
                {
                    return ApiResponse<ProductionOrder>.Fail("Nhân viên phụ trách không còn hoạt động", 400);
                }

                // Kiểm tra user có role Employee không
                var userRoles = await _userService.GetQueryable()
                    .Where(u => u.UserId == request.responsibleId)
                    .SelectMany(u => u.UserRoles)
                    .Include(ur => ur.Role)
                    .ToListAsync();

                if (!userRoles.Any(ur => ur.Role.RoleName == "Employee"))
                {
                    return ApiResponse<ProductionOrder>.Fail("Người phụ trách phải có vai trò nhân viên (Employee)", 400);
                }

                // Kiểm tra sản phẩm nguyên liệu tồn tại
                var productMaterial = await _productService.GetByIdAsync(request.MaterialProductId);
                if (productMaterial == null)
                {
                    return ApiResponse<ProductionOrder>.Fail("Sản phẩm nguyên liệu không tồn tại", 404);
                }

                // Kiểm tra danh sách thành phẩm
                var listFinishProductId = request.ListFinishProduct.Select(fp => fp.ProductId).ToList();
                var listFinishProduct = await _productService.GetByIds(listFinishProductId);
                
                foreach (var finishProduct in request.ListFinishProduct)
                {
                    var productFinishCheck = listFinishProduct.FirstOrDefault(p => p?.ProductId == finishProduct.ProductId);
                    if (productFinishCheck == null)
                    {
                        return ApiResponse<ProductionOrder>.Fail($"Sản phẩm hoàn thiện với ID {finishProduct.ProductId} không tồn tại", 404);
                    }
                }

                // Tạo ProductionOrder
                var entityProductionOrderCreate = new ProductionOrderCreateVM
                {
                    ResponsibleId = request.responsibleId,
                    Note = request.Note
                };
                var entityProductionOrder = _mapper.Map<ProductionOrderCreateVM, ProductionOrder>(entityProductionOrderCreate);
                entityProductionOrder.Status = (int)ProductionOrderStatus.Pending;
                entityProductionOrder.CreatedAt = DateTime.Now;
                await CreateAsync(entityProductionOrder);

                // Kiểm tra ProductionOrder đã được tạo
                if (entityProductionOrder.Id <= 0)
                {
                    return ApiResponse<ProductionOrder>.Fail("Lỗi khi tạo đơn sản xuất: Không thể tạo đơn sản xuất", 500);
                }

                // Tạo Finishproducts
                foreach (var finishProduct in request.ListFinishProduct)
                {
                    var product = listFinishProduct.FirstOrDefault(p => p?.ProductId == finishProduct.ProductId);
                    var entityFinishProductProductionCreate = new FinishproductCreateVM
                    {
                        ProductionId = entityProductionOrder.Id,
                        ProductId = finishProduct.ProductId,
                        Quantity = finishProduct.Quantity,
                        WarehouseId = 1, // Mặc định kho tổng là 1
                        TotalWeight = (product?.WeightPerUnit * finishProduct.Quantity) ?? 0
                    };
                    var entityFinishProductProduction = _mapper.Map<FinishproductCreateVM, Finishproduct>(entityFinishProductProductionCreate);
                    entityFinishProductProduction.CreatedAt = DateTime.Now;
                    await _finishproductService.CreateAsync(entityFinishProductProduction);
                }

                // Tạo Material
                var entityMaterialUsage = new MaterialCreateVM
                {
                    ProductionId = entityProductionOrder.Id,
                    ProductId = request.MaterialProductId,
                    Quantity = request.MaterialQuantity,
                    WarehouseId = RawMaterialWarehouseId, // Mặc định kho nguyên liệu là 2
                    TotalWeight = (productMaterial.WeightPerUnit * request.MaterialQuantity) ?? 0
                };
                var entityMaterial = _mapper.Map<MaterialCreateVM, Material>(entityMaterialUsage);
                entityMaterial.CreatedAt = DateTime.Now;
                entityMaterial.LastUpdated = DateTime.Now;
                await _materialService.CreateAsync(entityMaterial);

                return ApiResponse<ProductionOrder>.Ok(entityProductionOrder);
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductionOrder>.Fail($"Có lỗi xảy ra khi tạo đơn sản xuất: {ex.Message}", 500);
            }
        }
    }
}
