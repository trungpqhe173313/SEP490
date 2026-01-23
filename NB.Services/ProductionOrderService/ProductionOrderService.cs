using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinishproductService;
using NB.Service.FinishproductService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.IoTDeviceService;
using NB.Service.MaterialService;
using NB.Service.MaterialService.ViewModels;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.ProductionOrderService.ViewModels;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.UserService;
using NB.Service.WarehouseService;
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
        private readonly IWarehouseService _warehouseService;
        private readonly IIoTDeviceService _iotDeviceService;
        private readonly IInventoryService _inventoryService;
        private readonly IStockBatchService _stockBatchService;

        public ProductionOrderService(
            IRepository<ProductionOrder> repository,
            IMapper mapper,
            IMaterialService materialService,
            IFinishproductService finishproductService,
            IProductService productService,
            IUserService userService,
            IWarehouseService warehouseService,
            IIoTDeviceService iotDeviceService,
            IInventoryService inventoryService,
            IStockBatchService stockBatchService) : base(repository)
        {
            _mapper = mapper;
            _materialService = materialService;
            _finishproductService = finishproductService;
            _productService = productService;
            _userService = userService;
            _warehouseService = warehouseService;
            _iotDeviceService = iotDeviceService;
            _inventoryService = inventoryService;
            _stockBatchService = stockBatchService;
        }

        public async Task<PagedList<ProductionOrderDto>> GetData(ProductionOrderSearch search)
        {
            search ??= new ProductionOrderSearch();

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

            query = query.OrderByDescending(po => po.CreatedAt);
            return await PagedList<ProductionOrderDto>.CreateAsync(query, search);
        }

        public async Task<PagedList<ProductionOrderDto>> GetDataByResponsibleId(int responsibleId, ProductionOrderSearch search)
        {
            search ??= new ProductionOrderSearch();

            var query = from po in GetQueryable()
                        where po.ResponsibleId == responsibleId
                        select new ProductionOrderDto
                        {
                            Id = po.Id,
                            StartDate = po.StartDate,
                            EndDate = po.EndDate,
                            Status = po.Status,
                            Note = po.Note,
                            CreatedAt = po.CreatedAt
                        };

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

        public async Task<ApiResponse<FullProductionOrderVM>> GetDetailById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<FullProductionOrderVM>.Fail("Id không hợp lệ", 400);
                }

                // Lấy thông tin production order
                var detail = await GetByIdAsync(id);
                if (detail == null)
                {
                    return ApiResponse<FullProductionOrderVM>.Fail("Không tìm thấy đơn sản xuất", 404);
                }

                var productionOrder = new FullProductionOrderVM
                {
                    Id = detail.Id,
                    StartDate = detail.StartDate,
                    EndDate = detail.EndDate,
                    Status = detail.Status,
                    Note = detail.Note,
                    CreatedAt = detail.CreatedAt
                };

                // Gắn StatusName
                if (detail.Status.HasValue)
                {
                    ProductionOrderStatus status = (ProductionOrderStatus)detail.Status.Value;
                    productionOrder.StatusName = status.GetDescription();
                }

                // Lấy thông tin nhân viên phụ trách
                if (detail.ResponsibleId.HasValue)
                {
                    var responsibleEmployee = await _userService.GetByIdAsync(detail.ResponsibleId.Value);
                    productionOrder.ResponsibleEmployeeFullName = responsibleEmployee?.FullName;
                }

                // Lấy danh sách thành phẩm
                var finishProducts = await _finishproductService.GetQueryable()
                    .Where(f => f.ProductionId == id)
                    .ToListAsync();

                // Lấy danh sách nguyên liệu
                var materials = await _materialService.GetQueryable()
                    .Where(m => m.ProductionId == id)
                    .ToListAsync();

                // Lấy danh sách ProductId và WarehouseId để query một lần
                var productIds = finishProducts.Select(f => f.ProductId)
                    .Union(materials.Select(m => m.ProductId))
                    .Distinct()
                    .ToList();

                var warehouseIds = finishProducts.Select(f => f.WarehouseId)
                    .Union(materials.Select(m => m.WarehouseId))
                    .Distinct()
                    .ToList();

                // Lấy thông tin sản phẩm
                var products = await _productService.GetByIds(productIds);
                var productsDict = products.Where(p => p != null).ToDictionary(p => p!.ProductId, p => p!);

                // Lấy thông tin kho
                var warehouses = await _warehouseService.GetByListWarehouseId(warehouseIds);
                var warehousesDict = warehouses.Where(w => w != null).ToDictionary(w => w!.WarehouseId, w => w!);

                // Map thành phẩm
                var finishProductDetails = finishProducts.Select(fp =>
                {
                    var product = productsDict.ContainsKey(fp.ProductId) ? productsDict[fp.ProductId] : null;
                    var warehouse = warehousesDict.ContainsKey(fp.WarehouseId) ? warehousesDict[fp.WarehouseId] : null;
                    return new FinishProductDetailDto
                    {
                        Id = fp.Id,
                        ProductId = fp.ProductId,
                        ProductName = product?.ProductName ?? "N/A",
                        ProductCode = product?.ProductCode ?? "N/A",
                        WarehouseId = fp.WarehouseId,
                        WarehouseName = warehouse?.WarehouseName ?? "N/A",
                        Quantity = fp.Quantity,
                        WeightPerUnit = product?.WeightPerUnit ?? 0,
                        CreatedAt = fp.CreatedAt
                    };
                }).ToList();

                // Map nguyên liệu
                var materialDetails = materials.Select(m =>
                {
                    var product = productsDict.ContainsKey(m.ProductId) ? productsDict[m.ProductId] : null;
                    var warehouse = warehousesDict.ContainsKey(m.WarehouseId) ? warehousesDict[m.WarehouseId] : null;
                    return new MaterialDetailDto
                    {
                        Id = m.Id,
                        ProductId = m.ProductId,
                        ProductName = product?.ProductName ?? "N/A",
                        ProductCode = product?.ProductCode ?? "N/A",
                        WarehouseId = m.WarehouseId,
                        WarehouseName = warehouse?.WarehouseName ?? "N/A",
                        Quantity = m.Quantity,
                        WeightPerUnit = product?.WeightPerUnit ?? 0,
                        CreatedAt = m.CreatedAt,
                        LastUpdated = m.LastUpdated
                    };
                }).ToList();

                productionOrder.FinishProducts = finishProductDetails;
                productionOrder.Materials = materialDetails;

                return ApiResponse<FullProductionOrderVM>.Ok(productionOrder);
            }
            catch (Exception ex)
            {
                return ApiResponse<FullProductionOrderVM>.Fail($"Có lỗi xảy ra khi lấy chi tiết đơn sản xuất: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> ChangeToProcessingAsync(int id, ChangeToProcessingRequest request, int userId)
        {
            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.DeviceCode))
                {
                    return ApiResponse<object>.Fail("Mã thiết bị là bắt buộc", 400);
                }

                // Lấy đơn sản xuất
                var productionOrder = await GetQueryable()
                    .FirstOrDefaultAsync(po => po.Id == id);

                if (productionOrder == null)
                {
                    return ApiResponse<object>.Fail("Không tìm thấy đơn sản xuất", 404);
                }

                // Kiểm tra quyền của nhân viên
                if (productionOrder.ResponsibleId != userId)
                {
                    return ApiResponse<object>.Fail("Bạn không có quyền thay đổi trạng thái đơn sản xuất này", 403);
                }

                // Kiểm tra trạng thái hiện tại
                if (productionOrder.Status != (int)ProductionOrderStatus.Pending)
                {
                    return ApiResponse<object>.Fail("Chỉ có thể chuyển sang trạng thái đang xử lý khi đơn đang ở trạng thái chờ xử lý", 400);
                }

                // Kiểm tra xem đã có đơn nào đang Processing chưa
                var existingProcessingOrder = await GetQueryable()
                    .Where(po => po.Status == (int)ProductionOrderStatus.Processing && po.Id != id)
                    .FirstOrDefaultAsync();

                if (existingProcessingOrder != null)
                {
                    return ApiResponse<object>.Fail($"Đã có đơn sản xuất #{existingProcessingOrder.Id} đang ở trạng thái Processing. Vui lòng hoàn thành đơn đó trước.", 409);
                }

                // Lấy danh sách nguyên liệu
                var materials = await _materialService.GetQueryable()
                    .Where(m => m.ProductionId == id)
                    .ToListAsync();

                if (!materials.Any())
                {
                    return ApiResponse<object>.Fail("Đơn sản xuất không có nguyên liệu", 400);
                }

                var inventoryUpdates = new Dictionary<int, Inventory>();
                var stockBatchUpdates = new Dictionary<int, StockBatch>();

                // Xử lý từng nguyên liệu
                foreach (var material in materials)
                {
                    var productId = material.ProductId;
                    var quantity = material.Quantity;

                    // Kiểm tra tồn kho
                    var inventoryDto = await _inventoryService.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId);
                    if (inventoryDto == null)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return ApiResponse<object>.Fail($"Không tìm thấy sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguyên liệu.", 404);
                    }

                    if ((inventoryDto.Quantity ?? 0) < quantity)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return ApiResponse<object>.Fail($"Sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguyên liệu chỉ còn {inventoryDto.Quantity}, không đủ {quantity} yêu cầu.", 400);
                    }

                    // Lấy StockBatch theo FIFO
                    var listStockBatch = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (listStockBatch == null || !listStockBatch.Any())
                    {
                        return ApiResponse<object>.Fail($"Không tìm thấy lô hàng khả dụng cho sản phẩm {productId} trong kho nguyên liệu.", 404);
                    }

                    var batches = listStockBatch
                        .Where(sb => sb.ProductId == productId
                            && sb.WarehouseId == RawMaterialWarehouseId
                            && ((sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0))
                            && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today))
                        .OrderBy(sb => sb.ImportDate)
                        .ToList();

                    decimal remaining = quantity;
                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;
                        decimal available = ((batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0));
                        if (available <= 0) continue;

                        decimal take = Math.Min(available, remaining);

                        if (stockBatchUpdates.ContainsKey(batch.BatchId))
                        {
                            stockBatchUpdates[batch.BatchId].QuantityOut += take;
                        }
                        else
                        {
                            var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                            if (batchEntity != null)
                            {
                                batchEntity.QuantityOut += take;
                                batchEntity.LastUpdated = DateTime.Now;
                                stockBatchUpdates[batch.BatchId] = batchEntity;
                            }
                        }
                        remaining -= take;
                    }

                    if (remaining > 0)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return ApiResponse<object>.Fail($"Không đủ hàng trong các lô cho sản phẩm '{product?.ProductName ?? productId.ToString()}'", 400);
                    }

                    // Cập nhật Inventory
                    var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(RawMaterialWarehouseId, productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity -= quantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }
                }

                // Thực hiện update tất cả Inventory
                foreach (var inventory in inventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // Thực hiện update tất cả StockBatch
                foreach (var stockBatch in stockBatchUpdates.Values)
                {
                    await _stockBatchService.UpdateNoTracking(stockBatch);
                }

                // Kiểm tra thiết bị IoT
                var device = await _iotDeviceService.GetQueryable()
                    .FirstOrDefaultAsync(d => d.DeviceCode == request.DeviceCode);

                if (device == null)
                {
                    return ApiResponse<object>.Fail("Không tìm thấy thiết bị với mã đã cung cấp", 404);
                }

                if (device.CurrentProductionId.HasValue && device.CurrentProductionId != id)
                {
                    return ApiResponse<object>.Fail("Thiết bị đã được gán cho đơn sản xuất khác", 400);
                }

                // Cập nhật trạng thái đơn sản xuất
                productionOrder.Status = (int)ProductionOrderStatus.Processing;
                productionOrder.StartDate = DateTime.Now;

                // Cập nhật thiết bị IoT
                device.CurrentProductionId = id;

                await UpdateAsync(productionOrder);
                await _iotDeviceService.UpdateAsync(device);

                return ApiResponse<object>.Ok(new
                {
                    Id = productionOrder.Id,
                    Status = productionOrder.Status,
                    DeviceCode = device.DeviceCode,
                    DeviceName = device.DeviceName,
                    Message = "Đã chuyển đơn sản xuất sang trạng thái đang xử lý"
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"Có lỗi xảy ra khi thay đổi trạng thái đơn sản xuất: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> SubmitForApprovalAsync(int id, SubmitForApprovalRequest request, int userId)
        {
            try
            {
                // Validate request
                if (request == null || request.FinishProductQuantities == null || !request.FinishProductQuantities.Any())
                {
                    return ApiResponse<object>.Fail("Danh sách thành phẩm không được để trống", 400);
                }

                // Lấy đơn sản xuất
                var productionOrder = await GetQueryable()
                    .Include(po => po.Finishproducts)
                    .FirstOrDefaultAsync(po => po.Id == id);

                if (productionOrder == null)
                {
                    return ApiResponse<object>.Fail("Không tìm thấy đơn sản xuất", 404);
                }

                // Kiểm tra quyền của nhân viên
                if (productionOrder.ResponsibleId != userId)
                {
                    return ApiResponse<object>.Fail("Bạn không có quyền cập nhật đơn sản xuất này", 403);
                }

                // Kiểm tra trạng thái hiện tại - chỉ chuyển từ Processing sang WaitingApproval
                if (productionOrder.Status != (int)ProductionOrderStatus.Processing
                    && productionOrder.Status != (int)ProductionOrderStatus.Rejected)
                {
                    return ApiResponse<object>.Fail("Chỉ có thể gửi phê duyệt khi đơn đang ở trạng thái Đang xử lý hoặc Bị từ chối", 400);
                }

                // Cập nhật số lượng thành phẩm
                foreach (var finishProductQty in request.FinishProductQuantities)
                {
                    // Tìm finish product tương ứng
                    var finishProduct = productionOrder.Finishproducts
                        .FirstOrDefault(fp => fp.ProductId == finishProductQty.ProductId);

                    if (finishProduct == null)
                    {
                        return ApiResponse<object>.Fail($"Không tìm thấy sản phẩm {finishProductQty.ProductId} trong đơn sản xuất", 400);
                    }

                    // Cập nhật số lượng nếu được cung cấp
                    if (finishProductQty.Quantity.HasValue)
                    {
                        if (finishProductQty.Quantity.Value < 0)
                        {
                            return ApiResponse<object>.Fail("Số lượng không được âm", 400);
                        }
                        finishProduct.Quantity = finishProductQty.Quantity.Value;
                        // Cập nhật trực tiếp finish product
                        await _finishproductService.UpdateAsync(finishProduct);
                    }
                }

                // Cập nhật trạng thái đơn sản xuất sang WaitingApproval
                productionOrder.Status = (int)ProductionOrderStatus.WaitingApproval;

                // Cập nhật ghi chú nếu có
                if (!string.IsNullOrWhiteSpace(request.Note))
                {
                    productionOrder.Note = request.Note;
                }

                // Lưu thay đổi
                await UpdateAsync(productionOrder);

                return ApiResponse<object>.Ok(new
                {
                    Id = productionOrder.Id,
                    Status = productionOrder.Status,
                    StatusName = ProductionOrderStatus.WaitingApproval.GetDescription(),
                    FinishProducts = productionOrder.Finishproducts.Select(fp => new
                    {
                        fp.ProductId,
                        fp.Quantity
                    }),
                    Message = "Đã gửi đơn sản xuất để phê duyệt"
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"Có lỗi xảy ra khi gửi đơn sản xuất để phê duyệt: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> ChangeToRejectedAsync(int id, ChangeToRejectedRequest request)
        {
            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.Note))
                {
                    return ApiResponse<object>.Fail("Lý do từ chối là bắt buộc", 400);
                }

                // Lấy đơn sản xuất
                var productionOrder = await GetQueryable()
                    .FirstOrDefaultAsync(po => po.Id == id);

                if (productionOrder == null)
                {
                    return ApiResponse<object>.Fail("Không tìm thấy đơn sản xuất", 404);
                }

                // Kiểm tra trạng thái hiện tại - chỉ từ chối khi đang ở trạng thái WaitingApproval
                if (productionOrder.Status != (int)ProductionOrderStatus.WaitingApproval)
                {
                    return ApiResponse<object>.Fail("Chỉ có thể từ chối đơn sản xuất đang chờ phê duyệt", 400);
                }

                // Cập nhật trạng thái và ghi chú
                productionOrder.Status = (int)ProductionOrderStatus.Processing;
                productionOrder.Note = request.Note;

                await UpdateAsync(productionOrder);

                return ApiResponse<object>.Ok(new
                {
                    Id = productionOrder.Id,
                    Status = productionOrder.Status,
                    StatusName = ProductionOrderStatus.Rejected.GetDescription(),
                    Note = productionOrder.Note,
                    Message = "Đã từ chối đơn sản xuất. Nhân viên cần làm lại"
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"Có lỗi xảy ra khi từ chối đơn sản xuất: {ex.Message}", 500);
            }
        }
    }
}
