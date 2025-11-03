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
                // SHEET 1: THÔNG TIN CHUNG 
                var infoSheet = package.Workbook.Worksheets.Add("Thông tin chung");

                // Headers cho Sheet 1
                var infoHeaders = new[] { "WarehouseName", "SupplierName", "ExpireDate" };
                var infoDescriptions = new[]
                {
                    "Tên kho nhập (Bắt buộc)",
                    "Tên nhà cung cấp (Bắt buộc)",
                    "Ngày hết hạn chung (Bắt buộc, MM/DD/YYYY)"
                };

                // Tạo headers (Row 1)
                for (int i = 0; i < infoHeaders.Length; i++)
                {
                    var cell = infoSheet.Cells[1, i + 1];
                    cell.Value = infoHeaders[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo descriptions (Row 2)
                for (int i = 0; i < infoDescriptions.Length; i++)
                {
                    var cell = infoSheet.Cells[2, i + 1];
                    cell.Value = infoDescriptions[i];
                    cell.Style.Font.Italic = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 230, 241));
                    cell.Style.WrapText = true;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Sample data (Row 3)
                infoSheet.Cells[3, 1].Value = "Kho Hà Nội";
                infoSheet.Cells[3, 2].Value = "Nhà cung cấp ABC";
                infoSheet.Cells[3, 3].Value = "12-31-2025";
                for (int i = 1; i <= 3; i++)
                {
                    infoSheet.Cells[3, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    infoSheet.Cells[3, i].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    infoSheet.Cells[3, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
                infoSheet.Cells[1, 1, 3, 3].AutoFitColumns();

                // SHEET 2: DANH SÁCH SẢN PHẨM 
                var productSheet = package.Workbook.Worksheets.Add("Danh sách sản phẩm");

                // Define headers (chỉ còn Product info, không có Warehouse/Supplier)
                var headers = new[]
                {
                    "ProductName",
                    "Quantity",
                    "UnitPrice",
                    "Note"
                };

                // Define header descriptions
                var descriptions = new[]
                {
                    "Tên sản phẩm (Bắt buộc)",
                    "Số lượng (Bắt buộc, số nguyên > 0)",
                    "Giá nhập (Bắt buộc, số thực > 0)",
                    "Ghi chú (Tùy chọn)"
                };

                // Tạo headers (Row 1)
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = productSheet.Cells[1, i + 1];
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
                    var cell = productSheet.Cells[2, i + 1];
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
                    "Sản phẩm A",
                    1000,
                    50000.50,
                    "Lô hàng đầu tiên"
                };

                for (int i = 0; i < sampleData.Length; i++)
                {
                    var cell = productSheet.Cells[3, i + 1];
                    cell.Value = sampleData[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Tạo sample data (Row 4)
                var sampleData2 = new object[]
                {
                    "Sản phẩm B",
                    500,
                    75000.00,
                    "Lô hàng thứ hai"
                };

                for (int i = 0; i < sampleData2.Length; i++)
                {
                    var cell = productSheet.Cells[4, i + 1];
                    cell.Value = sampleData2[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                productSheet.Cells[1, 1, 4, headers.Length].AutoFitColumns();

                // Set độ rộng tối thiểu cho các cột
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (productSheet.Column(i).Width < 15)
                        productSheet.Column(i).Width = 15;
                }

                // Tạo sheet hướng dẫn
                var instructionSheet = package.Workbook.Worksheets.Add("Hướng Dẫn");
                instructionSheet.Cells["A1"].Value = "HƯỚNG DẪN IMPORT STOCK INPUT";
                instructionSheet.Cells["A1"].Style.Font.Bold = true;
                instructionSheet.Cells["A1"].Style.Font.Size = 16;
                instructionSheet.Cells["A3"].Value = "1. Cấu trúc file:";
                instructionSheet.Cells["A3"].Style.Font.Bold = true;
                instructionSheet.Cells["A4"].Value = "   - Dòng 1: Header (tên các cột)";
                instructionSheet.Cells["A5"].Value = "   - Dòng 2: Mô tả chi tiết (có thể xóa trước khi import)";
                instructionSheet.Cells["A6"].Value = "   - Từ dòng 3 trở đi: Dữ liệu thực tế (mỗi dòng là 1 sản phẩm)";
                instructionSheet.Cells["A8"].Value = "2. SHEET 1 - Thông tin chung (CHỈ 1 DÒNG):";
                instructionSheet.Cells["A8"].Style.Font.Bold = true;
                instructionSheet.Cells["A9"].Value = "   - WarehouseName: Tên kho nhập (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A10"].Value = "   - SupplierName: Tên nhà cung cấp (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A11"].Value = "   - ExpireDate: Ngày hết hạn CHUNG cho toàn bộ đơn (MM/DD/YYYY)";
                instructionSheet.Cells["A13"].Value = "3. SHEET 2 - Danh sách sản phẩm (NHIỀU DÒNG):";
                instructionSheet.Cells["A13"].Style.Font.Bold = true;
                instructionSheet.Cells["A14"].Value = "   - ProductName: Tên sản phẩm (phải tồn tại trong hệ thống)";
                instructionSheet.Cells["A15"].Value = "   - Quantity: Số lượng nhập (số nguyên > 0)";
                instructionSheet.Cells["A16"].Value = "   - UnitPrice: Giá nhập (số thực > 0, VD: 50000.50)";
                instructionSheet.Cells["A17"].Value = "   - Note: Ghi chú cho sản phẩm (tùy chọn)";
                instructionSheet.Cells["A19"].Value = "4. Lưu ý quan trọng:";
                instructionSheet.Cells["A19"].Style.Font.Bold = true;
                instructionSheet.Cells["A20"].Value = "   - PHẢI có đủ 2 sheets: 'Thông tin chung' và 'Danh sách sản phẩm'";
                instructionSheet.Cells["A21"].Value = "   - Sheet 'Thông tin chung' CHỈ CÓ 1 DÒNG DUY NHẤT (dòng 3)";
                instructionSheet.Cells["A22"].Value = "   - Sheet 'Danh sách sản phẩm' có thể có NHIỀU DÒNG (từ dòng 3 trở đi)";
                instructionSheet.Cells["A23"].Value = "   - Các dòng màu vàng là dữ liệu mẫu, có thể xóa và thay bằng dữ liệu thực";
                instructionSheet.Cells["A24"].Value = "   - File chỉ chấp nhận định dạng .xlsx hoặc .xls";
                instructionSheet.Cells["A25"].Value = "   - Kích thước file tối đa: 10MB";
                instructionSheet.Cells["A26"].Value = "   - Tên phải chính xác (WarehouseName, SupplierName, ProductName)";
                instructionSheet.Cells["A27"].Value = "   - Nếu xảy ra lỗi sẽ trả về file được nhập vào, các ô dữ liệu không hợp lệ sẽ bị bôi đỏ(Đang phát triển)";
                instructionSheet.Column(1).Width = 90;
                instructionSheet.Cells["A1:A26"].Style.WrapText = true;

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }
    }
}
