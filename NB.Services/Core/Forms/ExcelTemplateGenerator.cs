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
                var worksheet = package.Workbook.Worksheets.Add("Stock Input Template");

                // Define headers
                var headers = new[]
                {
                    "WarehouseId",
                    "ProductId",
                    "Quantity",
                    "BatchCode",
                    "ExpireDate",
                    "TransactionId",
                    "Note"
                };

                // Define header descriptions 
                var descriptions = new[]
                {
                    "ID Kho (Bắt buộc, số nguyên)",
                    "ID Sản phẩm (Bắt buộc, số nguyên)",
                    "Số lượng (Bắt buộc, số nguyên > 0)",
                    "Mã lô (Bắt buộc, sẽ tự động thêm số thứ tự)",
                    "Ngày hết hạn (Bắt buộc)",
                    "ID Đơn nhập (Bắt buộc, số nguyên)",
                    "Ghi chú (Tùy chọn)"
                };

                // Tạo headers (Row 1)
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

                // Taok descriptions (Row 2)
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
                    1,
                    101,
                    1000,
                    "BATCH001",
                    "2025-12-31",
                    1,
                    "Lô hàng đầu tiên"
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
                    1,
                    102,
                    500,
                    "BATCH001",
                    "2025-12-31",
                    1,
                    "Lô hàng thứ hai"
                };

                for (int i = 0; i < sampleData2.Length; i++)
                {
                    var cell = worksheet.Cells[4, i + 1];
                    cell.Value = sampleData2[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Set format ngày tháng (column 5)
                worksheet.Column(5).Style.Numberformat.Format = "yyyy-MM-dd";

                // Auto-fit columns
                worksheet.Cells[1, 1, 4, headers.Length].AutoFitColumns();

                // Set độ rộng tối thiểu cho các cột
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (worksheet.Column(i).Width < 15)
                        worksheet.Column(i).Width = 15;
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

                instructionSheet.Cells["A8"].Value = "2. Các trường bắt buộc:";
                instructionSheet.Cells["A8"].Style.Font.Bold = true;
                instructionSheet.Cells["A9"].Value = "   - WarehouseId: ID của kho (số nguyên)";
                instructionSheet.Cells["A10"].Value = "   - ProductId: ID của sản phẩm (số nguyên)";
                instructionSheet.Cells["A11"].Value = "   - Quantity: Số lượng nhập (số nguyên > 0)";
                instructionSheet.Cells["A12"].Value = "   - BatchCode: Mã lô (hệ thống sẽ tự động thêm số thứ tự 0001, 0002...)";
                instructionSheet.Cells["A13"].Value = "   - ExpireDate: Ngày hết hạn (phải sau ngày hiện tại, định dạng yyyy-MM-dd)";
                instructionSheet.Cells["A14"].Value = "   - TransactionId: ID của đơn nhập hàng (nếu có)";

                instructionSheet.Cells["A16"].Value = "3. Các trường tùy chọn:";
                instructionSheet.Cells["A17"].Style.Font.Bold = true;

                instructionSheet.Cells["A18"].Value = "   - Note: Ghi chú";

                instructionSheet.Cells["A20"].Value = "4. Quy tắc đặc biệt:";
                instructionSheet.Cells["A21"].Style.Font.Bold = true;
                instructionSheet.Cells["A22"].Value = "   - BatchCode: Nếu có nhiều dòng cùng BatchCode, hệ thống sẽ tự động thêm";
                instructionSheet.Cells["A23"].Value = "     số thứ tự (VD: BATCH001 -> BATCH0010001, BATCH0010002, BATCH0010003)";
                instructionSheet.Cells["A24"].Value = "   - Mỗi dòng tương đương 1 sản phẩm trong đơn nhập";
                instructionSheet.Cells["A25"].Value = "   - Hệ thống sẽ tự động cập nhật Inventory (tồn kho) khi import thành công";

                instructionSheet.Cells["A27"].Value = "5. Lưu ý:";
                instructionSheet.Cells["A28"].Style.Font.Bold = true;
                instructionSheet.Cells["A29"].Value = "   - ImportDate, BatchId, Status, IsActive sẽ được tự động tạo bởi hệ thống";
                instructionSheet.Cells["A30"].Value = "   - Các dòng màu vàng là dữ liệu mẫu, bạn có thể xóa và thay bằng dữ liệu thực";
                instructionSheet.Cells["A31"].Value = "   - Đảm bảo đúng thứ tự các cột như trong template";
                instructionSheet.Cells["A32"].Value = "   - File chỉ chấp nhận định dạng .xlsx hoặc .xls";
                instructionSheet.Cells["A33"].Value = "   - Kích thước file tối đa: 10MB";
                instructionSheet.Cells["A34"].Value = "   - WarehouseId và ProductId phải tồn tại trong hệ thống";
                instructionSheet.Cells["A35"].Value = "   - Mã lô không được trùng với mã lô đã tồn tại trong hệ thống";

                instructionSheet.Column(1).Width = 85;
                instructionSheet.Cells["A1:A36"].Style.WrapText = true;

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }
    }
}
