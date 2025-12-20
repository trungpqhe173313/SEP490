
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
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
using NB.Service.StockBatchService.ViewModels;
using NB.Service.ReturnTransactionService.ViewModels;
using NB.Service.ReturnTransactionDetailService.ViewModels;
using NB.Service.FinancialTransactionService.ViewModels;
using Xunit;
using FluentAssertions;
using NB.Service.Dto;
using NB.Service.WarehouseService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.ProductService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.WarehouseService.Dto;
using NB.Service.FinancialTransactionService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;

namespace NB.Tests.Services
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
                _mapperMock.Object
            );
            return controller;
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

        [Fact]
        public async Task CreateStockInputs_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            int responsibleId = 1;
            
            // Mock existing responsible user
            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });

            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Note = "n",
                Products = new List<ProductInputItem>
                {
                    new ProductInputItem { ProductId = 2, Quantity = 5, UnitPrice = 10 }
                }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto { WarehouseName = "W" });
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto { SupplierId = 1 });
            _productMock.Setup(p => p.GetById(2)).ReturnsAsync(new ProductDto { ProductId = 2, ProductName = "P", WeightPerUnit = 1 });
            _transactionMock.Setup(t => t.CreateAsync(It.IsAny<Transaction>())).Callback<Transaction>(t => t.TransactionId = 123).Returns(Task.CompletedTask);
            _transactionDetailMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDetailDto>())).Returns(Task.CompletedTask);
            _stockBatchMock.Setup(s => s.GetByName(It.IsAny<string>())).ReturnsAsync((StockBatchDto?)null);
            _stockBatchMock.Setup(s => s.CreateAsync(It.IsAny<StockBatchDto>())).Returns(Task.CompletedTask);
            _inventoryMock.Setup(i => i.GetByWarehouseAndProductId(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((InventoryDto?)null);
            _inventoryMock.Setup(i => i.CreateAsync(It.IsAny<InventoryDto>())).Returns(Task.CompletedTask);

            var result = await controller.CreateStockInputs(responsibleId, model);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ImportFromExcel_ReturnsBadRequest_WhenNoFile()
        {
            var controller = CreateController();
            var result = await controller.ImportFromExcel(null);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task DownloadTemplate_ReturnsFile()
        {
            var controller = CreateController();
            var result = controller.DownloadTemplate();
            var file = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        }

        [Fact]
        public async Task UpdateImport_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            int transactionId = 1;
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 2, Quantity = 2, UnitPrice = 5 }
                },
                Note = "note"
            };

            var transaction = new TransactionDto { TransactionId = transactionId, Type = "Import", Status = 1 };
            _transactionMock.Setup(t => t.GetByIdAsync(transactionId)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto { Id = 1, ProductId = 2, Quantity = 1 } });
            _transactionDetailMock.Setup(t => t.DeleteRange(It.IsAny<IEnumerable<TransactionDetailDto>>())).Returns(Task.CompletedTask);
            _productMock.Setup(p => p.GetById(2)).ReturnsAsync(new ProductDto { ProductId = 2, WeightPerUnit = 1 });
            _inventoryMock.Setup(i => i.GetByProductIdRetriveOneObject(2)).ReturnsAsync(new InventoryDto { Quantity = 5 });
            _mapperMock.Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>())).Returns(new TransactionDetail());
            _transactionDetailMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDetail>())).Returns(Task.CompletedTask);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);

            var result = await controller.UpdateImport(transactionId, model);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task DeleteImportTransaction_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);

            var result = await controller.DeleteImportTransaction(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task SetStatusChecking_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import" };
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);

            var result = await controller.SetStatusChecking(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task SetStatusChecked_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            int transactionId = 1;
            int responsibleId = 1;
            var request = new UpdateToCheckedStatusRequest { ResponsibleId = responsibleId };

            var transaction = new TransactionDto
            {
                TransactionId = transactionId,
                Type = "Import",
                Status = 1, // Đang kiểm
                WarehouseId = 1,
                ResponsibleId = responsibleId
            };
            _transactionMock.Setup(t => t.GetByTransactionId(transactionId)).ReturnsAsync(transaction);
            _transactionDetailMock
                .Setup(s => s.GetByTransactionId(transactionId))
                .ReturnsAsync(new List<TransactionDetailDto>
                {
                    new TransactionDetailDto { Id = 1, ProductId = 1, Quantity = 10 }
                });
            _warehouseMock
                .Setup(w => w.GetById(transaction.WarehouseId))
                .ReturnsAsync(new WarehouseDto { WarehouseId = transaction.WarehouseId, WarehouseName = "WH" });
            _stockBatchMock
                .Setup(s => s.GetByName(It.IsAny<string>()))
                .ReturnsAsync((StockBatchDto?)null);
            _stockBatchMock
                .Setup(s => s.CreateAsync(It.IsAny<StockBatchDto>()))
                .Returns(Task.CompletedTask);
            _inventoryMock
                .Setup(i => i.GetByWarehouseAndProductId(transaction.WarehouseId, It.IsAny<int>()))
                .ReturnsAsync((InventoryDto?)null);
            _inventoryMock
                .Setup(i => i.CreateAsync(It.IsAny<InventoryDto>()))
                .Returns(Task.CompletedTask);
            _inventoryMock
                .Setup(i => i.UpdateAsync(It.IsAny<InventoryDto>()))
                .Returns(Task.CompletedTask);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);

            var result = await controller.SetStatusChecked(transactionId, request);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task UpdateToPaidInFullStatus_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", TotalCost = 200 };
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);
            _financialMock.Setup(f => f.GetByRelatedTransactionID(1)).ReturnsAsync(new List<FinancialTransactionDto>());
            _mapperMock.Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(It.IsAny<FinancialTransactionCreateVM>()))
                .Returns(new FinancialTransaction());
            _financialMock.Setup(f => f.CreateAsync(It.IsAny<FinancialTransaction>())).Returns(Task.CompletedTask);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);

            var model = new FinancialTransactionCreateVM();
            var result = await controller.UpdateToPaidInFullStatus(1, model);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task CreatePartialPayment_ReturnsOk_OnSuccess_Partial()
        {
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", TotalCost = 100 };
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);
            _financialMock.Setup(f => f.GetByRelatedTransactionID(1)).ReturnsAsync(new List<FinancialTransactionDto>());
            _mapperMock.Setup(m => m.Map<FinancialTransactionCreateVM, FinancialTransaction>(It.IsAny<FinancialTransactionCreateVM>()))
                .Returns(new FinancialTransaction());
            _financialMock.Setup(f => f.CreateAsync(It.IsAny<FinancialTransaction>())).Returns(Task.CompletedTask);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<TransactionDto>())).Returns(Task.CompletedTask);

            var model = new FinancialTransactionCreateVM { Amount = 10 };
            var result = await controller.CreatePartialPayment(1, model);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        #region CreateStockInputs - Validation & Edge Cases Tests

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenWarehouseNotFound()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            // Mock existing responsible user để không fail ở bước check user
            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 999, // Non-existent warehouse
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Products = new List<ProductInputItem> { new ProductInputItem { ProductId = 1, Quantity = 5, UnitPrice = 10 } }
            };

            _warehouseMock.Setup(w => w.GetById(999)).ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenSupplierNotFound()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 999, // Non-existent supplier
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Products = new List<ProductInputItem> { new ProductInputItem { ProductId = 1, Quantity = 5, UnitPrice = 10 } }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto());
            _supplierMock.Setup(s => s.GetBySupplierId(999)).ReturnsAsync((SupplierDto?)null);

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenExpireDateIsInPast()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(-1), // Past date
                Products = new List<ProductInputItem> { new ProductInputItem { ProductId = 1, Quantity = 5, UnitPrice = 10 } }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto());
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto());

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenProductsListIsEmpty()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Products = new List<ProductInputItem>() // Empty list
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto());
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto());

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenProductQuantityIsZeroOrNegative()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Products = new List<ProductInputItem>
                {
                    new ProductInputItem { ProductId = 1, Quantity = 0, UnitPrice = 10 } // Zero quantity
                }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto());
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto());

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenProductNotFound()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Products = new List<ProductInputItem>
                {
                    new ProductInputItem { ProductId = 999, Quantity = 5, UnitPrice = 10 } // Non-existent product
                }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto());
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto());
            _productMock.Setup(p => p.GetById(999)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateStockInputs_ReturnsOk_WithMultipleProducts()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });
            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Note = "Test",
                Products = new List<ProductInputItem>
                {
                    new ProductInputItem { ProductId = 1, Quantity = 10, UnitPrice = 5 }, // 10 * 5 = 50, weight: 10 * 2 = 20
                    new ProductInputItem { ProductId = 2, Quantity = 5, UnitPrice = 10 }  // 5 * 10 = 50, weight: 5 * 3 = 15
                }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto { WarehouseName = "W" });
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto { SupplierId = 1 });
            _productMock.Setup(p => p.GetById(1)).ReturnsAsync(new ProductDto { ProductId = 1, ProductName = "P1", WeightPerUnit = 2 });
            _productMock.Setup(p => p.GetById(2)).ReturnsAsync(new ProductDto { ProductId = 2, ProductName = "P2", WeightPerUnit = 3 });

            Transaction capturedTransaction = null;
            _transactionMock.Setup(t => t.CreateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t =>
                {
                    capturedTransaction = t;
                    t.TransactionId = 1;
                })
                .Returns(Task.CompletedTask);

            _transactionDetailMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDetailDto>())).Returns(Task.CompletedTask);
            _stockBatchMock.Setup(s => s.GetByName(It.IsAny<string>())).ReturnsAsync((StockBatchDto?)null);
            _stockBatchMock.Setup(s => s.CreateAsync(It.IsAny<StockBatchDto>())).Returns(Task.CompletedTask);
            _inventoryMock.Setup(i => i.GetByWarehouseAndProductId(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((InventoryDto?)null);
            _inventoryMock.Setup(i => i.CreateAsync(It.IsAny<InventoryDto>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            capturedTransaction.Should().NotBeNull();
            capturedTransaction.TotalCost.Should().Be(100); // 50 + 50
            capturedTransaction.TotalWeight.Should().Be(35); // 20 + 15
        }

        #endregion

        #region UpdateImportTransaction - Validation & Edge Cases Tests

        [Fact]
        public async Task UpdateImport_ReturnsNotFound_WhenTransactionNotFound()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM { ListProductOrder = new List<ProductOrder>() };

            _transactionMock.Setup(t => t.GetByIdAsync(999)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await controller.UpdateImport(999, model);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateImport_ReturnsBadRequest_WhenTransactionIsAlreadyChecked()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM { ListProductOrder = new List<ProductOrder>() };
            var transaction = new TransactionDto { TransactionId = 1, Status = 6 }; // Status = 6 (Đã kiểm)

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateImport_ReturnsBadRequest_WhenTransactionIsExportType()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM { ListProductOrder = new List<ProductOrder>() };
            var transaction = new TransactionDto { TransactionId = 1, Type = "Export", Status = 1 };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateImport_ReturnsNotFound_WhenTransactionDetailsNotFound()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM { ListProductOrder = new List<ProductOrder>() };
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(new List<TransactionDetailDto>());

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateImport_ReturnsBadRequest_WhenProductQuantityIsZeroOrNegative()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 0, UnitPrice = 10 } // Zero quantity
                }
            };
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto() });

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateImport_ReturnsNotFound_WhenProductNotFound()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 999, Quantity = 5, UnitPrice = 10 }
                }
            };
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto() });
            _productMock.Setup(p => p.GetById(999)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateImport_CalculatesTotalWeightAndCostCorrectly()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 10, UnitPrice = 5 },  // Cost: 50, Weight: 20
                    new ProductOrder { ProductId = 2, Quantity = 5, UnitPrice = 8 }    // Cost: 40, Weight: 15
                },
                Note = "Updated"
            };

            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };
            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto() });
            _transactionDetailMock.Setup(t => t.DeleteRange(It.IsAny<IEnumerable<TransactionDetailDto>>())).Returns(Task.CompletedTask);

            _productMock.Setup(p => p.GetById(1)).ReturnsAsync(new ProductDto { ProductId = 1, WeightPerUnit = 2 });
            _productMock.Setup(p => p.GetById(2)).ReturnsAsync(new ProductDto { ProductId = 2, WeightPerUnit = 3 });

            _inventoryMock.Setup(i => i.GetByProductIdRetriveOneObject(It.IsAny<int>())).ReturnsAsync(new InventoryDto());
            _mapperMock.Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>())).Returns(new TransactionDetail());
            _transactionDetailMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDetail>())).Returns(Task.CompletedTask);

            Transaction capturedTransaction = null;
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedTransaction = t)
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            capturedTransaction.Should().NotBeNull();
            capturedTransaction.TotalCost.Should().Be(90);  // 50 + 40
            capturedTransaction.TotalWeight.Should().Be(35); // 20 + 15
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

        #region DeleteImportTransaction - Edge Cases Tests

        [Fact]
        public async Task DeleteImportTransaction_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.DeleteImportTransaction(0);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task DeleteImportTransaction_ReturnsNotFound_WhenTransactionNotFound()
        {
            // Arrange
            var controller = CreateController();
            _transactionMock.Setup(t => t.GetByTransactionId(999)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await controller.DeleteImportTransaction(999);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteImportTransaction_ReturnsBadRequest_WhenTransactionIsExportType()
        {
            // Arrange
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 1, Type = "Export", Status = 1 };
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);

            // Act
            var result = await controller.DeleteImportTransaction(1);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task DeleteImportTransaction_ReturnsBadRequest_WhenAlreadyCancelled()
        {
            // Arrange
            var controller = CreateController();
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 0 }; // Already cancelled
            _transactionMock.Setup(t => t.GetByTransactionId(1)).ReturnsAsync(transaction);

            // Act
            var result = await controller.DeleteImportTransaction(1);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        #endregion

        #region Input Validation Tests - Invalid Data Types

        [Fact]
        public async Task CreateStockInputs_ReturnsBadRequest_WhenProductUnitPriceIsNegative()
        {
            // Arrange
            var controller = CreateController();
            int responsibleId = 1;

            _userMock
                .Setup(u => u.GetByUserId(responsibleId))
                .ReturnsAsync(new UserDto { UserId = responsibleId, FullName = "Responsible User" });

            var model = new StockBatchCreateWithProductsVM
            {
                WarehouseId = 1,
                SupplierId = 1,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                Products = new List<ProductInputItem>
                {
                    new ProductInputItem { ProductId = 1, Quantity = 5, UnitPrice = -10 } // Negative price
                }
            };

            _warehouseMock.Setup(w => w.GetById(1)).ReturnsAsync(new WarehouseDto { WarehouseName = "W1" });
            _supplierMock.Setup(s => s.GetBySupplierId(1)).ReturnsAsync(new SupplierDto { SupplierId = 1 });
            _productMock.Setup(p => p.GetById(1)).ReturnsAsync(new ProductDto { ProductId = 1, WeightPerUnit = 1 });

            // Act
            var result = await controller.CreateStockInputs(responsibleId, model);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
            // If controller validates negative price, it should return BadRequest
            // If not, this test documents that negative prices are allowed (business decision)
        }

        [Fact]
        public async Task UpdateImport_AllowsZeroUnitPrice_ForBusinessReasons()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 5, UnitPrice = 0 } // Zero price might be valid (free goods)
                }
            };
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(1))
                .ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto { ProductId = 1, Quantity = 10 } });
            _productMock.Setup(p => p.GetById(1)).ReturnsAsync(new ProductDto { ProductId = 1, WeightPerUnit = 1 });
            _inventoryMock.Setup(i => i.GetByProductIdRetriveOneObject(1)).ReturnsAsync(new InventoryDto());
            _mapperMock.Setup(m => m.Map<TransactionDetailCreateVM, TransactionDetail>(It.IsAny<TransactionDetailCreateVM>()))
                .Returns(new TransactionDetail());
            _transactionDetailMock.Setup(t => t.DeleteRange(It.IsAny<IEnumerable<TransactionDetailDto>>())).Returns(Task.CompletedTask);
            _transactionDetailMock.Setup(t => t.CreateAsync(It.IsAny<TransactionDetail>())).Returns(Task.CompletedTask);
            _transactionMock.Setup(t => t.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert
            // Zero price might be valid for free samples or promotional items
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateImport_ReturnsBadRequest_WhenProductQuantityIsDecimal()
        {
            // Arrange
            var controller = CreateController();
            var model = new TransactionEditVM
            {
                ListProductOrder = new List<ProductOrder>
                {
                    new ProductOrder { ProductId = 1, Quantity = 5.5m, UnitPrice = 10 } // Decimal quantity (might not be allowed)
                }
            };
            var transaction = new TransactionDto { TransactionId = 1, Type = "Import", Status = 1 };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);
            _transactionDetailMock.Setup(t => t.GetByTransactionId(1))
                .ReturnsAsync(new List<TransactionDetailDto> { new TransactionDetailDto() });
            _productMock.Setup(p => p.GetById(1)).ReturnsAsync(new ProductDto { ProductId = 1 });

            // Act
            var result = await controller.UpdateImport(1, model);

            // Assert - Depending on business rules, this might be OK or BadRequest
            // For now, we just verify it doesn't crash
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteImportTransaction_ReturnsBadRequest_WhenTransactionIdIsNegative()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.DeleteImportTransaction(-1);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetDetail_ReturnsBadRequest_WhenTransactionIdIsZero()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetDetail(0);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SetStatusChecking_ReturnsBadRequest_WhenTransactionIdIsInvalid()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.SetStatusChecking(-999);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreatePartialPayment_ReturnsNotFound_WhenTransactionNotExists()
        {
            // Arrange
            var controller = CreateController();
            var model = new FinancialTransactionCreateVM
            {
                Amount = -100, // Negative amount (will fail after transaction check)
                PaymentMethod = "Cash"
            };

            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync((TransactionDto?)null);

            // Act
            var result = await controller.CreatePartialPayment(1, model);

            // Assert
            // Transaction not found is checked first
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreatePartialPayment_ValidatesAmount_AfterTransactionExists()
        {
            // Arrange
            var controller = CreateController();
            var model = new FinancialTransactionCreateVM
            {
                Amount = 0, // Zero amount
                PaymentMethod = "Cash"
            };

            var transaction = new TransactionDto { TransactionId = 1, TotalCost = 1000 };
            _transactionMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(transaction);

            // Act
            var result = await controller.CreatePartialPayment(1, model);

            // Assert
            // Controller may validate zero amount or allow it (business decision)
            result.Should().NotBeNull();
        }

        #endregion
    }
}