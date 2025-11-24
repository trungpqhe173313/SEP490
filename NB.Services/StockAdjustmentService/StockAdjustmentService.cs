using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.StockAdjustmentService.Dto;
using NB.Service.StockAdjustmentService.ViewModels;
using NB.Service.StockBatchService;

namespace NB.Service.StockAdjustmentService
{
    public class StockAdjustmentService : Service<StockAdjustment>, IStockAdjustmentService
    {
        private readonly IRepository<StockAdjustment> _stockAdjustmentRepository;
        private readonly IRepository<StockAdjustmentDetail> _stockAdjustmentDetailRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Inventory> _inventoryRepository;
        private readonly IRepository<StockBatch> _stockBatchRepository;
        private readonly IStockBatchService _stockBatchService;

        public StockAdjustmentService(
            IRepository<StockAdjustment> stockAdjustmentRepository,
            IRepository<StockAdjustmentDetail> stockAdjustmentDetailRepository,
            IRepository<Warehouse> warehouseRepository,
            IRepository<Product> productRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<StockBatch> stockBatchRepository,
            IStockBatchService stockBatchService) : base(stockAdjustmentRepository)
        {
            _stockAdjustmentRepository = stockAdjustmentRepository;
            _stockAdjustmentDetailRepository = stockAdjustmentDetailRepository;
            _warehouseRepository = warehouseRepository;
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
            _stockBatchRepository = stockBatchRepository;
            _stockBatchService = stockBatchService;
        }

        public async Task<PagedList<StockAdjustmentListDto>> GetPagedListAsync(StockAdjustmentSearch search)
        {
            var query = GetQueryable()
                .Include(sa => sa.Warehouse)
                .Include(sa => sa.StockAdjustmentDetails)
                .AsQueryable();

            // Filters
            if (search != null)
            {
                if (search.WarehouseId.HasValue)
                {
                    query = query.Where(sa => sa.WarehouseId == search.WarehouseId.Value);
                }

                if (search.Status.HasValue)
                {
                    query = query.Where(sa => sa.Status == search.Status.Value);
                }

                if (search.FromDate.HasValue)
                {
                    query = query.Where(sa => sa.CreatedAt >= search.FromDate.Value);
                }

                if (search.ToDate.HasValue)
                {
                    var toDateEnd = search.ToDate.Value.Date.AddDays(1);
                    query = query.Where(sa => sa.CreatedAt < toDateEnd);
                }
            }

            // Projection
            var result = query.Select(sa => new StockAdjustmentListDto
            {
                AdjustmentId = sa.AdjustmentId,
                WarehouseId = sa.WarehouseId,
                WarehouseName = sa.Warehouse.WarehouseName,
                Status = sa.Status,
                StatusDescription = ((StockAdjustmentStatus)sa.Status).GetDescription(),
                CreatedAt = sa.CreatedAt,
                ResolvedAt = sa.ResolvedAt,
                TotalProducts = sa.StockAdjustmentDetails.Count
            }).OrderByDescending(sa => sa.CreatedAt);

            return await PagedList<StockAdjustmentListDto>.CreateAsync(result, search);
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
            // CHÚ Ý: KHÔNG lưu SystemQuantity vào DB khi tạo Draft
            // SystemQuantity sẽ được lấy REALTIME khi GET draft
            // CHỈ lưu SystemQuantity khi RESOLVE để audit trail
            var details = model.Details.Select(d => new StockAdjustmentDetail
            {
                AdjustmentId = stockAdjustment.AdjustmentId,
                ProductId = d.ProductId,
                ActualQuantity = d.ActualQuantity,
                SystemQuantity = 0, // Chưa lưu, sẽ lưu khi resolve
                Note = d.Note,
                CreatedAt = DateTime.Now
            }).ToList();

            foreach (var detail in details)
            {
                _stockAdjustmentDetailRepository.Add(detail);
            }
            await _stockAdjustmentDetailRepository.SaveAsync();

            // Lấy SystemQuantity từ Inventory REALTIME
            var inventories = await _inventoryRepository.GetQueryable()
                .Where(i => i.WarehouseId == model.WarehouseId && productIds.Contains(i.ProductId))
                .ToListAsync();

            // Build response với SystemQuantity realtime
            var response = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = stockAdjustment.AdjustmentId,
                WarehouseId = stockAdjustment.WarehouseId,
                WarehouseName = warehouse.WarehouseName,
                Status = stockAdjustment.Status,
                StatusDescription = ((StockAdjustmentStatus)stockAdjustment.Status).GetDescription(),
                CreatedAt = stockAdjustment.CreatedAt,
                Details = details.Select(d => 
                {
                    var product = products.First(p => p.ProductId == d.ProductId);
                    var inventory = inventories.FirstOrDefault(i => i.ProductId == d.ProductId);
                    var systemQty = inventory?.Quantity ?? 0;
                    
                    return new StockAdjustmentDetailResponseVM
                    {
                        DetailId = d.DetailId,
                        ProductId = d.ProductId,
                        ProductName = product.ProductName,
                        ActualQuantity = d.ActualQuantity,
                        SystemQuantity = systemQty,
                        Difference = d.ActualQuantity - systemQty,
                        Note = d.Note,
                        CreatedAt = d.CreatedAt
                    };
                }).ToList()
            };

