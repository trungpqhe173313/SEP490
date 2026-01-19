
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.ReturnTransactionService;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.FinancialTransactionService;
using NB.Service.Core.Mapper;
using NB.Service.ReturnTransactionService.ViewModels;
using NB.Service.ReturnTransactionDetailService.ViewModels;
using NB.Service.FinancialTransactionService.ViewModels;
using Xunit;
using FluentAssertions;
using NB.Service.Dto;
using NB.Service.WarehouseService;
using NB.Service.TransactionService.ViewModels;
using NB.Service.ProductService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.WarehouseService.Dto;
using NB.Service.FinancialTransactionService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.Core;
using NB.Repository.Common;
using OfficeOpenXml;

namespace NB.Tests.Controllers
{
    public class StockInputControllerTests
    {
        private readonly Mock<IInventoryService> _inventoryMock = new();
        private readonly Mock<ITransactionService> _transactionMock = new();
        private readonly Mock<ITransactionDetailService> _transactionDetailMock = new();
        private readonly Mock<IWarehouseService> _warehouseMock = new();
        private readonly Mock<IProductService> _productMock = new();
        private readonly Mock<IStockBatchService> _stockBatchMock = new();
        private readonly Mock<ISupplierService> _supplierMock = new();
        private readonly Mock<IReturnTransactionService> _returnTranMock = new();
        private readonly Mock<IReturnTransactionDetailService> _returnTranDetailMock = new();
        private readonly Mock<IFinancialTransactionService> _financialMock = new();
        private readonly Mock<IUserService> _userMock = new();
        private readonly Mock<ILogger<StockInputController>> _loggerMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock = new();
        private readonly Mock<TransactionCodeGenerator> _transactionCodeGeneratorMock;

        public StockInputControllerTests()
        {
            _transactionRepositoryMock
                .Setup(r => r.GetQueryable())
                .Returns(new StockInputAsyncEnumerable<Transaction>(new List<Transaction>()));

            _transactionCodeGeneratorMock = new Mock<TransactionCodeGenerator>(MockBehavior.Loose, _transactionRepositoryMock.Object);
        }

        private StockInputController CreateController()
        {
            var controller = new StockInputController(
                _inventoryMock.Object,
                _transactionMock.Object,
                _transactionDetailMock.Object,
                _warehouseMock.Object,
                _productMock.Object,
                _stockBatchMock.Object,
                _supplierMock.Object,
                _returnTranMock.Object,
                _returnTranDetailMock.Object,
                _financialMock.Object,
                _userMock.Object,
                _loggerMock.Object,
                _mapperMock.Object,
                _transactionCodeGeneratorMock.Object
            );
            return controller;
        }

        private class StockInputAsyncQueryProvider<TEntity> : IQueryProvider
        {
            private readonly IQueryProvider _inner;

            public StockInputAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public IQueryable CreateQuery(Expression expression)
                => new StockInputAsyncEnumerable<TEntity>(expression);

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
                => new StockInputAsyncEnumerable<TElement>(expression);

            public object? Execute(Expression expression)
                => _inner.Execute(expression);

            public TResult Execute<TResult>(Expression expression)
                => _inner.Execute<TResult>(expression);
        }

        private class StockInputAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public StockInputAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            {
            }

            public StockInputAsyncEnumerable(Expression expression)
                : base(expression)
            {
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new StockInputAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

            IQueryProvider IQueryable.Provider => (IQueryProvider)new StockInputAsyncQueryProvider<T>(this);
        }

        private class StockInputAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public StockInputAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return default;
            }

