using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.TransactionService.Dto;
using System.Linq;


namespace NB.Service.TransactionService
{
    public class TransactionService : Service<Transaction>, ITransactionService
    {
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Supplier> _supplierRepository;

        public TransactionService(
            IRepository<Transaction> repository,
            IRepository<Warehouse> warehouseRepository,
            IRepository<User> userRepository,
            IRepository<Supplier> supplierRepository) : base(repository)
        {
            _warehouseRepository = warehouseRepository;
            _userRepository = userRepository;
            _supplierRepository = supplierRepository;
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
    }
}