            return response;
        }

        public async Task<StockAdjustmentDraftResponseVM> GetDraftByIdAsync(int id)
        {
            // Lấy stock adjustment
            var stockAdjustment = await _stockAdjustmentRepository.GetQueryable()
                .Include(sa => sa.Warehouse)
                .Include(sa => sa.StockAdjustmentDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(sa => sa.AdjustmentId == id);

            if (stockAdjustment == null)
            {
                throw new Exception($"Không tìm thấy phiếu kiểm kho với Id {id}");
            }

            var productIds = stockAdjustment.StockAdjustmentDetails.Select(d => d.ProductId).ToList();
            var isDraft = stockAdjustment.Status == (int)StockAdjustmentStatus.Draft;

            // Nếu là Draft → Lấy SystemQuantity REALTIME
            // Nếu đã Resolved → Lấy SystemQuantity từ DB (đã lưu khi resolve)
            Dictionary<int, decimal> systemQuantities = new Dictionary<int, decimal>();

            if (isDraft)
            {
                // Lấy SystemQuantity REALTIME từ Inventory
                var inventories = await _inventoryRepository.GetQueryable()
                    .Where(i => i.WarehouseId == stockAdjustment.WarehouseId && productIds.Contains(i.ProductId))
                    .ToListAsync();

                foreach (var inv in inventories)
                {
                    systemQuantities[inv.ProductId] = inv.Quantity ?? 0;
                }
            }
            else
            {
                // Lấy SystemQuantity từ DB (đã lưu khi resolve)
                foreach (var detail in stockAdjustment.StockAdjustmentDetails)
                {
                    systemQuantities[detail.ProductId] = detail.SystemQuantity;
                }
            }

            // Build response
            var response = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = stockAdjustment.AdjustmentId,
                WarehouseId = stockAdjustment.WarehouseId,
                WarehouseName = stockAdjustment.Warehouse.WarehouseName,
                Status = stockAdjustment.Status,
                StatusDescription = ((StockAdjustmentStatus)stockAdjustment.Status).GetDescription(),
                CreatedAt = stockAdjustment.CreatedAt,
                Details = stockAdjustment.StockAdjustmentDetails.Select(d =>
                {
                    var systemQty = systemQuantities.ContainsKey(d.ProductId) 
                        ? systemQuantities[d.ProductId] 
                        : 0;

                    return new StockAdjustmentDetailResponseVM
                    {
                        DetailId = d.DetailId,
                        ProductId = d.ProductId,
                        ProductCode = d.Product.ProductCode,
                        ProductName = d.Product.ProductName,
                        ActualQuantity = d.ActualQuantity,
                        SystemQuantity = systemQty, // REALTIME nếu Draft, từ DB nếu Resolved
                        Difference = d.ActualQuantity - systemQty,
                        Note = d.Note,
                        CreatedAt = d.CreatedAt
                    };
                }).ToList()
            };

            return response;
        }

