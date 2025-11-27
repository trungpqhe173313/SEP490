using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using NB.Service.TransactionService.ViewModels;
using System.Globalization;

namespace NB.Service.Core.Forms
{
    public static class TransactionPdfGenerator
    {
        static TransactionPdfGenerator()
        {
            // Set QuestPDF license to Community (free for open source and personal use)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Tạo file PDF cho phiếu giao dịch
        /// </summary>
        /// <param name="transaction">Thông tin giao dịch</param>
        /// <returns>Byte array chứa file PDF</returns>
        public static byte[] GenerateTransactionPdf(TransactionPrintVM transaction)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Thiết lập khổ giấy A4
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Header - Thông tin công ty
                    page.Header()
                        .Column(column =>
                        {
                            // Tên công ty
                            column.Item().AlignCenter().Text("CÔNG TY TNHH TM DV QUANG THÀNH")
                                .FontSize(14)
                                .Bold();

                            // Địa chỉ
                            column.Item().AlignCenter().Text("Đ/C: Khu 1 Bản Nguyên, Lâm Thao, Phú Thọ")
                                .FontSize(10);

                            // Điện thoại
                            column.Item().AlignCenter().Text("Điện thoại: 0326 130 139 - 0984 423 469")
                                .FontSize(10);

                            // Chủ tài khoản
                            column.Item().AlignCenter().Text("Chủ tài khoản: Công ty TNHH TM DV Quang Thành")
                                .FontSize(10);

                            // STK
                            column.Item().AlignCenter().Text("STK: 1077909999 - Vietcombank")
                                .FontSize(10);

                            // Khoảng cách
                            column.Item().PaddingTop(10);

                            // Tiêu đề phiếu - Tự động đổi theo Type
                            var title = transaction.Type?.Equals("Import", StringComparison.OrdinalIgnoreCase) == true
                                ? "PHIẾU NHẬP KHO"
                                : "PHIẾU XUẤT KHO";

                            column.Item().AlignCenter().Text(title)
                                .FontSize(16)
                                .Bold();

                            // Số HĐ
                            var soHD = !string.IsNullOrEmpty(transaction.TransactionCode)
                                ? transaction.TransactionCode
                                : $"HD{transaction.TransactionId:D6}";
                            column.Item().AlignCenter().Text($"Số HĐ: {soHD}")
                                .FontSize(10);

                            // Ngày tháng năm
                            column.Item().AlignCenter().Text(
                                $"Ngày {transaction.TransactionDate:dd} tháng {transaction.TransactionDate:MM} năm {transaction.TransactionDate:yyyy}")
                                .FontSize(10);
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(0.5f, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Thông tin khách hàng hoặc nhà cung cấp
                            column.Item().Element(c => ComposeCustomerOrSupplierInfo(c, transaction));

                            column.Item().PaddingTop(10);

                            // Bảng sản phẩm
                            column.Item().Element(c => ComposeProductTable(c, transaction));

                            column.Item().PaddingTop(10);

                            // Tổng cộng
                            column.Item().Element(c => ComposeTotalSection(c, transaction));
                        });

                    // Footer
                    page.Footer()
                        .Column(column =>
                        {
                            column.Item().AlignCenter().Text("Xin Cảm ơn Quý khách và Hẹn gặp lại!")
                                .FontSize(11)
                                .Italic();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposeCustomerOrSupplierInfo(IContainer container, TransactionPrintVM transaction)
        {
            container.Row(row =>
            {
                if (transaction.Type?.Equals("Import", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Import - Hiển thị nhà cung cấp
                    var supplierInfo = $"Nhà cung cấp: {transaction.SupplierName ?? "N/A"}";
                    var supplierPhone = !string.IsNullOrEmpty(transaction.SupplierPhone)
                        ? transaction.SupplierPhone
                        : "";

                    row.RelativeItem().Text(supplierInfo);
                    row.RelativeItem().AlignRight().Text($"SĐT: {supplierPhone}");
                }
                else
                {
                    // Export - Hiển thị khách hàng
                    var customerInfo = $"Khách hàng: {transaction.CustomerName ?? "N/A"}";
                    var customerPhone = !string.IsNullOrEmpty(transaction.CustomerPhone)
                        ? transaction.CustomerPhone
                        : "";

                    row.RelativeItem().Text(customerInfo);
                    row.RelativeItem().AlignRight().Text($"SĐT: {customerPhone}");
                }
            });
        }

        private static void ComposeProductTable(IContainer container, TransactionPrintVM transaction)
        {
            container.Table(table =>
            {
                // Định nghĩa các cột
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);      // TT
                    columns.RelativeColumn(3);       // Sản phẩm
                    columns.ConstantColumn(80);      // Đơn giá
                    columns.ConstantColumn(40);      // SL
                    columns.ConstantColumn(90);      // Thành tiền
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("TT").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Sản phẩm").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Đơn giá").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("SL").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Thành tiền").Bold();

                    static IContainer HeaderCellStyle(IContainer container)
                    {
                        return container
                            .Border(1)
                            .BorderColor(Colors.Black)
                            .Padding(5)
                            .AlignCenter()
                            .AlignMiddle();
                    }
                });

                // Rows - Dữ liệu sản phẩm
                if (transaction.ProductList != null && transaction.ProductList.Any())
                {
                    int stt = 1;
                    foreach (var item in transaction.ProductList)
                    {
                        var subtotal = item.Quantity * item.UnitPrice;

                        table.Cell().Element(CellStyle).Text(stt.ToString());
                        table.Cell().Element(CellStyleLeft).Text(item.ProductName ?? "");
                        table.Cell().Element(CellStyle).Text(item.UnitPrice.ToString("N0", new CultureInfo("vi-VN")));
                        table.Cell().Element(CellStyle).Text(item.Quantity.ToString("N0"));
                        table.Cell().Element(CellStyle).Text(subtotal.ToString("N0", new CultureInfo("vi-VN")));

                        stt++;
                    }
                }

                static IContainer CellStyle(IContainer container)
                {
                    return container
                        .Border(1)
                        .BorderColor(Colors.Black)
                        .Padding(5)
                        .AlignCenter()
                        .AlignMiddle();
                }

                static IContainer CellStyleLeft(IContainer container)
                {
                    return container
                        .Border(1)
                        .BorderColor(Colors.Black)
                        .Padding(5)
                        .AlignLeft()
                        .AlignMiddle();
                }
            });
        }

        private static void ComposeTotalSection(IContainer container, TransactionPrintVM transaction)
        {
            container.Column(column =>
            {
                // Tổng tiền hàng
                column.Item().Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(150).AlignRight().Text("Tổng tiền hàng:");
                    row.ConstantItem(150).AlignRight().Text(
                        (transaction.TotalCost ?? 0).ToString("N0", new CultureInfo("vi-VN")))
                        .Bold();
                });

                // Chiết khấu - Bỏ theo yêu cầu, nhưng giữ line để dễ maintain sau này
                // column.Item().Row(row =>
                // {
                //     row.RelativeItem();
                //     row.ConstantItem(150).AlignRight().Text("Chiết khấu:");
                //     row.ConstantItem(150).AlignRight().Text("0");
                // });

                // Tổng thanh toán
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(150).AlignRight().Text("Tổng thanh toán:")
                        .Bold().FontSize(12);
                    row.ConstantItem(150).AlignRight().Text(
                        (transaction.TotalCost ?? 0).ToString("N0", new CultureInfo("vi-VN")))
                        .Bold().FontSize(12);
                });
            });
        }
    }
}
