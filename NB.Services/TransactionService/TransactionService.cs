using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.TransactionDetailService;
using NB.Service.ProductService;
using NB.Service.InventoryService;
using NB.Service.StockBatchService;
using NB.Service.UserService;
using NB.Service.WarehouseService;
using NB.Service.SupplierService;
using System.Linq;


namespace NB.Service.TransactionService
{
    public class TransactionService : Service<Transaction>, ITransactionService
    {
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly IStockBatchService _stockBatchService;

        public TransactionService(
            IRepository<Transaction> repository,
            IRepository<Warehouse> warehouseRepository,
            IRepository<User> userRepository,
            IRepository<Supplier> supplierRepository,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IInventoryService inventoryService,
            IStockBatchService stockBatchService) : base(repository)
        {
            _warehouseRepository = warehouseRepository;
            _userRepository = userRepository;
            _supplierRepository = supplierRepository;
            _transactionDetailService = transactionDetailService;
            _productService = productService;
            _inventoryService = inventoryService;
            _stockBatchService = stockBatchService;
        }
        public async Task<List<TransactionDto>> GetById(int? id)
        {
            var query = from t in GetQueryable()
                        where t.TransactionId == id
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            TotalWeight = t.TotalWeight,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note,
                            TotalCost = t.TotalCost,
                            ResponsibleId = t.ResponsibleId
                        };
            return await query.ToListAsync();
        }