        public async Task<StockAdjustmentDraftResponseVM> UpdateDraftAsync(int id, StockAdjustmentDraftUpdateVM model)
        {
            // Step 1: Validate draft tồn tại và status = Draft
            var stockAdjustment = await _stockAdjustmentRepository.GetQueryable()
                .Include(sa => sa.StockAdjustmentDetails)
                .FirstOrDefaultAsync(sa => sa.AdjustmentId == id);

            if (stockAdjustment == null)
            {
                throw new Exception($"Không tìm thấy phiếu kiểm kho với Id {id}");
            }

            if (stockAdjustment.Status != (int)StockAdjustmentStatus.Draft)
            {
                throw new Exception($"Không thể chỉnh sửa phiếu kiểm kho đã {((StockAdjustmentStatus)stockAdjustment.Status).GetDescription()}");
            }

            // Step 2: Validate products
            var productIds = model.Details.Select(d => d.ProductId).Distinct().ToList();
            var products = await _productRepository.GetQueryable()
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                var missingIds = productIds.Except(products.Select(p => p.ProductId)).ToList();
                throw new Exception($"Không tìm thấy sản phẩm với Id: {string.Join(", ", missingIds)}");
            }

            // Step 3: XÓA HẾT details cũ
            var oldDetails = stockAdjustment.StockAdjustmentDetails.ToList();
            foreach (var detail in oldDetails)
            {
                _stockAdjustmentDetailRepository.Delete(detail);
            }

            // Step 4: THÊM MỚI toàn bộ details từ request
            var newDetails = model.Details.Select(d => new StockAdjustmentDetail
            {
                AdjustmentId = id,
                ProductId = d.ProductId,
                ActualQuantity = d.ActualQuantity,
                SystemQuantity = 0, // Chưa lưu, sẽ lưu khi resolve
                Note = d.Note,
                CreatedAt = DateTime.Now
            }).ToList();

            foreach (var detail in newDetails)
            {
                _stockAdjustmentDetailRepository.Add(detail);
            }

            await _stockAdjustmentDetailRepository.SaveAsync();

