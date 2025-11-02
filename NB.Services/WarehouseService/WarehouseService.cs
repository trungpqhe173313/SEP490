using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.WarehouseService.Dto;
using NB.Service.WarehouseService.ViewModels;
using NB.Services.WarehouseService.ViewModels;
using OfficeOpenXml;


namespace NB.Service.WarehouseService
{
    public class WarehouseService : Service<Warehouse>,IWarehouseService
    {
        public WarehouseService(IRepository<Warehouse> serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<PagedList<WarehouseDto?>> GetData(WarehouseSearch search)
        {
            var query = from warehouse in GetQueryable()
                        select new WarehouseDto()
                        {
                            WarehouseId = warehouse.WarehouseId,
                            WarehouseName = warehouse.WarehouseName,
                            Location = warehouse.Location,
                            Capacity = warehouse.Capacity,
                            Status = warehouse.Status,
                            Note = warehouse.Note,
                            CreatedAt = warehouse.CreatedAt
                        };
            if(search != null)
            {
                if (!string.IsNullOrEmpty(search.WarehouseName))
                {
                    var keyword = search.WarehouseName.Trim();
                    query = query.Where(w => EF.Functions.Collate(w.WarehouseName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
            }
            query = query.OrderByDescending(w => w.WarehouseId);
            return await PagedList<WarehouseDto>.CreateAsync(query, search);
        }

        public async Task<WarehouseDto?> GetById(int search)
        {
            var query = from warehouse in GetQueryable()
                        where warehouse.WarehouseId == search
                        select new WarehouseDto()
                        {
                            WarehouseId = warehouse.WarehouseId,
                            WarehouseName = warehouse.WarehouseName,
                            Location = warehouse.Location,
                            Capacity = warehouse.Capacity,
                            Status = warehouse.Status,
                            Note = warehouse.Note,
                            CreatedAt = warehouse.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<WarehouseDto?> GetByWarehouseStatus(int status)
        {
            var query = from warehouse in GetQueryable()
                        where warehouse.Status == status
                        select new WarehouseDto()
                        {
                            WarehouseId = warehouse.WarehouseId,
                            WarehouseName = warehouse.WarehouseName,
                            Location = warehouse.Location,
                            Capacity = warehouse.Capacity,
                            Status = warehouse.Status,
                            Note = warehouse.Note,
                            CreatedAt = warehouse.CreatedAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<WarehouseImportResultVM> ImportFromExcelAsync(Stream excelStream)
        {
            var result = new WarehouseImportResultVM();

            try
            {
                using (var package = new ExcelPackage(excelStream))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Get first worksheet
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        result.ErrorMessages.Add("File Excel không có dữ liệu hoặc chỉ có header.");
                        return result;
                    }

                    result.TotalRows = rowCount - 1; // Exclude header row

                    // Start from row 2 (skip header)
                    for (int row = 3; row < rowCount; row++)
                    {
                        try
                        {
                            // Read data from Excel columns
                            var warehouseName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            var location = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                            var capacityStr = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                            var statusStr = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                            var isActiveStr = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                            var note = worksheet.Cells[row, 6].Value?.ToString()?.Trim();

                            // Validate required fields
                            var rowErrors = new List<string>();

                            if (string.IsNullOrWhiteSpace(warehouseName))
                                rowErrors.Add($"Dòng {row}: Tên kho không được để trống");

                            if (string.IsNullOrWhiteSpace(location))
                                rowErrors.Add($"Dòng {row}: Vị trí không được để trống");

                            if (!int.TryParse(capacityStr, out int capacity))
                                rowErrors.Add($"Dòng {row}: Sức chứa phải là số nguyên");

                            string status = null;
                            if (!string.IsNullOrWhiteSpace(statusStr))
                            {
                                if (statusStr == "0" || statusStr == "Đóng" || statusStr == "Close")
                                    status = "Đóng";
                                if (statusStr == "1" || statusStr == "Mở" || statusStr == "Open")
                                    status = "Mở";
                                if (statusStr == "2" || statusStr == "Đang sửa chữa" || statusStr == "Repairing")
                                    status = "Đang sửa chữa";
                                else
                                    rowErrors.Add($"Dòng {row}: Trạng thái phải là số nguyên");
                            }

                            bool? isActive = null;
                            if (!string.IsNullOrWhiteSpace(isActiveStr))
                            {
                                var lowerIsActive = isActiveStr.ToLower();
                                if (lowerIsActive == "true" || lowerIsActive == "1" || lowerIsActive == "có" || lowerIsActive == "yes")
                                    isActive = true;
                                else if (lowerIsActive == "false" || lowerIsActive == "0" || lowerIsActive == "không" || lowerIsActive == "no")
                                    isActive = false;
                                else
                                    rowErrors.Add($"Dòng {row}: IsActive phải là true/false hoặc 1/0");
                            }

                            if (rowErrors.Any())
                            {
                                result.ErrorMessages.AddRange(rowErrors);
                                result.FailedCount++;
                                continue;
                            }
                            int statusInt = 0;
                            if (status == "Đóng")
                            {
                                statusInt = 0;
                            }
                            else if (status == "Mở")
                            {
                                statusInt = 1;
                            }
                            else if (status == "Đang sửa chữa")
                            {
                                statusInt = 2;
                            }
                            // Create warehouse entity
                            var warehouse = new Warehouse
                            {
                                WarehouseName = warehouseName!,
                                Location = location!,
                                Capacity = capacity,
                                Status = statusInt,
                                IsActive = isActive,
                                Note = note,
                                CreatedAt = DateTime.UtcNow
                            };

                            // Save to database
                            await CreateAsync(warehouse);

                            // Add to success list
                            
                            result.ImportedWarehouses.Add(new WarehouseOutputVM
                            {
                                WarehouseId = warehouse.WarehouseId,
                                WarehouseName = warehouse.WarehouseName,
                                Location = warehouse.Location,
                                Capacity = warehouse.Capacity,
                                Status = status,
                                IsActive = warehouse.IsActive,
                                Note = warehouse.Note,
                                CreatedAt = warehouse.CreatedAt
                            });

                            result.SuccessCount++;
                        }
                        catch (Exception ex)
                        {
                            result.ErrorMessages.Add($"Dòng {row}: {ex.Message}");
                            result.FailedCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Lỗi khi đọc file Excel: {ex.Message}");
            }

            return result;
        }
    }
}
