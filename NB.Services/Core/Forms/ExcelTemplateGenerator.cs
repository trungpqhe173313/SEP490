using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace NB.Service.Core.Forms
{
    public static class ExcelTemplateGenerator
    {
        // Static constructor to set EPPlus license once
        static ExcelTemplateGenerator()
        {
            //ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
        }


        // Tạo template Excel cho Warehouse Import
        /// <returns>MemoryStream chứa file Excel template</returns>
        public static MemoryStream GenerateWarehouseTemplate()
        {
            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Warehouse Template");

                // Define headers
                var headers = new[]
                {
                    "WarehouseName",
                    "Location",
                    "Capacity",
                    "Status",
                    "IsActive",
                    "Note"
                };

                // Define header descriptions
                var descriptions = new[]
                {
                    "Tên kho (Bắt buộc)",
                    "Vị trí (Bắt buộc)",
                    "Sức chứa (Bắt buộc, số nguyên)",
                    "Trạng thái (Số nguyên, tùy chọn)",
                    "Kích hoạt (true/false, 1/0, tùy chọn)",
                    "Ghi chú (Tùy chọn)"
                };

                // Tạo Headers (Row 1)
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo descriptions (Row 2)
                for (int i = 0; i < descriptions.Length; i++)
                {
                    var cell = worksheet.Cells[2, i + 1];
                    cell.Value = descriptions[i];
                    cell.Style.Font.Italic = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 230, 241));
                    cell.Style.WrapText = true;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 3)
                var sampleData = new object[]
                {
                    "Kho Hà Nội",
                    "123 Đường ABC, Hà Nội",
                    10000,
                    1,
                    "true",
                    "Kho chính"
                };

                for (int i = 0; i < sampleData.Length; i++)
                {
                    var cell = worksheet.Cells[3, i + 1];
                    cell.Value = sampleData[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 4)
                var sampleData2 = new object[]
                {
                    "Kho TP.HCM",
                    "456 Đường XYZ, TP.HCM",
                    15000,
                    1,
                    1,
                    "Kho chi nhánh phía Nam"
                };

                for (int i = 0; i < sampleData2.Length; i++)
                {
                    var cell = worksheet.Cells[4, i + 1];
                    cell.Value = sampleData2[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                worksheet.Cells[1, 1, 4, headers.Length].AutoFitColumns();

                // Set độ rộng tối thiểu cho các cột
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (worksheet.Column(i).Width < 15)
                        worksheet.Column(i).Width = 15;
                }

                // Thêm sheet hướng dẫn
                var instructionSheet = package.Workbook.Worksheets.Add("Hướng Dẫn");

                instructionSheet.Cells["A1"].Value = "HƯỚNG DẪN IMPORT WAREHOUSE";
                instructionSheet.Cells["A1"].Style.Font.Bold = true;
                instructionSheet.Cells["A1"].Style.Font.Size = 16;

                instructionSheet.Cells["A3"].Value = "1. Cấu trúc file:";
                instructionSheet.Cells["A3"].Style.Font.Bold = true;
                instructionSheet.Cells["A4"].Value = "   - Dòng 1: Header (tên các cột)";
                instructionSheet.Cells["A5"].Value = "   - Dòng 2: Mô tả chi tiết (Không được xóa)";
                instructionSheet.Cells["A6"].Value = "   - Từ dòng 3 trở đi: Dữ liệu truyền vào";

                instructionSheet.Cells["A8"].Value = "2. Các trường bắt buộc:";
                instructionSheet.Cells["A8"].Style.Font.Bold = true;
                instructionSheet.Cells["A9"].Value = "   - WarehouseName: Tên kho (không được để trống)";
                instructionSheet.Cells["A10"].Value = "   - Location: Vị trí kho (không được để trống)";
                instructionSheet.Cells["A11"].Value = "   - Capacity: Sức chứa (phải là số nguyên)";

                instructionSheet.Cells["A13"].Value = "3. Các trường tùy chọn:";
                instructionSheet.Cells["A13"].Style.Font.Bold = true;
                instructionSheet.Cells["A14"].Value = "   - Status: Trạng thái (số nguyên, có thể để trống)";
                instructionSheet.Cells["A15"].Value = "   - IsActive: Kích hoạt (true/false, 1/0, có/không, yes/no)";
                instructionSheet.Cells["A16"].Value = "   - Note: Ghi chú (có thể để trống)";

                instructionSheet.Cells["A18"].Value = "4. Lưu ý:";
                instructionSheet.Cells["A18"].Style.Font.Bold = true;
                instructionSheet.Cells["A19"].Value = "   - WarehouseId và CreatedAt sẽ được tự động tạo bởi hệ thống";
                instructionSheet.Cells["A20"].Value = "   - Các dòng màu vàng là dữ liệu mẫu, bạn có thể xóa và thay bằng dữ liệu thực";
                instructionSheet.Cells["A21"].Value = "   - Đảm bảo đúng thứ tự các cột như trong template";
                instructionSheet.Cells["A22"].Value = "   - File chỉ chấp nhận định dạng .xlsx hoặc .xls";
                instructionSheet.Cells["A23"].Value = "   - Kích thước file tối đa: 10MB";

                instructionSheet.Column(1).Width = 80;
                instructionSheet.Cells["A1:A23"].Style.WrapText = true;

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }


        //Tạo template Excel cho Stock Input Import

        /// <returns>MemoryStream chứa file Excel template</returns>
        public static MemoryStream GenerateStockInputTemplate()
        {
            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                // SHEET CHÍNH: Nhập kho (Gộp thông tin chung và danh sách sản phẩm)
                var mainSheet = package.Workbook.Worksheets.Add("Nhập kho");

                // Define headers (Thông tin chung + Thông tin sản phẩm)
                var headers = new[]
                {
                    "WarehouseName",
                    "SupplierName",
                    "ExpireDate",
                    "ProductName",
                    "Quantity",
                    "UnitPrice",
                    "Note"
                };

                // Define header descriptions
                var descriptions = new[]
                {
                    "Tên kho nhập (Chỉ điền ở dòng đầu)",
                    "Tên nhà cung cấp (Chỉ điền ở dòng đầu)",
                    "Ngày hết hạn chung (Chỉ điền ở dòng đầu)",
                    "Tên sản phẩm (Bắt buộc)",
                    "Số lượng (Bắt buộc, số nguyên > 0)",
                    "Giá nhập (Bắt buộc, số thực > 0)",
                    "Ghi chú (Tùy chọn)"
                };

                // Tạo headers (Row 1)
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = mainSheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

                    //Cột thông tin chung (A, B, C)
                    if (i < 3)
                    {
                        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 112, 192));
                    }
                    else //Cột sản phẩm (D, E, F, G)
                    {
                        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    }

                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo descriptions (Row 2)
                for (int i = 0; i < descriptions.Length; i++)
                {
                    var cell = mainSheet.Cells[2, i + 1];
                    cell.Value = descriptions[i];
                    cell.Style.Font.Italic = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 230, 241));
                    cell.Style.WrapText = true;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 3) - Dòng đầu tiên có đầy đủ thông tin
                var sampleData1 = new object[]
                {
                    "Kho Hà Nội",           // WarehouseName
                    "Nhà cung cấp ABC",     // SupplierName
                    "12-31-2025",           // ExpireDate
                    "Sản phẩm A",           // ProductName
                    1000,                   // Quantity
                    50000.50,               // UnitPrice
                    "Lô hàng đầu tiên"      // Note
                };

                for (int i = 0; i < sampleData1.Length; i++)
                {
                    var cell = mainSheet.Cells[3, i + 1];
                    cell.Value = sampleData1[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 4) - Dòng thứ 2 chỉ có thông tin sản phẩm
                var sampleData2 = new object[]
                {
                    "",                     // WarehouseName (để trống, lấy từ dòng 3)
                    "",                     // SupplierName (để trống, lấy từ dòng 3)
                    "",                     // ExpireDate (để trống, lấy từ dòng 3)
                    "Sản phẩm B",           // ProductName
                    500,                    // Quantity
                    75000.00,               // UnitPrice
                    "Lô hàng thứ hai"       // Note
                };

                for (int i = 0; i < sampleData2.Length; i++)
                {
                    var cell = mainSheet.Cells[4, i + 1];
                    cell.Value = sampleData2[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 5) - Dòng thứ 3 chỉ có thông tin sản phẩm
                var sampleData3 = new object[]
                {
                    "",                     // WarehouseName (để trống)
                    "",                     // SupplierName (để trống)
                    "",                     // ExpireDate (để trống)
                    "Sản phẩm C",           // ProductName
                    750,                    // Quantity
                    60000.00,               // UnitPrice
                    ""                      // Note (để trống)
                };

                for (int i = 0; i < sampleData3.Length; i++)
                {
                    var cell = mainSheet.Cells[5, i + 1];
                    cell.Value = sampleData3[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                mainSheet.Cells[1, 1, 5, headers.Length].AutoFitColumns();

                // Set độ rộng tối thiểu cho các cột
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (mainSheet.Column(i).Width < 15)
                        mainSheet.Column(i).Width = 15;
                }

                // Tạo sheet hướng dẫn
                var instructionSheet = package.Workbook.Worksheets.Add("Hướng Dẫn");
                instructionSheet.Cells["A1"].Value = "HƯỚNG DẪN IMPORT NHẬP KHO";
                instructionSheet.Cells["A1"].Style.Font.Bold = true;
                instructionSheet.Cells["A1"].Style.Font.Size = 16;

                instructionSheet.Cells["A3"].Value = "1. Cấu trúc file:";
                instructionSheet.Cells["A3"].Style.Font.Bold = true;
                instructionSheet.Cells["A4"].Value = "   - Dòng 1: Header (tên các cột)";
                instructionSheet.Cells["A5"].Value = "   - Dòng 2: Mô tả chi tiết";
                instructionSheet.Cells["A6"].Value = "   - Từ dòng 3 trở đi: Dữ liệu nhập kho (mỗi dòng là 1 sản phẩm)";

                instructionSheet.Cells["A8"].Value = "2. Thông tin chung (Cột A, B, C) - Chỉ điền ở dòng đầu tiên:";
                instructionSheet.Cells["A8"].Style.Font.Bold = true;
                instructionSheet.Cells["A9"].Value = "   - WarehouseName (Cột A): Tên kho nhập (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A10"].Value = "   - SupplierName (Cột B): Tên nhà cung cấp (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A11"].Value = "   - ExpireDate (Cột C): Ngày hết hạn: chung cho toàn bộ đơn ";
                instructionSheet.Cells["A12"].Value = "   → Chỉ cần điền ở dòng 3, các dòng tiếp theo để trống (hệ thống tự lấy từ dòng đầu)";

                instructionSheet.Cells["A14"].Value = "3. Thông tin sản phẩm (Cột D, E, F, G) - ĐIỀN Ở MỖI DÒNG:";
                instructionSheet.Cells["A14"].Style.Font.Bold = true;
                instructionSheet.Cells["A15"].Value = "   - ProductName (Cột D): Tên sản phẩm (bắt buộc, phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A16"].Value = "   - Quantity (Cột E): Số lượng nhập (bắt buộc, số nguyên > 0)";
                instructionSheet.Cells["A17"].Value = "   - UnitPrice (Cột F): Giá nhập (bắt buộc, số thực > 0, VD: 50000.50)";
                instructionSheet.Cells["A18"].Value = "   - Note (Cột G): Ghi chú cho sản phẩm (tùy chọn)";

                instructionSheet.Cells["A20"].Value = "4. Ví dụ cách điền:";
                instructionSheet.Cells["A20"].Style.Font.Bold = true;
                instructionSheet.Cells["A21"].Value = "   Dòng 3: Kho Hà Nội | Nhà cung cấp ABC | 12-31-2025 | Sản phẩm A | 1000 | 50000.50 | Ghi chú 1";
                instructionSheet.Cells["A22"].Value = "   Dòng 4: [Để trống] | [Để trống] | [Để trống] | Sản phẩm B | 500 | 75000 | Ghi chú 2";
                instructionSheet.Cells["A23"].Value = "   Dòng 5: [Để trống] | [Để trống] | [Để trống] | Sản phẩm C | 750 | 60000 | Ghi chú 3";

                instructionSheet.Cells["A25"].Value = "5. Lưu ý quan trọng:";
                instructionSheet.Cells["A25"].Style.Font.Bold = true;
                instructionSheet.Cells["A26"].Value = "   - Chỉ cần 1 sheet duy nhất: 'Nhập kho'";
                instructionSheet.Cells["A27"].Value = "   - Thông tin chung (tên kho, tên nhà cung cấp, ngày hết hạn) chỉ cần điền 1 lần (dòng 3)";
                instructionSheet.Cells["A28"].Value = "   - Các dòng tiếp theo chỉ cần điền thông tin sản phẩm (Cột D, E, F, G)";
                instructionSheet.Cells["A29"].Value = "   - Các dòng màu vàng là dữ liệu mẫu, có thể xóa và thay bằng dữ liệu thực";
                instructionSheet.Cells["A30"].Value = "   - File chỉ chấp nhận định dạng .xlsx hoặc .xls";
                instructionSheet.Cells["A31"].Value = "   - Kích thước file tối đa: 10MB";
                instructionSheet.Cells["A32"].Value = "   - Tên phải chính xác (tên kho, tên nhà cung cấp, ngày hết hạn)";
                instructionSheet.Cells["A33"].Value = "   - ExpireDate sẽ áp dụng cho toàn bộ sản phẩm trong đơn nhập";

                instructionSheet.Column(1).Width = 90;
                instructionSheet.Cells["A1:A33"].Style.WrapText = true;

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }


        // Tạo template Excel cho Product Import
        /// <returns>MemoryStream chứa file Excel template</returns>
        public static MemoryStream GenerateProductImportTemplate()
        {
            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                // SHEET CHÍNH: Nhập sản phẩm
                var mainSheet = package.Workbook.Worksheets.Add("Nhập sản phẩm");

                // Define headers
                var headers = new[]
                {
                    "SupplierName",
                    "CategoryName",
                    "ProductCode",
                    "ProductName",
                    "WeightPerUnit",
                    "SellingPrice",
                    "Description"
                };

                // Define header descriptions
                var descriptions = new[]
                {
                    "Tên nhà cung cấp (Bắt buộc)",
                    "Tên danh mục (Bắt buộc)",
                    "Mã sản phẩm (Bắt buộc, không trùng)",
                    "Tên sản phẩm (Bắt buộc, không trùng)",
                    "Trọng lượng/đơn vị (Bắt buộc, số >= 0)",
                    "Giá bán (Bắt buộc, số >= 0)",
                    "Mô tả (Tùy chọn)"
                };

                // Tạo headers (Row 1)
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = mainSheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo descriptions (Row 2)
                for (int i = 0; i < descriptions.Length; i++)
                {
                    var cell = mainSheet.Cells[2, i + 1];
                    cell.Value = descriptions[i];
                    cell.Style.Font.Italic = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 230, 241));
                    cell.Style.WrapText = true;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 3)
                var sampleData1 = new object[]
                {
                    "Nhà cung cấp ABC",     // SupplierName
                    "Danh mục Thực phẩm",   // CategoryName
                    "SP001",                // ProductCode
                    "Sản phẩm mẫu 1",       // ProductName
                    1.5,                    // WeightPerUnit
                    50000,                  // SellingPrice
                    "Mô tả sản phẩm 1"      // Description
                };

                for (int i = 0; i < sampleData1.Length; i++)
                {
                    var cell = mainSheet.Cells[3, i + 1];
                    cell.Value = sampleData1[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 4)
                var sampleData2 = new object[]
                {
                    "Nhà cung cấp XYZ",     // SupplierName
                    "Danh mục Đồ uống",     // CategoryName
                    "SP002",                // ProductCode
                    "Sản phẩm mẫu 2",       // ProductName
                    2.0,                    // WeightPerUnit
                    75000,                  // SellingPrice
                    "Mô tả sản phẩm 2"      // Description
                };

                for (int i = 0; i < sampleData2.Length; i++)
                {
                    var cell = mainSheet.Cells[4, i + 1];
                    cell.Value = sampleData2[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                mainSheet.Cells[1, 1, 4, headers.Length].AutoFitColumns();

                // Set độ rộng tối thiểu cho các cột
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (mainSheet.Column(i).Width < 15)
                        mainSheet.Column(i).Width = 15;
                }

                // Tạo sheet hướng dẫn
                var instructionSheet = package.Workbook.Worksheets.Add("Hướng Dẫn");
                instructionSheet.Cells["A1"].Value = "HƯỚNG DẪN IMPORT SẢN PHẨM";
                instructionSheet.Cells["A1"].Style.Font.Bold = true;
                instructionSheet.Cells["A1"].Style.Font.Size = 16;

                instructionSheet.Cells["A3"].Value = "1. Cấu trúc file:";
                instructionSheet.Cells["A3"].Style.Font.Bold = true;
                instructionSheet.Cells["A4"].Value = "   - Dòng 1: Header (tên các cột)";
                instructionSheet.Cells["A5"].Value = "   - Dòng 2: Mô tả chi tiết (Không được xóa)";
                instructionSheet.Cells["A6"].Value = "   - Từ dòng 3 trở đi: Dữ liệu sản phẩm (mỗi dòng là 1 sản phẩm)";

                instructionSheet.Cells["A8"].Value = "2. Các trường bắt buộc:";
                instructionSheet.Cells["A8"].Style.Font.Bold = true;
                instructionSheet.Cells["A9"].Value = "   - SupplierName: Tên nhà cung cấp (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A10"].Value = "   - CategoryName: Tên danh mục (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A11"].Value = "   - ProductCode: Mã sản phẩm (không được trùng với sản phẩm đã có)";
                instructionSheet.Cells["A12"].Value = "   - ProductName: Tên sản phẩm (không được trùng với sản phẩm đã có)";
                instructionSheet.Cells["A13"].Value = "   - WeightPerUnit: Trọng lượng trên đơn vị (số >= 0, VD: 1.5, 2.0)";
                instructionSheet.Cells["A14"].Value = "   - SellingPrice: Giá bán (số >= 0, VD: 50000, 75000)";

                instructionSheet.Cells["A16"].Value = "3. Các trường tùy chọn:";
                instructionSheet.Cells["A16"].Style.Font.Bold = true;
                instructionSheet.Cells["A17"].Value = "   - Description: Mô tả sản phẩm (có thể để trống)";

                instructionSheet.Cells["A19"].Value = "4. Các trường tự động:";
                instructionSheet.Cells["A19"].Style.Font.Bold = true;
                instructionSheet.Cells["A20"].Value = "   - ProductId: Tự động tạo bởi hệ thống";
                instructionSheet.Cells["A21"].Value = "   - ImageUrl: Mặc định null, có thể cập nhật sau";
                instructionSheet.Cells["A22"].Value = "   - IsAvailable: Mặc định true (sản phẩm khả dụng)";
                instructionSheet.Cells["A23"].Value = "   - CreatedAt, UpdatedAt: Tự động ghi nhận thời gian tạo/cập nhật";

                instructionSheet.Cells["A25"].Value = "5. Lưu ý quan trọng:";
                instructionSheet.Cells["A25"].Style.Font.Bold = true;
                instructionSheet.Cells["A26"].Value = "   - Không được cách dòng: Tất cả các dòng sản phẩm phải liền kề nhau (tính từ dòng 3 trở đi)";
                instructionSheet.Cells["A26"].Style.Font.Bold = true;
                instructionSheet.Cells["A26"].Style.Font.Color.SetColor(Color.Red);
                instructionSheet.Cells["A27"].Value = "   - Nếu có dòng trống giữa các sản phẩm, hệ thống sẽ dừng đọc và bỏ qua các sản phẩm phía sau";
                instructionSheet.Cells["A27"].Style.Font.Color.SetColor(Color.Red);
                instructionSheet.Cells["A28"].Value = "   - Tất cả sản phẩm phải hợp lệ mới được import";
                instructionSheet.Cells["A29"].Value = "   - Nếu có 1 sản phẩm lỗi, toàn bộ file sẽ bị hủy (rollback)";
                instructionSheet.Cells["A30"].Value = "   - Tên nhà cung cấp và danh mục phải chính xác (có phân biệt hoa thường)";
                instructionSheet.Cells["A31"].Value = "   - ProductCode và ProductName không được trùng với sản phẩm đã có";
                instructionSheet.Cells["A32"].Value = "   - Các dòng màu vàng là dữ liệu mẫu, có thể xóa và thay bằng dữ liệu thực";
                instructionSheet.Cells["A33"].Value = "   - File chỉ chấp nhận định dạng .xlsx hoặc .xls";
                instructionSheet.Cells["A34"].Value = "   - Kích thước file tối đa: 10MB";

                instructionSheet.Column(1).Width = 90;
                instructionSheet.Cells["A1:A34"].Style.WrapText = true;

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }
    }
}