            // Return response với dữ liệu mới
            return await GetDraftByIdAsync(id);
        }

        public async Task<StockAdjustmentDraftResponseVM> ResolveAsync(int id)
        {
            // Step 1: Lấy Draft và validate
            var stockAdjustment = await _stockAdjustmentRepository.GetQueryable()
                .Include(sa => sa.Warehouse)
                .Include(sa => sa.StockAdjustmentDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(sa => sa.AdjustmentId == id);

            if (stockAdjustment == null)
            {
                throw new Exception($"Không tìm thấy phiếu kiểm kho với Id {id}");
            }

            if (stockAdjustment.Status != (int)StockAdjustmentStatus.Draft)
            {
                throw new Exception($"Phiếu kiểm kho này đã được xử lý (Status: {((StockAdjustmentStatus)stockAdjustment.Status).GetDescription()})");
            }

            var warehouseId = stockAdjustment.WarehouseId;
            var details = stockAdjustment.StockAdjustmentDetails.ToList();

            // Step 2: Lấy SystemQuantity REALTIME từ Inventory
            var productIds = details.Select(d => d.ProductId).ToList();
            var inventories = await _inventoryRepository.GetQueryable()
                .Where(i => i.WarehouseId == warehouseId && productIds.Contains(i.ProductId))
                .ToListAsync();

            // Dictionary để track các update
            var inventoryUpdates = new Dictionary<int, Inventory>();
            var detailUpdates = new List<StockAdjustmentDetail>();

            foreach (var detail in details)
            {
                var inventory = inventories.FirstOrDefault(i => i.ProductId == detail.ProductId);
                var systemQty = inventory?.Quantity ?? 0;
                var actualQty = detail.ActualQuantity;
                var diff = actualQty - systemQty;

                // LƯU SystemQuantity vào DB để audit trail
                detail.SystemQuantity = systemQty;
                detail.UpdatedAt = DateTime.Now;
                detailUpdates.Add(detail);

                // Step 3: So sánh và xử lý
                if (diff == 0)
                {
                    continue;
                }
                else if (diff > 0)
                {
                    // CASE A: Actual > System → TỒN THIẾU → NHẬP (INCREASE)
                    // VD: Actual 100, System 80 → thiếu 20 → phải nhập 20
                    
                    if (inventory == null)
                    {
                        // Tạo mới inventory nếu chưa có
                        inventory = new Inventory
                        {
                            WarehouseId = warehouseId,
                            ProductId = detail.ProductId,
                            Quantity = diff,
                            LastUpdated = DateTime.Now
                        };
                        _inventoryRepository.Add(inventory);
                    }
                    else
                    {
                        inventory.Quantity += diff;
                        inventory.LastUpdated = DateTime.Now;
                        inventoryUpdates[detail.ProductId] = inventory;
                    }

                    // Tạo StockBatch mới cho số lượng nhập
                    var newBatch = new StockBatch
                    {
                        WarehouseId = warehouseId,
                        ProductId = detail.ProductId,
                        BatchCode = $"ADJ-{id}-{detail.ProductId}-{DateTime.Now:yyyyMMddHHmmss}",
                        ImportDate = DateTime.Now,
                        ExpireDate = DateTime.Now.AddMonths(3), 
                        QuantityIn = diff,
                        QuantityOut = 0,
                        UnitCost = 0,
                        Status = 1,
                        IsActive = true,
                        Note = $"Kiểm kho điều chỉnh - Nhập thêm {diff}",
                        LastUpdated = DateTime.Now
                    };
                    _stockBatchRepository.Add(newBatch);
                }
                else // diff < 0
                {
                    // CASE B: Actual < System → THỪA → XUẤT (DECREASE)
                    // VD: Actual 80, System 100 → thừa 20 → phải xuất 20
                    
                    var quantityToReduce = Math.Abs(diff);

                    // Giảm Inventory
                    if (inventory != null)
                    {
                        if (inventory.Quantity < quantityToReduce)
                        {
                            throw new Exception($"Không đủ tồn kho để xuất cho sản phẩm {detail.Product.ProductName}. Tồn: {inventory.Quantity}, Cần xuất: {quantityToReduce}");
                        }

                        inventory.Quantity -= quantityToReduce;
                        inventory.LastUpdated = DateTime.Now;
                        inventoryUpdates[detail.ProductId] = inventory;
                    }

                    // Áp dụng FIFO: Xuất từ các lô cũ nhất trước
                    var batches = await _stockBatchRepository.GetQueryable()
                        .Where(b => b.WarehouseId == warehouseId 
                                 && b.ProductId == detail.ProductId
                                 && b.QuantityIn > b.QuantityOut
                                 && b.IsActive == true)
                        .OrderBy(b => b.ImportDate) // FIFO: oldest first
                        .ToListAsync();

                    decimal remaining = quantityToReduce;

                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;

                        decimal available = (batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0);
                        if (available <= 0) continue;

                        decimal take = Math.Min(available, remaining);
                        
                        batch.QuantityOut = (batch.QuantityOut ?? 0) + take;
                        batch.LastUpdated = DateTime.Now;

                        // Nếu hết batch, mark IsActive = false
                        if (batch.QuantityIn <= batch.QuantityOut)
                        {
                            batch.IsActive = false;
                        }

                        _stockBatchRepository.Update(batch);
                        remaining -= take;
                    }

                    if (remaining > 0)
                    {
                        throw new Exception($"Không đủ hàng trong các lô để xuất cho sản phẩm {detail.Product.ProductName}. Còn thiếu: {remaining}");
                    }
                }
            }

            // Lưu tất cả inventory updates
            foreach (var inv in inventoryUpdates.Values)
            {
                _inventoryRepository.Update(inv);
            }

            // LƯU SystemQuantity vào StockAdjustmentDetail
            foreach (var detail in detailUpdates)
            {
                _stockAdjustmentDetailRepository.Update(detail);
            }

            // Step 4: Update Draft → Resolved
            stockAdjustment.Status = (int)StockAdjustmentStatus.Resolved;
            stockAdjustment.ResolvedAt = DateTime.Now;
            _stockAdjustmentRepository.Update(stockAdjustment);

            // Save all changes
            await _inventoryRepository.SaveAsync();
            await _stockBatchRepository.SaveAsync();
            await _stockAdjustmentDetailRepository.SaveAsync();
            await _stockAdjustmentRepository.SaveAsync();

            // Return response với SystemQuantity ĐÃ LƯU trong DB
            var response = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = stockAdjustment.AdjustmentId,
                WarehouseId = stockAdjustment.WarehouseId,
                WarehouseName = stockAdjustment.Warehouse.WarehouseName,
                Status = stockAdjustment.Status,
                StatusDescription = ((StockAdjustmentStatus)stockAdjustment.Status).GetDescription(),
                CreatedAt = stockAdjustment.CreatedAt,
                Details = details.Select(d =>
                {
                    // SystemQuantity đã được lưu vào DB tại thời điểm resolve
                    return new StockAdjustmentDetailResponseVM
                    {
                        DetailId = d.DetailId,
                        ProductId = d.ProductId,
                        ProductName = d.Product.ProductName,
                        ActualQuantity = d.ActualQuantity,
                        SystemQuantity = d.SystemQuantity, // Đã lưu trong DB
                        Difference = d.ActualQuantity - d.SystemQuantity, 
                        Note = d.Note,
                        CreatedAt = d.CreatedAt
                    };
                }).ToList()
            };

            return response;
        }

        /// <summary>
        /// Hủy/Xóa phiếu kiểm kho nháp
        /// CHỈ cho phép khi Status = Draft (1)
        /// KHÔNG cho phép khi Status = Resolved (2)
        /// KHÔNG tác động đến Inventory hoặc StockBatch
        /// </summary>
        public async Task<bool> DeleteDraftAsync(int id)
        {
            // Step 1: Lấy phiếu kiểm kho
            var stockAdjustment = await _stockAdjustmentRepository.GetQueryable()
                .Include(sa => sa.StockAdjustmentDetails)
                .FirstOrDefaultAsync(sa => sa.AdjustmentId == id);

            if (stockAdjustment == null)
            {
                throw new Exception($"Không tìm thấy phiếu kiểm kho với Id {id}");
            }

            // Step 2: VALIDATION - CHỈ cho phép xóa Draft
            if (stockAdjustment.Status == (int)StockAdjustmentStatus.Resolved)
            {
                throw new Exception("Không thể hủy phiếu kiểm kho đã được xác nhận (Resolved). Phiếu đã Resolved là READ-ONLY và không thể xóa hoặc chỉnh sửa.");
            }

            if (stockAdjustment.Status == (int)StockAdjustmentStatus.Cancelled)
            {
                throw new Exception("Phiếu kiểm kho này đã bị hủy trước đó.");
            }

            if (stockAdjustment.Status != (int)StockAdjustmentStatus.Draft)
            {
                throw new Exception($"Chỉ có thể hủy phiếu kiểm kho ở trạng thái Nháp. Trạng thái hiện tại: {((StockAdjustmentStatus)stockAdjustment.Status).GetDescription()}");
            }

            // Step 3: OPTION A - Đánh dấu Cancelled (khuyến nghị cho audit trail)
            
            // CÁCH 1: Đánh dấu Cancelled (giữ lại để audit)
            stockAdjustment.Status = (int)StockAdjustmentStatus.Cancelled;
            stockAdjustment.ResolvedAt = DateTime.Now; // Dùng ResolvedAt để lưu thời gian hủy
            _stockAdjustmentRepository.Update(stockAdjustment);

            await _stockAdjustmentRepository.SaveAsync();
            return true;
        }
    }
}