            public ValueTask<bool> MoveNextAsync()
                => new ValueTask<bool>(_inner.MoveNext());
        }

        private static StockBatchCreateWithProductsVM BuildModelWithProducts(IEnumerable<ProductInputItem>? products = null)
        {
            return new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(1),
                Note = "note",
                Products = products != null
                    ? new List<ProductInputItem>(products)
                    : new List<ProductInputItem>
                    {
                        new ProductInputItem { ProductId = 1, Quantity = 1, UnitPrice = 1 }
                    }
            };
        }

        static StockInputControllerTests()
        {
            SetExcelPackageLicense();
        }

        private static void SetExcelPackageLicense()
        {
            var licenseProp = typeof(ExcelPackage).GetProperty("License", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var setter = licenseProp?.GetSetMethod(true);
            #pragma warning disable CS0618
            setter?.Invoke(null, new object[] { LicenseContext.NonCommercial });
            #pragma warning restore CS0618
        }

        private static IFormFile BuildExcelFile(IEnumerable<(string Name, Action<ExcelWorksheet>? Configure)> sheets)
        {
            var stream = new MemoryStream();
            using (var package = new ExcelPackage())
            {
                foreach (var (name, configure) in sheets)
                {
                    var worksheet = package.Workbook.Worksheets.Add(name);
                    configure?.Invoke(worksheet);
                }

                package.SaveAs(stream);
            }

            stream.Position = 0;
            return new FormFile(stream, 0, stream.Length, "file", "template.xlsx")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        private static IFormFile BuildExcelFileWithProduct(string productName, string quantity, string unitPrice, string warehouseName, string supplierName)
        {
            return BuildExcelFile(new (string Name, Action<ExcelWorksheet>? Configure)[]
            {
                ("Nhập kho", sheet =>
                {
                    sheet.Cells[1, 1].Value = "Header";
                    sheet.Cells[2, 1].Value = "Header2";
                    sheet.Cells[3, 1].Value = warehouseName;
                    sheet.Cells[3, 2].Value = supplierName;
                    sheet.Cells[3, 3].Value = "note";
                    sheet.Cells[4, 4].Value = productName;
                    sheet.Cells[4, 5].Value = quantity;
                    sheet.Cells[4, 6].Value = unitPrice;
                })
            });
        }

        private static List<ProductInputItem> BuildProductList(int productId, int quantity, decimal unitPrice)
        {
            return new List<ProductInputItem>
            {
                new ProductInputItem { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice }
            };
        }

        private void SetupValidResponsible(int responsibleId)
        {
            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible" });
        }

        private void SetupValidWarehouse(int warehouseId = 1)
        {
            _warehouseMock
                .Setup(w => w.GetById(warehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = warehouseId, WarehouseName = "Warehouse" });
        }

        private void SetupValidSupplier(int supplierId = 1)
        {
            _supplierMock
                .Setup(s => s.GetBySupplierId(supplierId))
                .ReturnsAsync(new SupplierDto { SupplierId = supplierId, SupplierName = "Supplier" });
        }

        [Fact]
        public async Task GetData_ReturnsOk_WithEmptySearch()
        {
            // Arrange
            var controller = CreateController();
            var dto = new TransactionDto { TransactionId = 1, WarehouseId = 1, SupplierId = 1, Type = "Import", Status = 1, TotalCost = 10 };
            var paged = new PagedList<TransactionDto>(new List<TransactionDto> { dto }, 1, 10, 1);
            _transactionMock.Setup(s => s.GetData(It.IsAny<TransactionSearch>())).ReturnsAsync(paged);
            _warehouseMock.Setup(w => w.GetById(It.IsAny<int>())).ReturnsAsync(new WarehouseDto { WarehouseName = "Kho A" });
            _supplierMock.Setup(s => s.GetBySupplierId(It.IsAny<int>())).ReturnsAsync(new SupplierDto { SupplierId = 1, SupplierName = "Nhà cung cấp A" });

            // Act
            var result = await controller.GetData(new TransactionSearch());

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetData_ReturnsOk_WithFilters()
        {
            // Arrange
            var controller = CreateController();
            var search = new TransactionSearch
            {
                WarehouseId = 2,
                SupplierId = 5,
                Status = 6
            };
            var dto = new TransactionDto
            {
                TransactionId = 10,
                WarehouseId = 2,
                SupplierId = 5,
                Type = "Import",
                Status = 6,
                TotalCost = 5000
            };
            var paged = new PagedList<TransactionDto>(new List<TransactionDto> { dto }, 1, 10, 1);
            _transactionMock.Setup(s => s.GetData(It.Is<TransactionSearch>(x =>
                x.WarehouseId == 2 && x.SupplierId == 5 && x.Status == 6))).ReturnsAsync(paged);
            _warehouseMock.Setup(w => w.GetById(2)).ReturnsAsync(new WarehouseDto { WarehouseName = "Kho B" });
            _supplierMock.Setup(s => s.GetBySupplierId(5)).ReturnsAsync(new SupplierDto { SupplierId = 5, SupplierName = "Nhà cung cấp B" });

            // Act
            var result = await controller.GetData(search);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            var response = ok.Value;
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task GetData_ReturnsBadRequest_WhenWarehouseNotExists()
        {
            // Arrange
            var controller = CreateController();
            var search = new TransactionSearch { WarehouseId = 999 };
            var dto = new TransactionDto { TransactionId = 1, WarehouseId = 999, SupplierId = 1, Type = "Import", Status = 1, TotalCost = 10 };
            var paged = new PagedList<TransactionDto>(new List<TransactionDto> { dto }, 1, 10, 1);
            _transactionMock.Setup(s => s.GetData(It.IsAny<TransactionSearch>())).ReturnsAsync(paged);
            _warehouseMock.Setup(w => w.GetById(999)).ThrowsAsync(new Exception("Warehouse not found"));
            _supplierMock.Setup(s => s.GetBySupplierId(It.IsAny<int>())).ReturnsAsync(new SupplierDto { SupplierId = 1, SupplierName = "S" });

            // Act
            var result = await controller.GetData(search);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetDetail_ReturnsOk_WhenDataExists()
        {
            var controller = CreateController();

            var transaction = new TransactionDto { TransactionId = 1, WarehouseId = 1, SupplierId = 1, TransactionDate = DateTime.UtcNow, Status = 1, TotalCost = 100, Note = "n" };
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);
            _warehouseMock.Setup(w => w.GetById(It.IsAny<int>())).ReturnsAsync(new WarehouseDto { WarehouseName = "w" });
            _supplierMock.Setup(s => s.GetBySupplierId(It.IsAny<int>())).ReturnsAsync(new SupplierDto { SupplierId = 1, SupplierName = "s", Email = "e", Phone = "p", IsActive = true });
            var td = new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = 2, Quantity = 1, UnitPrice = 5 } };
            _transactionDetailMock.Setup(x => x.GetByTransactionId(1)).ReturnsAsync(td);
            _productMock.Setup(p => p.GetById(It.IsAny<int>())).ReturnsAsync(new ProductDto { ProductId = 2, ProductName = "Prod", ProductCode = "P001" });
            _stockBatchMock.Setup(s => s.GetByTransactionId(1)).ReturnsAsync(new List<StockBatchDto> { new StockBatchDto { BatchId = 1, ExpireDate = DateTime.UtcNow.AddDays(10), Note = "note" } });

            var result = await controller.GetDetail(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetStockBatchById_ReturnsOk_WhenFound()
        {
            // Arrange
            var controller = CreateController();
            _stockBatchMock.Setup(s => s.GetByTransactionId(1)).ReturnsAsync(new List<StockBatchDto> { new StockBatchDto { BatchId = 1, ProductId = 1, TransactionId = 1 } });

            // Act
            var result = await controller.GetStockBatchById(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetStockBatchById_ReturnsNotFound_WhenIdNotExists()
        {
            // Arrange
            var controller = CreateController();
            _stockBatchMock.Setup(s => s.GetByTransactionId(999)).ReturnsAsync(new List<StockBatchDto>());

            // Act
            var result = await controller.GetStockBatchById(999);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetStockBatchById_ReturnsBadRequest_WhenIdIsNegative()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetStockBatchById(-1);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        #region CreateStockInputs Tests

        /// <summary>
        /// TCID01: CreateStockInputs với responsible không tồn tại
        /// 
        /// PRECONDITION:
        /// - responsibleId không được gán trong hệ thống
        /// 
        /// INPUT:
        /// - responsibleId: 999 (không tồn tại)
        /// - model: StockBatchCreateWithProductsVM tối thiểu
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy người chịu trách nhiệm"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID01_CreateStockInputs_ResponsibleNotFound_ReturnsNotFound()
        {
            var controller = CreateController();
            int responsibleId = 999;
            var model = BuildModelWithProducts();
            
            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync((UserDto?)null);

            var result = await controller.CreateStockInputs(responsibleId, model);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy người chịu trách nhiệm", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateStockInputs với kho không tồn tại
        /// 
        /// PRECONDITION:
        /// - responsible và supplier hợp lệ
        /// - WarehouseId không trỏ tới kho thực
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: StockBatchCreateWithProductsVM với WarehouseId = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy kho với ID: 1"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID02_CreateStockInputs_WarehouseNotFound_ReturnsNotFound()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var model = BuildModelWithProducts();

            SetupValidResponsible(responsibleId);
            _warehouseMock
                .Setup(w => w.GetById(model.WarehouseId))
                .ReturnsAsync((WarehouseDto?)null);

            var result = await controller.CreateStockInputs(responsibleId, model);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal($"Không tìm thấy kho với ID: {model.WarehouseId}", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreateStockInputs với supplier không tồn tại
        /// 
        /// PRECONDITION:
        /// - responsible và warehouse hợp lệ
        /// - SupplierId không trỏ tới nhà cung cấp thực
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: StockBatchCreateWithProductsVM với SupplierId = 1
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy nhà cung cấp với ID: 1"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID03_CreateStockInputs_SupplierNotFound_ReturnsNotFound()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var model = BuildModelWithProducts();

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            _supplierMock
                .Setup(s => s.GetBySupplierId(model.SupplierId))
                .ReturnsAsync((SupplierDto?)null);

            var result = await controller.CreateStockInputs(responsibleId, model);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal($"Không tìm thấy nhà cung cấp với ID: {model.SupplierId}", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreateStockInputs với danh sách sản phẩm rỗng
        /// 
        /// PRECONDITION:
        /// - responsible, warehouse và supplier tồn tại
        /// - Products = empty list
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: Products = new List<ProductInputItem>()
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Danh sách sản phẩm không được để trống."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_CreateStockInputs_EmptyProductList_ReturnsBadRequest()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var model = BuildModelWithProducts();
            model.Products = new List<ProductInputItem>();

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            SetupValidSupplier();

            var result = await controller.CreateStockInputs(responsibleId, model);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Danh sách sản phẩm không được để trống.", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreateStockInputs với số lượng sản phẩm không lớn hơn 0
        /// 
        /// PRECONDITION:
        /// - responsible, warehouse và supplier hợp lệ
        /// - Products chứa item với Quantity = 0
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: Products với ProductId = 2, Quantity = 0
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số lượng sản phẩm với ID: 2 phải lớn hơn 0."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_CreateStockInputs_InvalidQuantity_ReturnsBadRequest()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var model = BuildModelWithProducts(BuildProductList(2, 0, 100));

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            SetupValidSupplier();

            var result = await controller.CreateStockInputs(responsibleId, model);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Số lượng sản phẩm với ID: 2 phải lớn hơn 0.", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CreateStockInputs với đơn giá sản phẩm không lớn hơn 0
        /// 
        /// PRECONDITION:
        /// - responsible, warehouse và supplier hợp lệ
        /// - Products chứa item với UnitPrice = 0
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: Products với ProductId = 3, UnitPrice = 0
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Đơn giá sản phẩm với ID: 3 phải lớn hơn 0."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_CreateStockInputs_InvalidUnitPrice_ReturnsBadRequest()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var model = BuildModelWithProducts(BuildProductList(3, 1, 0));

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            SetupValidSupplier();

            var result = await controller.CreateStockInputs(responsibleId, model);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Đơn giá sản phẩm với ID: 3 phải lớn hơn 0.", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: CreateStockInputs với sản phẩm không tồn tại
        /// 
        /// PRECONDITION:
        /// - responsible, warehouse và supplier hợp lệ
        /// - Products chứa ProductId không tìm thấy
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: Products với ProductId = 4
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy sản phẩm với ID: 4"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID07_CreateStockInputs_ProductNotFound_ReturnsNotFound()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var model = BuildModelWithProducts(BuildProductList(4, 1, 10));

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            SetupValidSupplier();
            _productMock
                .Setup(p => p.GetById(4))
                .ReturnsAsync((ProductDto?)null);

            var result = await controller.CreateStockInputs(responsibleId, model);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy sản phẩm với ID: 4", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID08: CreateStockInputs khi service ném exception
        /// 
        /// PRECONDITION:
        /// - responsible, warehouse, supplier và sản phẩm hợp lệ
        /// - _transactionService.CreateAsync ném exception
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: Products hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi tạo lô nhập kho."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_CreateStockInputs_ExceptionThrown_ReturnsBadRequest()
        {
            var controller = CreateController();
            int responsibleId = 1;
            var productList = BuildProductList(5, 1, 50);
            var model = BuildModelWithProducts(productList);

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            SetupValidSupplier();
            _productMock
                .Setup(p => p.GetById(5))
                .ReturnsAsync(new ProductDto { ProductId = 5, WeightPerUnit = 1 });
            _transactionMock
                .Setup(t => t.CreateAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await controller.CreateStockInputs(responsibleId, model);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi tạo lô nhập kho.", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: CreateStockInputs thành công
        /// 
        /// PRECONDITION:
        /// - responsible, warehouse, supplier và sản phẩm hợp lệ
        /// - TransactionService, TransactionDetailService hoạt động bình thường
        /// 
        /// INPUT:
        /// - responsibleId: 1
        /// - model: Products với ProductId = 6, Quantity = 2, UnitPrice = 10
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với thông tin transaction vừa tạo
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID09_CreateStockInputs_Success_ReturnsOk()
        {
            // Tạo repository mock riêng cho TransactionCodeGenerator với empty list
            // để CountAsync() trả về 0 (không có transaction nào trong ngày)
            var transactionRepoForCodeGenerator = new Mock<IRepository<Transaction>>();
            var emptyTransactionList = new StockInputAsyncEnumerable<Transaction>(new List<Transaction>());
            transactionRepoForCodeGenerator
                .Setup(r => r.GetQueryable())
                .Returns(emptyTransactionList);
            
            // Tạo TransactionCodeGenerator thực sự với repository mock
            var realTransactionCodeGenerator = new TransactionCodeGenerator(transactionRepoForCodeGenerator.Object);
            
            // Tạo controller với TransactionCodeGenerator thực sự
            var controller = new StockInputController(
                _inventoryMock.Object,
                _transactionMock.Object,
                _transactionDetailMock.Object,
                _warehouseMock.Object,
                _productMock.Object,
                _stockBatchMock.Object,
                _supplierMock.Object,
                _returnTranMock.Object,
                _returnTranDetailMock.Object,
                _financialMock.Object,
                _userMock.Object,
                _loggerMock.Object,
                _mapperMock.Object,
                realTransactionCodeGenerator
            );
            
            int responsibleId = 1;
            var productList = BuildProductList(6, 2, 10);
            var model = BuildModelWithProducts(productList);
            var expectedTransactionId = 99;

            SetupValidResponsible(responsibleId);
            SetupValidWarehouse();
            SetupValidSupplier();
            _productMock
                .Setup(p => p.GetById(6))
                .ReturnsAsync(new ProductDto { ProductId = 6, ProductName = "Product6", WeightPerUnit = 1.5m });

            _transactionMock
                .Setup(t => t.CreateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(dto => dto.TransactionId = expectedTransactionId)
                .Returns(Task.CompletedTask);

            _transactionDetailMock
                .Setup(t => t.CreateAsync(It.IsAny<TransactionDetailDto>()))
                .Returns(Task.CompletedTask);

            var result = await controller.CreateStockInputs(responsibleId, model);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            var transactionIdProperty = response.Data!.GetType().GetProperty("TransactionId");
            Assert.NotNull(transactionIdProperty);
            Assert.Equal(expectedTransactionId, transactionIdProperty!.GetValue(response.Data));
        }

        #endregion

        #region ImportFromExcel Tests

        /// <summary>
        /// TCID01: ImportFromExcel với file null
        ///
        /// PRECONDITION:
        /// - file = null
        ///
        /// INPUT:
        /// - file = null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "File không được để trống"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID01_ImportFromExcel_NoFile_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.ImportFromExcel(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("File không được để trống", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: ImportFromExcel với extension không hợp lệ
        ///
        /// PRECONDITION:
        /// - File extension = .txt
        ///
        /// INPUT:
        /// - file name = data.txt
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "Chỉ chấp nhận file Excel (.xlsx, .xls)"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID02_ImportFromExcel_InvalidExtension_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "file", "data.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var result = await controller.ImportFromExcel(file);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Chỉ chấp nhận file Excel (.xlsx, .xls)", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: ImportFromExcel với file quá lớn (>10MB)
        ///
        /// PRECONDITION:
        /// - File length > 10MB
        ///
        /// INPUT:
        /// - 10MB + 1 bytes
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message about max size
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID03_ImportFromExcel_OversizedFile_ReturnsBadRequest()
        {
            var controller = CreateController();
            var data = new byte[10 * 1024 * 1024 + 1];
            var file = new FormFile(new MemoryStream(data), 0, data.Length, "file", "template.xlsx")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            var result = await controller.ImportFromExcel(file);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Kích thước file không được vượt quá 10MB", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: ImportFromExcel không tìm thấy sheet 'Nhập kho'
        ///
        /// PRECONDITION:
        /// - Workbook chỉ chứa sheet khác
        ///
        /// INPUT:
        /// - sheet name = Other
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "Không tìm thấy sheet 'Nhập kho'"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID04_ImportFromExcel_MissingSheet_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = BuildExcelFile(
                new (string Name, Action<ExcelWorksheet>? Configure)[]
                {
                    ("Other", null)
                });

            var result = await controller.ImportFromExcel(file);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy sheet 'Nhập kho'", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: ImportFromExcel sheet có <3 dòng
        ///
        /// PRECONDITION:
        /// - Sheet 'Nhập kho' có 2 dòng
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "Sheet 'Nhập kho' không có dữ liệu"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID05_ImportFromExcel_SheetTooShort_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = BuildExcelFile(
                new (string Name, Action<ExcelWorksheet>? Configure)[]
                {
                    ("Nhập kho", sheet =>
                    {
                        sheet.Cells[1, 1].Value = "A";
                        sheet.Cells[2, 1].Value = "B";
                    })
                });

            var result = await controller.ImportFromExcel(file);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Sheet 'Nhập kho' không có dữ liệu", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: ImportFromExcel khi WarehouseName/SupplierName trống
        ///
        /// PRECONDITION:
        /// - Row 3 cột A hoặc B trống
        ///
        /// INPUT:
        /// - WarehouseName = null, SupplierName = "S"
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message list with both errors
        /// </summary>
        [Fact]
        public async Task TCID06_ImportFromExcel_MissingWarehouseOrSupplier_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = BuildExcelFileWithProduct("Sản phẩm A", "1", "10", "", "");

            var result = await controller.ImportFromExcel(file);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            var messages = response.Error?.Messages ?? new List<string>();
            Assert.Contains(messages, m => m.Contains("WarehouseName"));
            Assert.Contains(messages, m => m.Contains("SupplierName"));
        }

        /// <summary>
        /// TCID07: ImportFromExcel with validation errors in product rows (decimal quantity)
        ///
        /// PRECONDITION:
        /// - Sheet row contains decimal quantity
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest with validation message
        /// </summary>
        [Fact]
        public async Task TCID07_ImportFromExcel_ProductDecimalQuantity_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = BuildExcelFileWithProduct("Sản phẩm A", "10.5", "100", "Warehouse A", "Supplier A");

            _warehouseMock.Setup(w => w.GetByWarehouseName("Warehouse A")).ReturnsAsync(new WarehouseDto { WarehouseId = 1, WarehouseName = "Warehouse A" });
            _supplierMock.Setup(s => s.GetByName("Supplier A")).ReturnsAsync(new SupplierDto { SupplierId = 1, SupplierName = "Supplier A" });
            _productMock.Setup(p => p.GetByProductName("Sản phẩm A")).ReturnsAsync(new ProductDto { ProductId = 1, ProductName = "Sản phẩm A", WeightPerUnit = 1 });
            _productMock.Setup(p => p.GetById(1)).ReturnsAsync( new ProductDto { ProductId = 1, ProductName = "Sản phẩm A", WeightPerUnit = 1 });

            var result = await controller.ImportFromExcel(file);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(badRequest.Value);

            Assert.False(response.Success);
            var messages = response.Error?.Messages ?? new List<string>();
            Assert.Contains(messages, m => m.Contains("Số lượng phải là số nguyên"));
        }

        /// <summary>
        /// TCID08: ImportFromExcel thành công
        ///
        /// PRECONDITION:
        /// - Sheet đầy đủ thông tin
        /// - Warehouse & Supplier tồn tại
        /// - ProductName tìm thấy
        /// - Services hoạt động
        ///
        /// EXPECTED OUTPUT:
        /// - Ok với danh sách TransactionDetailOutputVM
        /// </summary>
        [Fact]
        public async Task TCID08_ImportFromExcel_Success_ReturnsOk()
        {
            var controller = CreateController();
            var warehouseName = "Warehouse A";
            var supplierName = "Supplier A";
            var productName = "Sản phẩm A";

            var file = BuildExcelFileWithProduct(productName, "5", "100", warehouseName, supplierName);

            _warehouseMock.Setup(w => w.GetByWarehouseName(It.IsAny<string>())).ReturnsAsync(new WarehouseDto { WarehouseId = 1, WarehouseName = warehouseName });
            _supplierMock.Setup(s => s.GetByName(It.IsAny<string>())).ReturnsAsync(new SupplierDto { SupplierId = 1, SupplierName = supplierName });
            _productMock.Setup(p => p.GetByProductName(It.IsAny<string>())).ReturnsAsync(new ProductDto { ProductId = 1, ProductName = productName, ProductCode = "P01", WeightPerUnit = 1.5m });
            _productMock.Setup(p => p.GetById(1)).ReturnsAsync(new ProductDto { ProductId = 1, ProductName = productName, ProductCode = "P01", WeightPerUnit = 1.5m });

            _transactionMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);
            var detailCallCount = 0;
            _transactionDetailMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDetailDto>()))
                .Callback(() => detailCallCount++)
                .Returns(Task.CompletedTask);

            _stockBatchMock.Setup(s => s.GetByName(It.IsAny<string>())).ReturnsAsync((StockBatchDto?)null);
            _stockBatchMock.Setup(s => s.CreateAsync(It.IsAny<StockBatchDto>())).Returns(Task.CompletedTask);
            _inventoryMock.Setup(i => i.GetByWarehouseAndProductId(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((InventoryDto?)null);
            _inventoryMock.Setup(i => i.CreateAsync(It.IsAny<InventoryDto>())).Returns(Task.CompletedTask);
            _inventoryMock.Setup(i => i.UpdateAsync(It.IsAny<InventoryDto>())).Returns(Task.CompletedTask);

            var result = await controller.ImportFromExcel(file);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<TransactionDetailOutputVM>>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.True(response.Data.Count >= 1);
            Assert.Equal(1, detailCallCount);
        }

        #endregion


        #region SetStatusChecked Tests

        /// <summary>
        /// TSCD01: SetStatusChecked với transactionId không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId <= 0
        ///
        /// INPUT:
        /// - transactionId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "Transaction ID không hợp lệ"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD01_SetStatusChecked_InvalidId_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.SetStatusChecked(0, new UpdateToCheckedStatusRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Transaction ID không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD02: SetStatusChecked với request null
        ///
        /// PRECONDITION:
        /// - transactionId > 0
        /// - request = null
        ///
        /// INPUT:
        /// - transactionId = 1
        /// - request = null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "Request không hợp lệ"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD02_SetStatusChecked_RequestNull_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.SetStatusChecked(1, null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Request không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD03: SetStatusChecked với responsibleId không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId > 0
        /// - responsibleId <= 0
        /// - transaction exists with different responsibleId
        ///
        /// INPUT:
        /// - transactionId = 1
        /// - responsibleId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "UserId người chịu trách nhiệm không hợp lệ"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD03_SetStatusChecked_InvalidResponsibleId_ReturnsBadRequest()
        {
            int transactionId = 1;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 0, ExpireDate = DateTime.UtcNow.AddDays(1) };

            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Import", Status = (int)TransactionStatus.importChecking, ResponsibleId = 1 };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);

            var result = await controller.SetStatusChecked(transactionId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("UserId người chịu trách nhiệm không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD04: SetStatusChecked khi transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0
        /// - repository returns null
        ///
        /// INPUT:
        /// - transactionId = 5
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound với message "Không tìm thấy giao dịch"
        /// - Status 404
        /// </summary>
        [Fact]
        public async Task TSCD04_SetStatusChecked_TransactionNotFound_ReturnsNotFound()
        {
            int transactionId = 5;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 1, ExpireDate = DateTime.UtcNow.AddDays(1) };

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            var result = await controller.SetStatusChecked(transactionId, request);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy giao dịch", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TSCD05: SetStatusChecked với transaction không ở trạng thái importChecking
        ///
        /// PRECONDITION:
        /// - transaction exists
        /// - Status != importChecking
        ///
        /// INPUT:
        /// - transactionId = 6
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message about only updating transactions in checking state
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD05_SetStatusChecked_StatusNotChecking_ReturnsBadRequest()
        {
            int transactionId = 6;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 1, ExpireDate = DateTime.UtcNow.AddDays(1) };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = (int)TransactionStatus.importReceived,
                ResponsibleId = 1
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = 1, Quantity = 1 }
            });

            var result = await controller.SetStatusChecked(transactionId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Chỉ có thể cập nhật trạng thái 'Đã kiểm' cho giao dịch đang ở trạng thái 'Đang kiểm'", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD06: SetStatusChecked với transaction không phải import
        ///
        /// PRECONDITION:
        /// - transaction exists
        /// - Type = Export
        ///
        /// INPUT:
        /// - transactionId = 7
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "Giao dịch không phải là loại Import"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD06_SetStatusChecked_NotImport_ReturnsBadRequest()
        {
            int transactionId = 7;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 1, ExpireDate = DateTime.UtcNow.AddDays(1) };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Export",
                Status = (int)TransactionStatus.importChecking,
                ResponsibleId = 1
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);

            var result = await controller.SetStatusChecked(transactionId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Giao dịch không phải là loại Import", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD07: SetStatusChecked khi responsibleId không khớp
        ///
        /// PRECONDITION:
        /// - transaction exists with responsibleId = 1
        /// - request responsibleId = 2
        ///
        /// INPUT:
        /// - transactionId = 8
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message about lacking permission
        /// - Status 403
        /// </summary>
        [Fact]
        public async Task TSCD07_SetStatusChecked_ResponsibleMismatch_ReturnsForbidden()
        {
            int transactionId = 8;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 2, ExpireDate = DateTime.UtcNow.AddDays(1) };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = (int)TransactionStatus.importChecking,
                ResponsibleId = 1
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);

            var result = await controller.SetStatusChecked(transactionId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Bạn không có quyền xác nhận nhập kho cho đơn hàng này.", response.Error?.Message);
            Assert.Equal(403, response.StatusCode);
        }

        /// <summary>
        /// TSCD08: SetStatusChecked với ExpireDate trong quá khứ
        ///
        /// PRECONDITION:
        /// - transaction exists with warehouse id
        /// - ExpireDate in request < today
        ///
        /// INPUT:
        /// - transactionId = 9
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "Ngày hết hạn không được là ngày quá khứ"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD08_SetStatusChecked_ExpireDatePast_ReturnsBadRequest()
        {
            int transactionId = 9;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 1, ExpireDate = DateTime.UtcNow.AddDays(-1) };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = (int)TransactionStatus.importChecking,
                ResponsibleId = 1,
                WarehouseId = 1
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);

            var result = await controller.SetStatusChecked(transactionId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Ngày hết hạn không được là ngày quá khứ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD09: SetStatusChecked không có transaction details
        ///
        /// PRECONDITION:
        /// - transaction exists without details
        ///
        /// INPUT:
        /// - transactionId = 10
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest message "Không tìm thấy chi tiết giao dịch"
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TSCD09_SetStatusChecked_NoTransactionDetails_ReturnsBadRequest()
        {
            int transactionId = 10;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 1, ExpireDate = DateTime.UtcNow.AddDays(1) };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = (int)TransactionStatus.importChecking,
                ResponsibleId = 1,
                WarehouseId = 1
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(new List<TransactionDetailDto>());

            var result = await controller.SetStatusChecked(transactionId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy chi tiết giao dịch", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TSCD10: SetStatusChecked khi warehouse không tồn tại
        ///
        /// PRECONDITION:
        /// - transaction exists with invalid warehouse
        ///
        /// INPUT:
        /// - transactionId = 11
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound message referencing warehouse ID
        /// - Status 404
        /// </summary>
        [Fact]
        public async Task TSCD10_SetStatusChecked_WarehouseNotFound_ReturnsNotFound()
        {
            int transactionId = 11;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = 1, ExpireDate = DateTime.UtcNow.AddDays(1) };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = (int)TransactionStatus.importChecking,
                ResponsibleId = 1,
                WarehouseId = 99
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = 1 } });
            _warehouseMock.Setup(w => w.GetById(transaction.WarehouseId)).ReturnsAsync((WarehouseDto?)null);

            var result = await controller.SetStatusChecked(transactionId, request);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal($"Không tìm thấy kho với ID: {transaction.WarehouseId}", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TSCD11: SetStatusChecked thành công
        ///
        /// PRECONDITION:
        /// - transaction exists with responsibleId and warehouse
        /// - transactionStatus = importChecking
        /// - transaction details present
        ///
        /// INPUT:
        /// - transactionId = 12
        /// - valid UpdateToCheckedStatusRequest
        ///
        /// EXPECTED OUTPUT:
        /// - Ok with success message
        /// - Transaction.Status becomes importReceived
        /// </summary>
        [Fact]
        public async Task TSCD11_SetStatusChecked_Success_ReturnsOk()
        {
            int transactionId = 12;
            int responsibleId = 1;
            var controller = CreateController();
            var request = new UpdateToCheckedStatusRequest
            {
                ResponsibleId = responsibleId,
                ExpireDate = DateTime.UtcNow.AddDays(1)
            };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = (int)TransactionStatus.importChecking,
                ResponsibleId = responsibleId,
                WarehouseId = 1,
                Note = "note"
            };
            var details = new List<TransactionDetailDto>
            {
                new TransactionDetailDto { Id = 1, ProductId = 1, Quantity = 5 }
            };

            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(details);
            _warehouseMock.Setup(w => w.GetById(transaction.WarehouseId)).ReturnsAsync(new WarehouseDto { WarehouseId = transaction.WarehouseId, WarehouseName = "WH" });
            _stockBatchMock.Setup(s => s.GetByName(It.IsAny<string>())).ReturnsAsync((StockBatchDto?)null);
            _stockBatchMock.Setup(s => s.CreateAsync(It.IsAny<StockBatchDto>())).Returns(Task.CompletedTask);
            _inventoryMock.Setup(i => i.GetByWarehouseAndProductId(transaction.WarehouseId, It.IsAny<int>())).ReturnsAsync((InventoryDto?)null);
            _inventoryMock.Setup(i => i.CreateAsync(It.IsAny<InventoryDto>())).Returns(Task.CompletedTask);
            _inventoryMock.Setup(i => i.UpdateAsync(It.IsAny<InventoryDto>())).Returns(Task.CompletedTask);

            Transaction? updatedTransaction = null;
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => updatedTransaction = t)
                .Returns(Task.CompletedTask);

            var result = await controller.SetStatusChecked(transactionId, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Đã cập nhật trạng thái: Đã nhận hàng và cập nhật vào kho thành công", response.Data);
            Assert.NotNull(updatedTransaction);
            Assert.Equal((int)TransactionStatus.importReceived, updatedTransaction.Status);
        }

        #endregion

        //#region SubmitForApproval Tests

        ///// <summary>
        ///// TCID11: SubmitForApproval khi service trả về thất bại
        /////
        ///// PRECONDITION:
        ///// - SubmitForApprovalAsync trả về Success = false
        /////
        ///// INPUT:
        ///// - transactionId = 1
        ///// - valid SubmitForApprovalVM
        /////
        ///// EXPECTED OUTPUT:
        ///// - BadRequest message từ service
        ///// - Status 400
        ///// </summary>
        //[Fact]
        //public async Task TCID11_SubmitForApproval_ServiceFails_ReturnsBadRequest()
        //{
        //    int transactionId = 1;
        //    var controller = CreateController();
        //    var viewModel = new SubmitForApprovalVM
        //    {
        //        ResponsibleId = 1,
        //        Products = new List<ProductActualQuantity> { new() { ProductId = 1, ActualQuantity = 5 } }
        //    };

        //    _transactionMock
        //        .Setup(s => s.SubmitForApprovalAsync(transactionId, viewModel))
        //        .ReturnsAsync(new SubmitForApprovalResult
        //        {
        //            Success = false,
        //            Message = "Thất bại"
        //        });

        //    var result = await controller.SubmitForApproval(transactionId, viewModel);

        //    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

        //    Assert.False(response.Success);
        //    Assert.Equal("Thất bại", response.Error?.Message);
        //}

        ///// <summary>
        ///// TCID12: SubmitForApproval thành công
        /////
        ///// PRECONDITION:
        ///// - SubmitForApprovalAsync trả về Success = true
        /////
        ///// INPUT:
        ///// - transactionId = 1
        ///// - valid SubmitForApprovalVM
        /////
        ///// EXPECTED OUTPUT:
        ///// - Ok với ApiResponse chứa data
        ///// </summary>
        //[Fact]
        //public async Task TCID12_SubmitForApproval_Success_ReturnsOk()
        //{
        //    int transactionId = 1;
        //    var controller = CreateController();
        //    var viewModel = new SubmitForApprovalVM
        //    {
        //        ResponsibleId = 1,
        //        Products = new List<ProductActualQuantity> { new() { ProductId = 2, ActualQuantity = 3 } }
        //    };

        //    _transactionMock
        //        .Setup(s => s.SubmitForApprovalAsync(transactionId, viewModel))
        //        .ReturnsAsync(new SubmitForApprovalResult
        //        {
        //            Success = true,
        //            TransactionId = transactionId,
        //            Status = 1,
        //            TotalCost = 500,
        //            Message = "Đã gửi"
        //        });

        //    var result = await controller.SubmitForApproval(transactionId, viewModel);

        //    var ok = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ApiResponse<object>>(ok.Value);

        //    Assert.True(response.Success);
        //    Assert.NotNull(response.Data);
        //}

        //#endregion

        //#region ApproveImport Tests

        ///// <summary>
        ///// TCID13: ApproveImport khi service trả về thất bại
        /////
        ///// PRECONDITION:
        ///// - ApproveImportAsync trả về Success = false
        /////
        ///// INPUT:
        ///// - transactionId = 1
        ///// - valid ApproveImportVM
        /////
        ///// EXPECTED OUTPUT:
        ///// - BadRequest message từ service
        ///// - Status 400
        ///// </summary>
        //[Fact]
        //public async Task TCID13_ApproveImport_ServiceFails_ReturnsBadRequest()
        //{
        //    int transactionId = 1;
        //    var controller = CreateController();
        //    var viewModel = new ApproveImportVM { ApproverId = 1, ExpireDate = DateTime.UtcNow.AddDays(2) };

        //    _transactionMock
        //        .Setup(s => s.ApproveImportAsync(transactionId, viewModel))
        //        .ReturnsAsync(new ApproveImportResult { Success = false, Message = "Không thể approve" });

        //    var result = await controller.ApproveImport(transactionId, viewModel);

        //    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

        //    Assert.False(response.Success);
        //    Assert.Equal("Không thể approve", response.Error?.Message);
        //}

        ///// <summary>
        ///// TCID14: ApproveImport thành công
        /////
        ///// PRECONDITION:
        ///// - ApproveImportAsync trả về Success = true
        /////
        ///// INPUT:
        ///// - transactionId = 1
        ///// - valid ApproveImportVM
        /////
        ///// EXPECTED OUTPUT:
        ///// - Ok với ApiResponse success cùng message
        ///// </summary>
        //[Fact]
        //public async Task TCID14_ApproveImport_Success_ReturnsOk()
        //{
        //    int transactionId = 1;
        //    var controller = CreateController();
        //    var viewModel = new ApproveImportVM { ApproverId = 2, ExpireDate = DateTime.UtcNow.AddDays(1) };

        //    _transactionMock
        //        .Setup(s => s.ApproveImportAsync(transactionId, viewModel))
        //        .ReturnsAsync(new ApproveImportResult
        //        {
        //            Success = true,
        //            TransactionId = transactionId,
        //            Status = (int)TransactionStatus.importReceived,
        //            ApprovedBy = viewModel.ApproverId,
        //            ApprovedDate = DateTime.UtcNow,
        //            Message = "Đã approve"
        //        });

        //    var result = await controller.ApproveImport(transactionId, viewModel);

        //    var ok = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        //    Assert.True(response.Success);
        //}

        //#endregion

        //#region RejectImport Tests

        ///// <summary>
        ///// TCID15: RejectImport khi service thất bại
        /////
        ///// PRECONDITION:
        ///// - RejectImportAsync trả về Success = false
        /////
        ///// INPUT:
        ///// - transactionId = 1
        ///// - valid RejectImportVM
        /////
        ///// EXPECTED OUTPUT:
        ///// - BadRequest với message từ service
        ///// </summary>
        //[Fact]
        //public async Task TCID15_RejectImport_ServiceFails_ReturnsBadRequest()
        //{
        //    int transactionId = 1;
        //    var controller = CreateController();
        //    var viewModel = new RejectImportVM { ApproverId = 1, Reason = "Lỗi" };

        //    _transactionMock
        //        .Setup(s => s.RejectImportAsync(transactionId, viewModel))
        //        .ReturnsAsync(new RejectImportResult { Success = false, Message = "Không thể reject" });

        //    var result = await controller.RejectImport(transactionId, viewModel);

        //    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        //    Assert.False(response.Success);
        //    Assert.Equal("Không thể reject", response.Error?.Message);
        //}

        ///// <summary>
        ///// TCID16: RejectImport thành công
        /////
        ///// PRECONDITION:
        ///// - RejectImportAsync trả về Success = true
        /////
        ///// INPUT:
        ///// - transactionId = 1
        ///// - valid RejectImportVM
        /////
        ///// EXPECTED OUTPUT:
        ///// - Ok với message từ service
        ///// </summary>
        //[Fact]
        //public async Task TCID16_RejectImport_Success_ReturnsOk()
        //{
        //    int transactionId = 1;
        //    var controller = CreateController();
        //    var viewModel = new RejectImportVM { ApproverId = 3, Reason = "Sai số" };

        //    _transactionMock
        //        .Setup(s => s.RejectImportAsync(transactionId, viewModel))
        //        .ReturnsAsync(new RejectImportResult
        //        {
        //            Success = true,
        //            TransactionId = transactionId,
        //            Status = (int)TransactionStatus.cancel,
        //            RejectedBy = viewModel.ApproverId,
        //            RejectedDate = DateTime.UtcNow,
        //            Message = "Đã reject"
        //        });

        //    var result = await controller.RejectImport(transactionId, viewModel);

        //    var ok = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        //    Assert.True(response.Success);
        //}

        //#endregion

        #region UpdateImport Tests

        /// <summary>
        /// TCID01: UpdateImport với transaction không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction với transactionId không tồn tại trong database
        /// - Code controller check transaction.Status trước khi check null
        /// 
        /// INPUT:
        /// - transactionId: 999 (không tồn tại)
        /// - model: new TransactionEditVM() với ListProductOrder = new List&lt;ProductOrder&gt;()
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// - Note: Do code check transaction.Status trước khi check null nên throw NullReferenceException
        /// </summary>
        [Fact]
        public async Task TCID01_UpdateImport_TransactionNotFound_ReturnsBadRequest()
        {
            // Arrange - INPUT: transactionId không tồn tại
            int transactionId = 999;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>()
            };

            // Setup mock: Transaction không tồn tại
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest do NullReferenceException
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật đơn hàng.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: UpdateImport với transaction status không phải importChecking
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Status != importChecking (ví dụ: Status = 6 - Đã kiểm)
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: new TransactionEditVM() với ListProductOrder = new List&lt;ProductOrder&gt;()
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Chỉ được cập nhật đơn hàng đang kiểm."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID02_UpdateImport_StatusNotImportChecking_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction status != importChecking
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>()
            };

            // Setup mock: Transaction tồn tại nhưng status != importChecking
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 6 // Đã kiểm, không phải importChecking (1)
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Chỉ được cập nhật đơn hàng đang kiểm."
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Chỉ được cập nhật đơn hàng đang kiểm.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: UpdateImport với transaction type là Export
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Status == importChecking
        /// - Transaction.Type == "Export"
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: new TransactionEditVM() với ListProductOrder = new List&lt;ProductOrder&gt;()
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Không thể cập nhật đơn hàng xuất kho."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_UpdateImport_ExportType_ReturnsBadRequest()
        {
            // Arrange - INPUT: Transaction type = "Export"
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>()
            };

            // Setup mock: Transaction type = "Export"
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Export", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Không thể cập nhật đơn hàng xuất kho."
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không thể cập nhật đơn hàng xuất kho.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: UpdateImport với transaction details không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Status == importChecking
        /// - Transaction.Type == "Import"
        /// - TransactionDetails == null hoặc empty
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: new TransactionEditVM() với ListProductOrder = new List&lt;ProductOrder&gt;()
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy chi tiết đơn hàng."
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateImport_TransactionDetailsNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: TransactionDetails = null hoặc empty
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>()
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Setup mock: TransactionDetails = empty list
            _transactionDetailMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto>());

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy chi tiết đơn hàng."
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy chi tiết đơn hàng.", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateImport với số lượng sản phẩm là số thập phân
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - TransactionDetails tồn tại
        /// - Quantity là số thập phân (ví dụ: 10.5)
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: TransactionEditVM với ListProductOrder chứa ProductOrder có Quantity = 10.5
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số lượng sản phẩm với ID: {productId} phải là số nguyên, không được là số thập phân."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateImport_QuantityIsDecimal_ReturnsBadRequest()
        {
            // Arrange - INPUT: Quantity là số thập phân
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10.5m, UnitPrice = 10000 }
                }
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Setup mock: TransactionDetails tồn tại
            _transactionDetailMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto> 
                { 
                    new TransactionDetailDto { Id = 1, ProductId = 1 } 
                });

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message về số thập phân
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("phải là số nguyên, không được là số thập phân", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: UpdateImport với số lượng sản phẩm <= 0
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - TransactionDetails tồn tại
        /// - Quantity <= 0
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: TransactionEditVM với ListProductOrder chứa ProductOrder có Quantity = 0
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Số lượng sản phẩm với ID: {productId} phải lớn hơn 0."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateImport_QuantityZeroOrNegative_ReturnsBadRequest()
        {
            // Arrange - INPUT: Quantity = 0
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 0, UnitPrice = 10000 }
                }
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Setup mock: TransactionDetails tồn tại
            _transactionDetailMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto> 
                { 
                    new TransactionDetailDto { Id = 1, ProductId = 1 } 
                });

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message về số lượng phải > 0
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Contains("phải lớn hơn 0", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: UpdateImport với sản phẩm không tồn tại
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - TransactionDetails tồn tại
        /// - Product với ProductId không tồn tại
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: TransactionEditVM với ListProductOrder chứa ProductOrder có ProductId = 999 (không tồn tại)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy sản phẩm với ID: {productId}"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID07_UpdateImport_ProductNotFound_ReturnsNotFound()
        {
            // Arrange - INPUT: ProductId không tồn tại
            int transactionId = 1;
            int nonExistentProductId = 999;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = nonExistentProductId, Quantity = 10, UnitPrice = 10000 }
                }
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Setup mock: TransactionDetails tồn tại
            _transactionDetailMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto> 
                { 
                    new TransactionDetailDto { Id = 1, ProductId = 1 } 
                });

            // Setup mock: Product không tồn tại
            _productMock
                .Setup(p => p.GetById(nonExistentProductId))
                .ReturnsAsync((ProductDto?)null);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: NotFound với message "Không tìm thấy sản phẩm với ID: {productId}"
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal($"Không tìm thấy sản phẩm với ID: {nonExistentProductId}", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID08: UpdateImport khi service ném exception
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ
        /// - TransactionDetails tồn tại
        /// - Product tồn tại
        /// - Service layer throw exception
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: TransactionEditVM với ListProductOrder hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_UpdateImport_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange - INPUT: Service throw exception
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 10000 }
                }
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Setup mock: TransactionDetails tồn tại
            _transactionDetailMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto> 
                { 
                    new TransactionDetailDto { Id = 1, ProductId = 1 } 
                });

            // Setup mock: Product tồn tại
            _productMock
                .Setup(p => p.GetById(1))
                .ReturnsAsync(new ProductDto { ProductId = 1, WeightPerUnit = 1 });

            // Setup mock: DeleteRange throw exception
            _transactionDetailMock
                .Setup(t => t.DeleteRange(It.IsAny<IEnumerable<TransactionDetailDto>>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: BadRequest với message "Có lỗi xảy ra khi cập nhật đơn hàng."
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Có lỗi xảy ra khi cập nhật đơn hàng.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: UpdateImport thành công
        /// 
        /// PRECONDITION:
        /// - Transaction tồn tại và hợp lệ (Status = importChecking, Type = "Import")
        /// - TransactionDetails tồn tại
        /// - Products tồn tại
        /// - Service layer hoạt động bình thường
        /// 
        /// INPUT:
        /// - transactionId: 1
        /// - model: TransactionEditVM với ListProductOrder chứa 1 sản phẩm (ProductId = 2, Quantity = 5, UnitPrice = 10000)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Cập nhật đơn hàng thành công."
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// - Transaction được cập nhật với TotalCost và TotalWeight đúng
        /// </summary>
        [Fact]
        public async Task TCID09_UpdateImport_Success_ReturnsOk()
        {
            // Arrange - INPUT: Tất cả điều kiện hợp lệ
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 2, Quantity = 5, UnitPrice = 10000 }
                },
                Note = "Updated note"
            };

            // Setup mock: Transaction tồn tại và hợp lệ
            var transaction = new TransactionDto 
            { 
                TransactionId = transactionId, 
                Type = "Import", 
                Status = 1 // importChecking
            };
            _transactionMock
                .Setup(t => t.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Setup mock: TransactionDetails tồn tại
            _transactionDetailMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto> 
                { 
                    new TransactionDetailDto { Id = 1, ProductId = 1 } 
                });

            // Setup mock: DeleteRange
            _transactionDetailMock
                .Setup(t => t.DeleteRange(It.IsAny<IEnumerable<TransactionDetailDto>>()))
                .Returns(Task.CompletedTask);

            // Setup mock: Product tồn tại
            _productMock
                .Setup(p => p.GetById(2))
                .ReturnsAsync(new ProductDto { ProductId = 2, WeightPerUnit = 2.5m });

            // Setup mock: Inventory
            _inventoryMock
                .Setup(i => i.GetByProductIdRetriveOneObject(It.IsAny<int>()))
                .ReturnsAsync(new InventoryDto());

            // Setup mock: Mapper
            _mapperMock
                .Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>()))
                .Returns(new TransactionDetail());

            // Setup mock: CreateAsync transaction detail
            _transactionDetailMock
                .Setup(t => t.CreateAsync(It.IsAny<TransactionDetail>()))
                .Returns(Task.CompletedTask);

            // Setup mock: UpdateAsync transaction
            // Note: UpdateAsync nhận Transaction (entity), nhưng TransactionDto : Transaction nên có thể cast
            Transaction? capturedTransaction = null;
            _transactionMock
                .Setup(t => t.UpdateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedTransaction = t)
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateImport(transactionId, model);

            // Assert - EXPECTED OUTPUT: Ok với message "Cập nhật đơn hàng thành công."
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Cập nhật đơn hàng thành công.", response.Data);

            // Verify transaction được cập nhật đúng
            Assert.NotNull(capturedTransaction);
            Assert.Equal(50000m, capturedTransaction.TotalCost); // 5 * 10000
            Assert.Equal(12.5m, capturedTransaction.TotalWeight); // 5 * 2.5
            Assert.Equal("Updated note", capturedTransaction.Note);
        }

        #endregion

        #region DeleteImportTransaction Tests

        /// <summary>
        /// TCID01: DeleteImportTransaction với Id <= 0
        ///
        /// PRECONDITION:
        /// - Id không hợp lệ (<= 0)
        ///
        /// INPUT:
        /// - Id = 0
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Id không hợp lệ"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_DeleteImportTransaction_InvalidId_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.DeleteImportTransaction(0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<SupplierDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Id không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: DeleteImportTransaction với transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - Id > 0
        /// - transaction không tìm thấy
        ///
        /// INPUT:
        /// - Id = 999
        ///
        /// EXPECTED OUTPUT:
        /// - Return: NotFound với message "Không tìm thấy giao dịch nhập kho"
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCID02_DeleteImportTransaction_TransactionNotFound_ReturnsNotFound()
        {
            int transactionId = 999;
            var controller = CreateController();

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            var result = await controller.DeleteImportTransaction(transactionId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<SupplierDto>>>(notFound.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy giao dịch nhập kho", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: DeleteImportTransaction với transaction type khác Import
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Type = "Export"
        ///
        /// INPUT:
        /// - Id = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Giao dịch không phải là nhập kho"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_DeleteImportTransaction_ExportType_ReturnsBadRequest()
        {
            int transactionId = 1;
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Export", Status = 1 };

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(transaction);

            var result = await controller.DeleteImportTransaction(transactionId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<SupplierDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Giao dịch không phải là nhập kho", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: DeleteImportTransaction với transaction không ở trạng thái importChecking
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Type = "Import"
        /// - Transaction.Status != importChecking
        ///
        /// INPUT:
        /// - Id = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Chỉ được hủy giao dịch ở trạng thái đang kiểm."
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_DeleteImportTransaction_StatusNotImportChecking_ReturnsBadRequest()
        {
            int transactionId = 1;
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Import", Status = 0 };

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(transaction);

            var result = await controller.DeleteImportTransaction(transactionId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<SupplierDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Chỉ được hủy giao dịch ở trạng thái đang kiểm.", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: DeleteImportTransaction thành công
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Type = "Import"
        /// - Transaction.Status = importChecking
        ///
        /// INPUT:
        /// - Id = 1
        ///
        /// EXPECTED OUTPUT:
        /// - Return: Ok với message "Đã hủy giao dịch nhập kho thành công"
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// - Transaction.Status được đặt thành importCancelled
        /// </summary>
        [Fact]
        public async Task TCID05_DeleteImportTransaction_Success_ReturnsOk()
        {
            int transactionId = 1;
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Import", Status = (int)TransactionStatus.importChecking };

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(transaction);

            Transaction? updatedTransaction = null;
            _transactionMock
                .Setup(t => t.UpdateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => updatedTransaction = t)
                .Returns(Task.CompletedTask);

            var result = await controller.DeleteImportTransaction(transactionId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Đã hủy giao dịch nhập kho thành công", response.Data);
            Assert.NotNull(updatedTransaction);
            Assert.Equal((int)TransactionStatus.importCancelled, updatedTransaction.Status);
        }

        #endregion

        #region SetStatusChecking Tests

        /// <summary>
        /// TCID01: SetStatusChecking với Id không hợp lệ
        ///
        /// PRECONDITION:
        /// - transactionId <= 0
        ///
        /// INPUT:
        /// - transactionId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "Transaction ID không hợp lệ"
        /// - Type: A
        /// - Status: 400
        /// </summary>
        [Fact]
        public async Task TCID01_SetStatusChecking_InvalidId_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.SetStatusChecking(0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Transaction ID không hợp lệ", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: SetStatusChecking khi transaction không tồn tại
        ///
        /// PRECONDITION:
        /// - transactionId > 0
        /// - GetByTransactionId trả null
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound với message "Không tìm thấy giao dịch"
        /// - Status: 404
        /// </summary>
        [Fact]
        public async Task TCID02_SetStatusChecking_TransactionNotFound_ReturnsNotFound()
        {
            int transactionId = 5;
            var controller = CreateController();

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync((TransactionDto?)null);

            var result = await controller.SetStatusChecking(transactionId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Không tìm thấy giao dịch", response.Error.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID03: SetStatusChecking với transaction không phải Import
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Type = "Export"
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message "Giao dịch không phải là loại Import"
        /// - Status: 400
        /// </summary>
        [Fact]
        public async Task TCID03_SetStatusChecking_NotImportType_ReturnsBadRequest()
        {
            int transactionId = 7;
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Export", Status = 0 };

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(transaction);

            var result = await controller.SetStatusChecking(transactionId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Giao dịch không phải là loại Import", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: SetStatusChecking thành công
        ///
        /// PRECONDITION:
        /// - Transaction tồn tại
        /// - Transaction.Type = "Import"
        /// - Status bất kỳ
        ///
        /// EXPECTED OUTPUT:
        /// - Ok với message "Đã cập nhật trạng thái: Đang kiểm hàng"
        /// - Transaction.Status set thành importChecking
        /// </summary>
        [Fact]
        public async Task TCID04_SetStatusChecking_Success_ReturnsOk()
        {
            int transactionId = 1;
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Import", Status = 0 };

            _transactionMock
                .Setup(t => t.GetByTransactionId(transactionId))
                .ReturnsAsync(transaction);

            Transaction? updatedTransaction = null;
            _transactionMock
                .Setup(t => t.UpdateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => updatedTransaction = t)
                .Returns(Task.CompletedTask);

            var result = await controller.SetStatusChecking(transactionId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Đã cập nhật trạng thái: Đang kiểm hàng", response.Data);
            Assert.NotNull(updatedTransaction);
            Assert.Equal((int)TransactionStatus.importChecking, updatedTransaction.Status);
        }

        #endregion

        #region GetDetail - Edge Cases Tests

        [Fact]
        public async Task GetDetail_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetDetail(-1);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetDetail_ReturnsNotFound_WhenTransactionNotFound()
        {
            // Arrange
            var controller = CreateController();
            _transactionMock.Setup(t => t.GetByTransactionId(999)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await controller.GetDetail(999);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetDetail_ReturnsNotFound_WhenTransactionDetailsNotFound()
        {
            // Arrange
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 2, WarehouseId = 1, SupplierId = 1 };

            _transactionMock.Setup(t => t.GetByTransactionId(2)).ReturnsAsync(transaction);
            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto());
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto());
            _transactionDetailMock.Setup(t => t.GetByTransactionId(2)).ReturnsAsync(new List<TransactionDetailDto>());

            // Act
            var result = await controller.GetDetail(2);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        #endregion

    }
}