        public async Task<PagedList<TransactionDto>> GetByListStatus(TransactionSearch search, List<int> listStatus)
        {
            var query = from t in GetQueryable()
                        where listStatus.Contains((int)t.Status)
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            TotalWeight = t.TotalWeight,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note,
                            TotalCost = t.TotalCost,
                            ResponsibleId = t.ResponsibleId
                        };
            if (search != null)
            {
                if (search.CustomerId.HasValue)
                {
                    query = query.Where(t => t.CustomerId == search.CustomerId);
                }
                if (search.SupplierId.HasValue)
                {
                    query = query.Where(t => t.SupplierId == search.SupplierId);
                }
                if (search.WarehouseId.HasValue)
                {
                    query = query.Where(t => t.WarehouseId == search.WarehouseId);
                }
                if (search.Status.HasValue)
                {
                    query = query.Where(t => t.Status.Value == search.Status.Value);
                }
                if (!string.IsNullOrEmpty(search.Type))
                {
                    var keyword = search.Type.Trim();
                    query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.TransactionFromDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= search.TransactionFromDate);
                }
                if (search.TransactionToDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= search.TransactionToDate);
                }
                if (search.ResponsibleId.HasValue && search.ResponsibleId.Value > 0)
                {
                    query = query.Where(t => t.ResponsibleId == search.ResponsibleId);
                }
            }

            query = query.OrderByDescending(t => t.TransactionDate);
            return await PagedList<TransactionDto>.CreateAsync(query, search);
        }

        public async Task<TransactionDto?> GetByTransactionId(int? id)
        {
            var query = from t in GetQueryable()
                        where t.TransactionId == id
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            TotalWeight = t.TotalWeight,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note,
                            TotalCost = t.TotalCost,
                            PriceListId = t.PriceListId,
                            ResponsibleId = t.ResponsibleId
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<PagedList<TransactionDto>> GetData(TransactionSearch search)
        {
       

            var query = from t in GetQueryable()
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            TotalWeight = t.TotalWeight,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note,
                            TotalCost = t.TotalCost,
                            ResponsibleId = t.ResponsibleId
                        };
            if (search != null)
            {
                if(search.SupplierId > 0)
                {
                    query = query.Where(t => t.SupplierId == search.SupplierId);
                }
                if (search.WarehouseId > 0)
                {
                    query = query.Where(t => t.WarehouseId == search.WarehouseId);
                }
                if (search.Status >= 0)
                {
                    query = query.Where(t => t.Status == search.Status);
                }
                if (!string.IsNullOrEmpty(search.Type))
                {
                    var keyword = search.Type.Trim();
                    // Nếu Type là "Export", gán query trả về rỗng
                    if (keyword.Equals("Export", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => 1 == 0);
                    }
                    else if (keyword.Equals("Import", StringComparison.OrdinalIgnoreCase))
                    {

                        query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI") == "Import");
                    }
                    else if (keyword.Equals("Transfer", StringComparison.OrdinalIgnoreCase))
                    {

                        query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI") == "Transfer");
                    }
                }
                else
                {
                    
                    query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI") == "Import"
                                          || EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI") == "Transfer");
                }

                if (search.TransactionFromDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= search.TransactionFromDate);
                }
                if (search.TransactionToDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= search.TransactionToDate);
                }
                if (search.ResponsibleId.HasValue && search.ResponsibleId.Value > 0)
                {
                    query = query.Where(t => t.ResponsibleId == search.ResponsibleId);
                }
            }

            query = query.OrderByDescending(t => t.TransactionDate);
            return await PagedList<TransactionDto>.CreateAsync(query, search);
        }

    

        public async Task<PagedList<TransactionDto>> GetDataForExport(TransactionSearch search)
        {
            var query = from t in GetQueryable()
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            TotalWeight = t.TotalWeight,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note,
                            TotalCost = t.TotalCost,
                            PriceListId = t.PriceListId,
                            ResponsibleId = t.ResponsibleId
                        };
            if(search != null)
            {
                if (search.CustomerId.HasValue)
                {
                    query = query.Where(t => t.CustomerId == search.CustomerId);
                }
                if(search.SupplierId.HasValue)
                {
                    query = query.Where(t => t.SupplierId == search.SupplierId);
                }
                if(search.WarehouseId.HasValue)
                {
                    query = query.Where(t => t.WarehouseId == search.WarehouseId);
                }
                if (search.Status.HasValue)
                {
                    query = query.Where(t => t.Status.Value == search.Status.Value);
                }
                if (!string.IsNullOrEmpty(search.Type))
                {
                    var keyword = search.Type.Trim();
                    query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.TransactionFromDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= search.TransactionFromDate);
                }
                if (search.TransactionToDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= search.TransactionToDate);
                }
                if (search.ResponsibleId.HasValue && search.ResponsibleId.Value > 0)
                {
                    query = query.Where(t => t.ResponsibleId == search.ResponsibleId);
                }
            }

            query = query.OrderByDescending(t => t.TransactionDate);
            return await PagedList<TransactionDto>.CreateAsync(query,search);
        }

        public async Task<TransactionDetailResponseDto> GetTransactionDetailById(int transactionId)
        {
            var transaction = await GetQueryable()
                .Include(t => t.TransactionDetails)
                    .ThenInclude(td => td.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null)
            {
                throw new ArgumentException($"Không tìm thấy giao dịch với ID {transactionId}");
            }

            // Lấy tên Warehouse
            var warehouse = await _warehouseRepository.GetByIdAsync(transaction.WarehouseId);
            string warehouseName = warehouse?.WarehouseName ?? "N/A";

            // Lấy tên WarehouseIn (nếu có - dùng cho Transfer)
            string? warehouseInName = null;
            if (transaction.WarehouseInId.HasValue)
            {
                var warehouseIn = await _warehouseRepository.GetByIdAsync(transaction.WarehouseInId.Value);
                warehouseInName = warehouseIn?.WarehouseName;
            }

            // Lấy tên Customer (nếu có - dùng cho Export)
            string? customerName = null;
            if (transaction.CustomerId.HasValue)
            {
                var customer = await _userRepository.GetByIdAsync(transaction.CustomerId.Value);
                customerName = customer?.FullName;
            }

            // Lấy tên Supplier (nếu có - dùng cho Import)
            string? supplierName = null;
            if (transaction.SupplierId.HasValue)
            {
                var supplier = await _supplierRepository.GetByIdAsync(transaction.SupplierId.Value);
                supplierName = supplier?.SupplierName;
            }

            // Lấy tên người chịu trách nhiệm (nếu có)
            string? responsibleName = null;
            if (transaction.ResponsibleId.HasValue)
            {
                var responsible = await _userRepository.GetByIdAsync(transaction.ResponsibleId.Value);
                responsibleName = responsible?.FullName;
            }

            var details = transaction.TransactionDetails.Select(td => new TransactionDetailItemDto
            {
                ProductId = td.ProductId,
                ProductName = td.Product?.ProductName ?? "N/A",
                Quantity = td.Quantity,
                UnitPrice = td.UnitPrice,
                TotalPrice = td.Quantity * td.UnitPrice,
                TotalWeight = td.Product?.WeightPerUnit.HasValue == true 
                    ? td.Quantity * td.Product.WeightPerUnit.Value 
                    : null
            }).ToList();

            return new TransactionDetailResponseDto
            {
                TransactionId = transaction.TransactionId,
                Type = transaction.Type,
                Status = transaction.Status?.ToString() ?? "N/A",
                TransactionDate = transaction.TransactionDate,
                TransactionCode = transaction.TransactionCode,
                Note = transaction.Note,
                TotalWeight = transaction.TotalWeight,
                TotalCost = transaction.TotalCost,
                WarehouseId = transaction.WarehouseId,
                WarehouseName = warehouseName,
                WarehouseInId = transaction.WarehouseInId,
                WarehouseInName = warehouseInName,
                CustomerId = transaction.CustomerId,
                CustomerName = customerName,
                SupplierId = transaction.SupplierId,
                SupplierName = supplierName,
                PriceListId = transaction.PriceListId,
                ResponsibleId = transaction.ResponsibleId,
                ResponsibleName = responsibleName,
                Details = details
            };
        }

        public async Task<ImportWeightSummaryDto> GetImportWeightAsync(DateTime fromDate, DateTime toDate)
        {
            // Lấy tất cả Import transactions đã hoàn thành trong khoảng thời gian
            // Status: done(4), checked(8), paidInFull(11), partiallyPaid(12)
            var transactions = await GetQueryable()
                .Where(t => t.Type == "Import"
                    && t.TransactionDate >= fromDate
                    && t.TransactionDate <= toDate
                    && (t.Status == 4 || t.Status == 8 || t.Status == 11 || t.Status == 12))
                .ToListAsync();

            var totalWeight = transactions.Sum(t => t.TotalWeight ?? 0);
            var transactionCount = transactions.Count;

            // Group theo Supplier để lấy chi tiết
            var details = new List<ImportWeightDetailDto>();
            var groupedBySupplier = transactions
                .Where(t => t.SupplierId.HasValue)
                .GroupBy(t => t.SupplierId!.Value);

            foreach (var group in groupedBySupplier)
            {
                var supplier = await _supplierRepository.GetByIdAsync(group.Key);
                details.Add(new ImportWeightDetailDto
                {
                    SupplierId = group.Key,
                    SupplierName = supplier?.SupplierName ?? "N/A",
                    TotalWeight = group.Sum(t => t.TotalWeight ?? 0),
                    TransactionCount = group.Count()
                });
            }

            return new ImportWeightSummaryDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalWeight = totalWeight,
                TransactionCount = transactionCount,
                Details = details.OrderByDescending(d => d.TotalWeight).ToList()
            };
        }

        public async Task<ExportWeightSummaryDto> GetExportWeightAsync(DateTime fromDate, DateTime toDate)
        {
            // Lấy tất cả Export transactions đã hoàn thành trong khoảng thời gian
            // Status: done(4), paidInFull(11), partiallyPaid(12)
            var transactions = await GetQueryable()
                .Where(t => t.Type == "Export"
                    && t.TransactionDate >= fromDate
                    && t.TransactionDate <= toDate
                    && (t.Status == 4 || t.Status == 11 || t.Status == 12))
                .ToListAsync();

            var totalWeight = transactions.Sum(t => t.TotalWeight ?? 0);
            var transactionCount = transactions.Count;

            // Group theo Customer để lấy chi tiết
            var details = new List<ExportWeightDetailDto>();
            var groupedByCustomer = transactions
                .Where(t => t.CustomerId.HasValue)
                .GroupBy(t => t.CustomerId!.Value);

            foreach (var group in groupedByCustomer)
            {
                var customer = await _userRepository.GetByIdAsync(group.Key);
                details.Add(new ExportWeightDetailDto
                {
                    CustomerId = group.Key,
                    CustomerName = customer?.FullName ?? "N/A",
                    TotalWeight = group.Sum(t => t.TotalWeight ?? 0),
                    TransactionCount = group.Count()
                });
            }

            return new ExportWeightSummaryDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalWeight = totalWeight,
                TransactionCount = transactionCount,
                Details = details.OrderByDescending(d => d.TotalWeight).ToList()
            };
        }

        public async Task<SubmitForApprovalResult> SubmitForApprovalAsync(int transactionId, SubmitForApprovalVM model)
        {
            // Kiểm tra transaction có tồn tại không
            var transaction = await GetByIdAsync(transactionId);
            if (transaction == null)
            {
                return new SubmitForApprovalResult
                {
                    Success = false,
                    Message = "Không tìm thấy đơn nhập kho"
                };
            }

            // Kiểm tra loại transaction phải là Import
            if (transaction.Type != "Import")
            {
                return new SubmitForApprovalResult
                {
                    Success = false,
                    Message = "Đơn hàng không phải là đơn nhập kho"
                };
            }

            // Kiểm tra trạng thái phải là "Đang kiểm" (status = 1)
            if (transaction.Status != (int)TransactionStatus.importChecking)
            {
                return new SubmitForApprovalResult
                {
                    Success = false,
                    Message = "Chỉ có thể gửi phê duyệt cho đơn hàng đang ở trạng thái 'Đang kiểm'"
                };
            }

            // Kiểm tra quyền: User phải là người chịu trách nhiệm
            if (!transaction.ResponsibleId.HasValue || transaction.ResponsibleId.Value != model.ResponsibleId)
            {
                return new SubmitForApprovalResult
                {
                    Success = false,
                    Message = "Bạn không có quyền gửi phê duyệt cho đơn hàng này"
                };
            }

            // Kiểm tra người chịu trách nhiệm có tồn tại
            var responsibleUser = await _userRepository.GetByIdAsync(model.ResponsibleId);
            if (responsibleUser == null)
            {
                return new SubmitForApprovalResult
                {
                    Success = false,
                    Message = "Không tìm thấy người chịu trách nhiệm"
                };
            }

            // Lấy tất cả TransactionDetails của đơn hàng
            var transactionDetails = await _transactionDetailService.GetByTransactionId(transactionId);

            if (transactionDetails == null || !transactionDetails.Any())
            {
                return new SubmitForApprovalResult
                {
                    Success = false,
                    Message = "Không tìm thấy chi tiết đơn hàng"
                };
            }

            // Kiểm tra tất cả products trong request có tồn tại trong TransactionDetail không
            var transactionDetailDict = transactionDetails.ToDictionary(td => td.ProductId, td => td);

            foreach (var product in model.Products)
            {
                if (!transactionDetailDict.ContainsKey(product.ProductId))
                {
                    return new SubmitForApprovalResult
                    {
                        Success = false,
                        Message = $"Sản phẩm với ID {product.ProductId} không có trong đơn hàng này"
                    };
                }

                // Validate actualQuantity phải là số nguyên không âm
                if (product.ActualQuantity < 0)
                {
                    return new SubmitForApprovalResult
                    {
                        Success = false,
                        Message = $"Số lượng thực tế của sản phẩm {product.ProductId} không được âm"
                    };
                }
            }

            // Cập nhật số lượng thực tế cho từng TransactionDetail
            decimal totalCost = 0;
            decimal totalWeight = 0;

            foreach (var product in model.Products)
            {
                var detail = transactionDetailDict[product.ProductId];

                // Cập nhật quantity = actualQuantity
                detail.Quantity = product.ActualQuantity;

                // Tính lại tổng
                totalCost += product.ActualQuantity * detail.UnitPrice;

                // Lấy thông tin sản phẩm để tính trọng lượng
                var productInfo = await _productService.GetById(product.ProductId);
                if (productInfo != null)
                {
                    totalWeight += product.ActualQuantity * (productInfo.WeightPerUnit ?? 0);
                }

                // Lưu cập nhật
                await _transactionDetailService.UpdateAsync(detail);
            }

            // Cập nhật Transaction
            transaction.Status = (int)TransactionStatus.pendingWarehouseApproval; // Status = 4
            transaction.TotalCost = totalCost;
            transaction.TotalWeight = totalWeight;

            // Cập nhật note nếu có
            if (!string.IsNullOrWhiteSpace(model.Note))
            {
                transaction.Note = string.IsNullOrWhiteSpace(transaction.Note)
                    ? model.Note
                    : $"{transaction.Note}\n[Gửi phê duyệt lúc {DateTime.Now:dd/MM/yyyy HH:mm}]: {model.Note}";
            }

            await UpdateAsync(transaction);

            return new SubmitForApprovalResult
            {
                Success = true,
                Message = "Gửi phê duyệt thành công",
                TransactionId = transaction.TransactionId,
                Status = transaction.Status ?? 0,
                TotalCost = transaction.TotalCost
            };
        }

        public async Task<ApproveImportResult> ApproveImportAsync(int transactionId, ApproveImportVM model)
        {
            // Kiểm tra transaction có tồn tại không
            var transaction = await GetByIdAsync(transactionId);
            if (transaction == null)
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = "Không tìm thấy đơn nhập kho"
                };
            }

            // Kiểm tra loại transaction phải là Import
            if (transaction.Type != "Import")
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = "Đơn hàng không phải là đơn nhập kho"
                };
            }

            // Kiểm tra trạng thái phải là "Chờ phê duyệt kho" (status = 4)
            if (transaction.Status != (int)TransactionStatus.pendingWarehouseApproval)
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = "Chỉ có thể phê duyệt đơn hàng đang ở trạng thái 'Chờ phê duyệt kho'"
                };
            }

            // Kiểm tra approverId có tồn tại
            var approver = await _userRepository.GetByIdAsync(model.ApproverId);
            if (approver == null)
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = "Không tìm thấy người phê duyệt"
                };
            }

            // Lấy TransactionDetails
            var transactionDetails = await _transactionDetailService.GetByTransactionId(transactionId);

            if (transactionDetails == null || !transactionDetails.Any())
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = "Không tìm thấy chi tiết đơn hàng"
                };
            }

            // Lấy thông tin kho
            var warehouse = await _warehouseRepository.GetByIdAsync(transaction.WarehouseId);
            if (warehouse == null)
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = $"Không tìm thấy kho với ID: {transaction.WarehouseId}"
                };
            }

            // Validate ExpireDate không được là ngày quá khứ
            if (model.ExpireDate.Date < DateTime.Now.Date)
            {
                return new ApproveImportResult
                {
                    Success = false,
                    Message = "Ngày hết hạn không được là ngày quá khứ"
                };
            }

            DateTime expireDate = model.ExpireDate;

            // Tạo StockBatch và cập nhật Inventory
            string batchCodePrefix = "BATCH-NUMBER";
            int batchCounter = 1;
            Dictionary<string, InventoryService.Dto.InventoryDto> inventoryCache = new Dictionary<string, InventoryService.Dto.InventoryDto>();

            foreach (var detail in transactionDetails)
            {
                // Tạo BatchCode unique
                string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                {
                    batchCounter++;
                    uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                }

                // Tạo StockBatch
                var newStockBatch = new StockBatchService.Dto.StockBatchDto
                {
                    WarehouseId = transaction.WarehouseId,
                    ProductId = detail.ProductId,
                    TransactionId = transactionId,
                    BatchCode = uniqueBatchCode,
                    ImportDate = DateTime.Now,
                    ExpireDate = expireDate,
                    QuantityIn = detail.Quantity,
                    Status = 1, // Còn hàng
                    IsActive = true,
                    LastUpdated = DateTime.Now,
                    Note = transaction.Note
                };
                await _stockBatchService.CreateAsync(newStockBatch);

                // Cập nhật Inventory với cache
                string inventoryKey = $"{transaction.WarehouseId}_{detail.ProductId}";

                if (!inventoryCache.ContainsKey(inventoryKey))
                {
                    var existInventory = await _inventoryService.GetByWarehouseAndProductId(transaction.WarehouseId, detail.ProductId);

                    if (existInventory == null)
                    {
                        var newInventory = new InventoryService.Dto.InventoryDto
                        {
                            WarehouseId = transaction.WarehouseId,
                            ProductId = detail.ProductId,
                            Quantity = detail.Quantity,
                            LastUpdated = DateTime.Now
                        };
                        inventoryCache[inventoryKey] = newInventory;
                    }
                    else
                    {
                        existInventory.Quantity = (existInventory.Quantity ?? 0) + detail.Quantity;
                        existInventory.LastUpdated = DateTime.Now;
                        inventoryCache[inventoryKey] = existInventory;
                    }
                }
                else
                {
                    var cachedInventory = inventoryCache[inventoryKey];
                    cachedInventory.Quantity = (cachedInventory.Quantity ?? 0) + detail.Quantity;
                    cachedInventory.LastUpdated = DateTime.Now;
                }

                batchCounter++;
            }

            // Lưu Inventory vào DB
            foreach (var inv in inventoryCache.Values)
            {
                if (inv.InventoryId == 0)
                {
                    await _inventoryService.CreateAsync(inv);
                }
                else
                {
                    await _inventoryService.UpdateAsync(inv);
                }
            }

            // Cập nhật Transaction
            transaction.Status = (int)TransactionStatus.importReceived; // Status = 2 - Đã nhận hàng

            // Thêm ghi chú về việc phê duyệt
            var approverName = approver.FullName ?? approver.Username ?? "N/A";
            var approvalNote = $"[Phê duyệt bởi {approverName} lúc {DateTime.Now:dd/MM/yyyy HH:mm}]";
            transaction.Note = string.IsNullOrWhiteSpace(transaction.Note)
                ? approvalNote
                : $"{transaction.Note}\n{approvalNote}";

            await UpdateAsync(transaction);

            return new ApproveImportResult
            {
                Success = true,
                Message = "Phê duyệt đơn nhập kho thành công",
                TransactionId = transaction.TransactionId,
                Status = transaction.Status ?? 0,
                ApprovedBy = model.ApproverId,
                ApprovedDate = DateTime.Now
            };
        }

        public async Task<RejectImportResult> RejectImportAsync(int transactionId, RejectImportVM model)
        {
            // Kiểm tra transaction có tồn tại không
            var transaction = await GetByIdAsync(transactionId);
            if (transaction == null)
            {
                return new RejectImportResult
                {
                    Success = false,
                    Message = "Không tìm thấy đơn nhập kho"
                };
            }

            // Kiểm tra loại transaction phải là Import
            if (transaction.Type != "Import")
            {
                return new RejectImportResult
                {
                    Success = false,
                    Message = "Đơn hàng không phải là đơn nhập kho"
                };
            }

            // Kiểm tra trạng thái phải là "Chờ phê duyệt kho" (status = 4)
            if (transaction.Status != (int)TransactionStatus.pendingWarehouseApproval)
            {
                return new RejectImportResult
                {
                    Success = false,
                    Message = "Chỉ có thể từ chối đơn hàng đang ở trạng thái 'Chờ phê duyệt kho'"
                };
            }

            // Kiểm tra approverId có tồn tại
            var approver = await _userRepository.GetByIdAsync(model.ApproverId);
            if (approver == null)
            {
                return new RejectImportResult
                {
                    Success = false,
                    Message = "Không tìm thấy người phê duyệt"
                };
            }

            // Kiểm tra lý do từ chối không được để trống
            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                return new RejectImportResult
                {
                    Success = false,
                    Message = "Lý do từ chối không được để trống"
                };
            }

            // Cập nhật Transaction - trả về trạng thái "Đang kiểm"
            transaction.Status = (int)TransactionStatus.importChecking; // Status = 1

            // Thêm ghi chú về việc từ chối
            var approverName = approver.FullName ?? approver.Username ?? "N/A";
            var rejectionNote = $"[Từ chối bởi {approverName} lúc {DateTime.Now:dd/MM/yyyy HH:mm}]: {model.Reason}";
            transaction.Note = string.IsNullOrWhiteSpace(transaction.Note)
                ? rejectionNote
                : $"{transaction.Note}\n{rejectionNote}";

            await UpdateAsync(transaction);

            return new RejectImportResult
            {
                Success = true,
                Message = "Từ chối đơn nhập kho thành công",
                TransactionId = transaction.TransactionId,
                Status = transaction.Status ?? 0,
                RejectedBy = model.ApproverId,
                RejectedDate = DateTime.Now,
                RejectionReason = model.Reason
            };
        }
    }
}
