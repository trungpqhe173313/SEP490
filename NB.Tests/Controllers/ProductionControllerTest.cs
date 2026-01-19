using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Dto;
using NB.Service.Core.Mapper;
using NB.Service.FinishproductService;
using NB.Service.FinishproductService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.MaterialService;
using NB.Service.MaterialService.ViewModels;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ProductionOrderService;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.ProductionOrderService.ViewModels;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using Xunit;

namespace NB.Tests.Controllers
{
    public class ProductionControllerTest
    {
        private readonly Mock<IProductionOrderService> _productionOrderServiceMock;
        private readonly Mock<IMaterialService> _materialServiceMock;
        private readonly Mock<IFinishproductService> _finishproductServiceMock;
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly Mock<IStockBatchService> _stockBatchServiceMock;
        private readonly Mock<IWarehouseService> _warehouseServiceMock;
        private readonly Mock<IRepository<IoTdevice>> _iotDeviceRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ProductionController>> _loggerMock;
        private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
        private readonly ProductionController _controller;
        private const int RawMaterialWarehouseId = 2;

        public ProductionControllerTest()
        {
            _productionOrderServiceMock = new Mock<IProductionOrderService>();
            _materialServiceMock = new Mock<IMaterialService>();
            _finishproductServiceMock = new Mock<IFinishproductService>();
            _productServiceMock = new Mock<IProductService>();
            _inventoryServiceMock = new Mock<IInventoryService>();
            _stockBatchServiceMock = new Mock<IStockBatchService>();
            _warehouseServiceMock = new Mock<IWarehouseService>();
            _iotDeviceRepositoryMock = new Mock<IRepository<IoTdevice>>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ProductionController>>();
            _cloudinaryServiceMock = new Mock<ICloudinaryService>();

            _productionOrderServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<ProductionOrder>()))
                .Returns(Task.CompletedTask);

            _inventoryServiceMock
                .Setup(i => i.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            _stockBatchServiceMock
                .Setup(s => s.UpdateNoTracking(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            _iotDeviceRepositoryMock
                .Setup(r => r.SaveAsync())
                .Returns(Task.CompletedTask);

            _materialServiceMock
                .Setup(m => m.DeleteRange(It.IsAny<IEnumerable<Material>>()))
                .Returns(Task.CompletedTask);
            _materialServiceMock
                .Setup(m => m.CreateAsync(It.IsAny<Material>()))
                .Returns(Task.CompletedTask);

            _finishproductServiceMock
                .Setup(f => f.DeleteRange(It.IsAny<IEnumerable<Finishproduct>>()))
                .Returns(Task.CompletedTask);
            _finishproductServiceMock
                .Setup(f => f.CreateAsync(It.IsAny<Finishproduct>()))
                .Returns(Task.CompletedTask);

            _controller = new ProductionController(
                _productionOrderServiceMock.Object,
                _materialServiceMock.Object,
                _finishproductServiceMock.Object,
                _productServiceMock.Object,
                _inventoryServiceMock.Object,
                _stockBatchServiceMock.Object,
                _warehouseServiceMock.Object,
                _iotDeviceRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _cloudinaryServiceMock.Object);
        }

        #region CreateProductionOrder Tests

        /// <summary>
        /// TCID01: CreateProductionOrder với request null
        ///
        /// PRECONDITION:
        /// - Request = null
        ///
        /// INPUT:
        /// - po = null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Dữ liệu request không được để trống")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID01_CreateProductionOrder_RequestNull_ReturnsBadRequest()
        {
            var result = await _controller.CreateProductionOrder(null!);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu request không được để trống", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateProductionOrder khi ModelState không hợp lệ
        ///
        /// PRECONDITION:
        /// - Request có thuộc tính không hợp lệ
        ///
        /// INPUT:
        /// - po = valid request nhưng controller.ModelState invalid
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Dữ liệu không hợp lệ")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID02_CreateProductionOrder_InvalidModelState_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            _controller.ModelState.AddModelError("Test", "invalid");

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreateProductionOrder với MaterialProductId không hợp lệ
        ///
        /// PRECONDITION:
        /// - MaterialProductId <= 0
        ///
        /// INPUT:
        /// - po.MaterialProductId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("ID sản phẩm nguyên liệu không hợp lệ")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID03_CreateProductionOrder_InvalidMaterialProductId_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            request.MaterialProductId = 0;

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("ID sản phẩm nguyên liệu không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreateProductionOrder với MaterialQuantity không hợp lệ
        ///
        /// PRECONDITION:
        /// - MaterialQuantity <= 0
        ///
        /// INPUT:
        /// - po.MaterialQuantity = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Số lượng nguyên liệu phải lớn hơn 0")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID04_CreateProductionOrder_InvalidMaterialQuantity_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            request.MaterialQuantity = 0;

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Số lượng nguyên liệu phải lớn hơn 0", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreateProductionOrder với danh sách thành phẩm trống
        ///
        /// PRECONDITION:
        /// - ListFinishProduct = null hoặc empty
        ///
        /// INPUT:
        /// - ListFinishProduct = new List&lt;ProductProductionDto&gt;()
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Danh sách thành phẩm không được để trống")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID05_CreateProductionOrder_EmptyFinishProducts_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            request.ListFinishProduct = new List<ProductProductionDto>();

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Danh sách thành phẩm không được để trống", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CreateProductionOrder khi responsibleId không hợp lệ
        ///
        /// PRECONDITION:
        /// - responsibleId = null hoặc <= 0
        ///
        /// INPUT:
        /// - responsibleId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Phải có nhân viên phụ trách đơn")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID06_CreateProductionOrder_InvalidResponsibleId_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            request.responsibleId = 0;

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Phải có nhân viên phụ trách đơn", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: CreateProductionOrder với ID sản phẩm thành phẩm không hợp lệ
        ///
        /// PRECONDITION:
        /// - Một finishProduct.ProductId <= 0
        ///
        /// INPUT:
        /// - finishProduct.ProductId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("ID sản phẩm thành phẩm không hợp lệ")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID07_CreateProductionOrder_InvalidFinishProductId_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            request.ListFinishProduct[0].ProductId = 0;

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("ID sản phẩm thành phẩm không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID08: CreateProductionOrder với số lượng thành phẩm âm
        ///
        /// PRECONDITION:
        /// - finishProduct.Quantity &lt; 0
        ///
        /// INPUT:
        /// - finishProduct.Quantity = -1
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail($"Số lượng thành phẩm với ID {productId} phải lớn hơn bằng 0")
        /// - Status 400
        /// </summary>
        [Fact]
        public async Task TCID08_CreateProductionOrder_FinishProductNegativeQuantity_ReturnsBadRequest()
        {
            var request = CreateValidProductionRequest();
            request.ListFinishProduct[0].Quantity = -1;

            var result = await _controller.CreateProductionOrder(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal($"Số lượng thành phẩm với ID {request.ListFinishProduct[0].ProductId} phải lớn hơn bằng 0", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: CreateProductionOrder khi service trả về lỗi
        ///
        /// PRECONDITION:
        /// - Dữ liệu hợp lệ
        /// - Service trả về ApiResponse.Fail
        ///
        /// INPUT:
        /// - po = valid request
        ///
        /// EXPECTED OUTPUT:
        /// - StatusCode theo ApiResponse.StatusCode
        /// - Response = ApiResponse.Fail("Service failure")
        /// </summary>
        [Fact]
        public async Task TCID09_CreateProductionOrder_ServiceFails_ReturnsStatusCode()
        {
            var request = CreateValidProductionRequest();
            var serviceResponse = ApiResponse<ProductionOrder>.Fail("Service failure", 500);
            _productionOrderServiceMock
                .Setup(s => s.CreateProductionOrderAsync(It.IsAny<ProductionRequest>()))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.CreateProductionOrder(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(objectResult.Value);

            Assert.False(response.Success);
            Assert.Equal("Service failure", response.Error?.Message);
        }

        /// <summary>
        /// TCID10: CreateProductionOrder thành công
        ///
        /// PRECONDITION:
        /// - Dữ liệu hợp lệ
        /// - Service trả về ApiResponse.Ok
        ///
        /// INPUT:
        /// - po = valid request
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Ok("Tạo đơn sản xuất thành công")
        /// </summary>
        [Fact]
        public async Task TCID10_CreateProductionOrder_ServiceSucceeds_ReturnsOk()
        {
            var request = CreateValidProductionRequest();
            var serviceResponse = ApiResponse<ProductionOrder>.Ok(new ProductionOrder());
            _productionOrderServiceMock
                .Setup(s => s.CreateProductionOrderAsync(It.IsAny<ProductionRequest>()))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.CreateProductionOrder(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Tạo đơn sản xuất thành công", response.Data);
        }

        #endregion

        #region ChangeToProcessing Tests

        /// <summary>
        /// TPCD01: ChangeToProcessing với productionOrderId không hợp lệ
        ///
        /// PRECONDITION:
        /// - productionOrderId <= 0
        ///
        /// INPUT:
        /// - productionOrderId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Id đơn sản xuất không hợp lệ", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TPCD01_ChangeToProcessing_InvalidId_ReturnsBadRequest()
        {
            var result = await _controller.ChangeToProcessing(0, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Id đơn sản xuất không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TPCD02: ChangeToProcessing với DeviceCode trống
        ///
        /// PRECONDITION:
        /// - request.DeviceCode = whitespace
        ///
        /// INPUT:
        /// - DeviceCode = "   "
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("DeviceCode là bắt buộc", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TPCD02_ChangeToProcessing_DeviceCodeMissing_ReturnsBadRequest()
        {
            var request = new ChangeToProcessingRequest { DeviceCode = "   " };
            var result = await _controller.ChangeToProcessing(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("DeviceCode là bắt buộc", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TPCD03: ChangeToProcessing với đơn sản xuất không tồn tại
        ///
        /// PRECONDITION:
        /// - GetByIdAsync trả về null
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound chứa ApiResponse.Fail("Không tìm thấy đơn sản xuất", 404)
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TPCD03_ChangeToProcessing_ProductionOrderNotFound_ReturnsNotFound()
        {
            const int productionOrderId = 1;
            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync((ProductionOrder?)null);

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy đơn sản xuất", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TPCD04: ChangeToProcessing với đơn không ở trạng thái Pending
        ///
        /// PRECONDITION:
        /// - Transaction.Status != Pending
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Chỉ có thể chuyển đơn từ trạng thái Pending sang Processing")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TPCD04_ChangeToProcessing_StatusNotPending_ReturnsBadRequest()
        {
            const int productionOrderId = 2;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Processing);
            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(productionOrder);
            SetupProductionOrderQueryable(new[] { productionOrder });

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Chỉ có thể chuyển đơn từ trạng thái Pending sang Processing", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TPCD05: ChangeToProcessing khi đã có đơn đang Processing
        ///
        /// PRECONDITION:
        /// - Một đơn khác đang ở trạng thái Processing
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail với message có ID của đơn đang processing
        /// - Type: A (Abnormal)
        /// - Status: 409 Conflict
        /// </summary>
        [Fact]
        public async Task TPCD05_ChangeToProcessing_ExistingProcessingOrder_ReturnsBadRequest()
        {
            const int productionOrderId = 3;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(pendingOrder);

            var processingOrder = CreateProductionOrder(99, (int)ProductionOrderStatus.Processing);
            SetupProductionOrderQueryable(new[] { processingOrder });

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Contains($"#{processingOrder.Id}", response.Error?.Message);
            Assert.Equal(409, response.StatusCode);
        }

        /// <summary>
        /// TPCD06: ChangeToProcessing khi không có nguyên liệu
        ///
        /// PRECONDITION:
        /// - materialService trả về danh sách rỗng
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Đơn sản xuất không có nguyên liệu")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TPCD06_ChangeToProcessing_NoMaterials_ReturnsBadRequest()
        {
            const int productionOrderId = 4;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());
            SetupMaterialQueryable(Array.Empty<Material>());

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Đơn sản xuất không có nguyên liệu", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TPCD07: ChangeToProcessing với nguyên liệu không tồn tại trong kho
        ///
        /// PRECONDITION:
        /// - InventoryService trả về null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message chứa tên sản phẩm
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TPCD07_ChangeToProcessing_MaterialInventoryMissing_ReturnsBadRequest()
        {
            const int productionOrderId = 5;
            const int productId = 11;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());

            var materials = new[] { CreateMaterial(productionOrderId, productId, 3) };
            SetupMaterialQueryable(materials);

            _inventoryServiceMock
                .Setup(i => i.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId))
                .ReturnsAsync((InventoryDto?)null);

            _productServiceMock
                .Setup(p => p.GetByIdAsync(productId))
                .ReturnsAsync(new Product { ProductId = productId, ProductName = "Nguyên liệu thử" });

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Contains("Không tìm thấy sản phẩm 'Nguyên liệu thử'", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TPCD08: ChangeToProcessing với số lượng nguyên liệu không đủ
        ///
        /// PRECONDITION:
        /// - InventoryDto.Quantity nhỏ hơn material.Quantity
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa message mô tả thiếu số lượng
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TPCD08_ChangeToProcessing_InsufficientInventory_ReturnsBadRequest()
        {
            const int productionOrderId = 6;
            const int productId = 12;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());

            var materials = new[] { CreateMaterial(productionOrderId, productId, 10) };
            SetupMaterialQueryable(materials);

            var inventoryDto = new InventoryDto
            {
                WarehouseId = RawMaterialWarehouseId,
                ProductId = productId,
                Quantity = 3
            };
            _inventoryServiceMock
                .Setup(i => i.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId))
                .ReturnsAsync(inventoryDto);

            _productServiceMock
                .Setup(p => p.GetByIdAsync(productId))
                .ReturnsAsync(new Product { ProductId = productId, ProductName = "Vật tư A" });

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Contains("không đủ 10 yêu cầu", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TPCD09: ChangeToProcessing khi không tìm thấy lô hàng khả dụng
        ///
        /// PRECONDITION:
        /// - StockBatchService trả về list rỗng
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa message thông báo không tìm thấy lô
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TPCD09_ChangeToProcessing_NoStockBatch_ReturnsBadRequest()
        {
            const int productionOrderId = 7;
            const int productId = 13;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());

            var materials = new[] { CreateMaterial(productionOrderId, productId, 5) };
            SetupMaterialQueryable(materials);

            _inventoryServiceMock
                .Setup(i => i.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new InventoryDto { WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 10 });

            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto>());

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Contains($"Không tìm thấy lô hàng khả dụng cho sản phẩm {productId}", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TPCD10: ChangeToProcessing khi không tìm thấy thiết bị IoT
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest thông báo deviceCode không tồn tại
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TPCD10_ChangeToProcessing_IoTDeviceMissing_ReturnsBadRequest()
        {
            const int productionOrderId = 8;
            const int productId = 14;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());

            var material = CreateMaterial(productionOrderId, productId, 5);
            SetupMaterialQueryable(new[] { material });

            _inventoryServiceMock
                .Setup(i => i.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new InventoryDto { WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 10 });

            var stockBatchDto = CreateStockBatchDto(10, RawMaterialWarehouseId, productId, 10, 0);
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(stockBatchDto.BatchId))
                .ReturnsAsync(new StockBatch
                {
                    BatchId = stockBatchDto.BatchId,
                    ProductId = stockBatchDto.ProductId,
                    WarehouseId = stockBatchDto.WarehouseId,
                    QuantityIn = stockBatchDto.QuantityIn,
                    QuantityOut = stockBatchDto.QuantityOut
                });

            _inventoryServiceMock
                .Setup(i => i.GetEntityByWarehouseAndProductIdAsync(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new Inventory { InventoryId = 1, WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 20m });

            SetupIoTDeviceQueryable(Array.Empty<IoTdevice>());

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal($"Không tìm thấy thiết bị với mã '{CreateProcessingRequest().DeviceCode}'", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TPCD11: ChangeToProcessing khi thiết bị đang bận
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest với message bao gồm mã thiết bị và productionId
        /// - Type: A (Abnormal)
        /// - Status: 409 Conflict
        /// </summary>
        [Fact]
        public async Task TPCD11_ChangeToProcessing_IoTDeviceBusy_ReturnsBadRequest()
        {
            const int productionOrderId = 9;
            const int productId = 15;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());

            var material = CreateMaterial(productionOrderId, productId, 5);
            SetupMaterialQueryable(new[] { material });

            _inventoryServiceMock
                .Setup(i => i.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new InventoryDto { WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 10 });

            var stockBatchDto = CreateStockBatchDto(11, RawMaterialWarehouseId, productId, 10, 0);
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(stockBatchDto.BatchId))
                .ReturnsAsync(new StockBatch
                {
                    BatchId = stockBatchDto.BatchId,
                    ProductId = stockBatchDto.ProductId,
                    WarehouseId = stockBatchDto.WarehouseId,
                    QuantityIn = stockBatchDto.QuantityIn,
                    QuantityOut = stockBatchDto.QuantityOut
                });

            _inventoryServiceMock
                .Setup(i => i.GetEntityByWarehouseAndProductIdAsync(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new Inventory { InventoryId = 2, WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 20m });

            var deviceCode = "DEVICE-BUSY";
            var busyDevice = new IoTdevice { DeviceCode = deviceCode, CurrentProductionId = 99 };
            SetupIoTDeviceQueryable(new[] { busyDevice });

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest(deviceCode));

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Contains($"Thiết bị '{deviceCode}' đang được sử dụng cho đơn sản xuất #99", response.Error?.Message);
            Assert.Equal(409, response.StatusCode);
        }

        /// <summary>
        /// TPCD12: ChangeToProcessing thành công
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Ok("Đơn sản xuất đang được xử lý")
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TPCD12_ChangeToProcessing_Success_ReturnsOk()
        {
            const int productionOrderId = 10;
            const int productId = 16;
            var pendingOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(pendingOrder);
            SetupProductionOrderQueryable(Array.Empty<ProductionOrder>());

            var material = CreateMaterial(productionOrderId, productId, 5);
            SetupMaterialQueryable(new[] { material });

            _inventoryServiceMock
                .Setup(i => i.GetByWarehouseAndProductId(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new InventoryDto { WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 10 });

            var stockBatchDto = CreateStockBatchDto(12, RawMaterialWarehouseId, productId, 10, 0);
            _stockBatchServiceMock
                .Setup(s => s.GetByProductIdForOrder(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StockBatchDto> { stockBatchDto });

            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(stockBatchDto.BatchId))
                .ReturnsAsync(new StockBatch
                {
                    BatchId = stockBatchDto.BatchId,
                    ProductId = stockBatchDto.ProductId,
                    WarehouseId = stockBatchDto.WarehouseId,
                    QuantityIn = stockBatchDto.QuantityIn,
                    QuantityOut = stockBatchDto.QuantityOut
                });

            _inventoryServiceMock
                .Setup(i => i.GetEntityByWarehouseAndProductIdAsync(RawMaterialWarehouseId, productId))
                .ReturnsAsync(new Inventory { InventoryId = 3, WarehouseId = RawMaterialWarehouseId, ProductId = productId, Quantity = 20m });

            var device = new IoTdevice { DeviceCode = "DEVICE-OK", CurrentProductionId = null };
            SetupIoTDeviceQueryable(new[] { device });

            var result = await _controller.ChangeToProcessing(productionOrderId, CreateProcessingRequest(device.DeviceCode));

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Đơn sản xuất đang được xử lý", response.Data);
        }

        #endregion

        #region ChangeToCancel Tests

        /// <summary>
        /// TCC01: ChangeToCancel với id không hợp lệ
        ///
        /// PRECONDITION:
        /// - productionOrderId <= 0
        ///
        /// INPUT:
        /// - productionOrderId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Id đơn sản xuất không hợp lệ", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCC01_ChangeToCancel_InvalidId_ReturnsBadRequest()
        {
            var result = await _controller.ChangeToCancel(0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Id đơn sản xuất không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCC02: ChangeToCancel khi đơn không tồn tại
        ///
        /// PRECONDITION:
        /// - _productionOrderService.GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - productionOrderId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound chứa ApiResponse.Fail("Không tìm thấy đơn sản xuất", 404)
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCC02_ChangeToCancel_NotFound_ReturnsNotFound()
        {
            const int productionOrderId = 1;
            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync((ProductionOrder?)null);

            var result = await _controller.ChangeToCancel(productionOrderId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy đơn sản xuất", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCC03: ChangeToCancel khi đơn không ở trạng thái Pending
        ///
        /// PRECONDITION:
        /// - ProductionOrder.Status != Pending
        ///
        /// INPUT:
        /// - productionOrderId = 2
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Chỉ có thể hủy đơn sản xuất khi đơn đang ở trạng thái Đang chờ xử lý (Pending)", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCC03_ChangeToCancel_StatusNotPending_ReturnsBadRequest()
        {
            const int productionOrderId = 2;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Processing);
            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(productionOrder);

            var result = await _controller.ChangeToCancel(productionOrderId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Chỉ có thể hủy đơn sản xuất khi đơn đang ở trạng thái Đang chờ xử lý (Pending)", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCC04: ChangeToCancel thành công
        ///
        /// PRECONDITION:
        /// - ProductionOrder tồn tại với trạng thái Pending
        /// - _productionOrderService.UpdateAsync không ném
        ///
        /// INPUT:
        /// - productionOrderId = 3
        ///
        /// EXPECTED OUTPUT:
        /// - Ok chứa ApiResponse.Ok("Đơn sản xuất đã được hủy")
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCC04_ChangeToCancel_Success_ReturnsOk()
        {
            const int productionOrderId = 3;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            ProductionOrder? updatedOrder = null;

            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(productionOrder);

            _productionOrderServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<ProductionOrder>()))
                .Callback<ProductionOrder>(order => updatedOrder = order)
                .Returns(Task.CompletedTask);

            var result = await _controller.ChangeToCancel(productionOrderId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Đơn sản xuất đã được hủy", response.Data);
            Assert.Equal((int)ProductionOrderStatus.Cancel, updatedOrder?.Status);
        }

        /// <summary>
        /// TCC05: ChangeToCancel khi UpdateAsync ném exception
        ///
        /// PRECONDITION:
        /// - _productionOrderService.UpdateAsync ném InvalidOperationException
        ///
        /// INPUT:
        /// - productionOrderId = 4
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Có lỗi xảy ra khi hủy đơn sản xuất")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCC05_ChangeToCancel_ExceptionThrown_ReturnsBadRequest()
        {
            const int productionOrderId = 4;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);

            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(productionOrder);

            _productionOrderServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<ProductionOrder>()))
                .ThrowsAsync(new InvalidOperationException("db"));

            var result = await _controller.ChangeToCancel(productionOrderId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi hủy đơn sản xuất", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region ChangeToFinished Tests

        /// <summary>
        /// TCFD01: ChangeToFinished với productionOrderId không hợp lệ
        ///
        /// PRECONDITION:
        /// - productionOrderId <= 0
        ///
        /// INPUT:
        /// - productionOrderId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Id đơn sản xuất không hợp lệ", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCFD01_ChangeToFinished_InvalidId_ReturnsBadRequest()
        {
            var result = await _controller.ChangeToFinished(0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Id đơn sản xuất không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCFD02: ChangeToFinished khi đơn sản xuất không tồn tại
        ///
        /// PRECONDITION:
        /// - GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - productionOrderId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound chứa ApiResponse.Fail("Không tìm thấy đơn sản xuất", 404)
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TCFD02_ChangeToFinished_NotFound_ReturnsNotFound()
        {
            const int productionOrderId = 1;
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync((ProductionOrder?)null);

            var result = await _controller.ChangeToFinished(productionOrderId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy đơn sản xuất", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCFD03: ChangeToFinished với trạng thái không phải WaitingApproval
        ///
        /// PRECONDITION:
        /// - ProductionOrder.Status != WaitingApproval
        ///
        /// INPUT:
        /// - productionOrderId với status khác WaitingApproval
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Chỉ có thể phê duyệt đơn đang chờ duyệt (WaitingApproval)", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCFD03_ChangeToFinished_StatusNotWaitingApproval_ReturnsBadRequest()
        {
            const int productionOrderId = 2;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Processing);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);

            var result = await _controller.ChangeToFinished(productionOrderId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Chỉ có thể phê duyệt đơn đang chờ duyệt (WaitingApproval)", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCFD04: ChangeToFinished khi không có thành phẩm
        ///
        /// PRECONDITION:
        /// - finishproductService trả về danh sách rỗng
        ///
        /// INPUT:
        /// - productionOrderId hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Đơn sản xuất không có thành phẩm", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCFD04_ChangeToFinished_NoFinishProducts_ReturnsBadRequest()
        {
            const int productionOrderId = 3;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.WaitingApproval);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.ChangeToFinished(productionOrderId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Đơn sản xuất không có thành phẩm", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCFD05: ChangeToFinished khi có exception trong service
        ///
        /// PRECONDITION:
        /// - throw exception trong try block
        ///
        /// INPUT:
        /// - productionOrderId hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Có lỗi xảy ra khi phê duyệt đơn sản xuất", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCFD05_ChangeToFinished_ExceptionThrown_ReturnsBadRequest()
        {
            const int productionOrderId = 4;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.WaitingApproval);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            SetupFinishproductQueryable(new[] { CreateFinishProduct(productionOrderId, 1, 5) });

            _stockBatchServiceMock.Setup(s => s.GetByName(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

            var result = await _controller.ChangeToFinished(productionOrderId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi phê duyệt đơn sản xuất", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCFD06: ChangeToFinished thành công
        ///
        /// PRECONDITION:
        /// - ProductionOrder tồn tại với status WaitingApproval
        /// - finishproductService trả danh sách tối thiểu
        /// - Inventory và StockBatch hoạt động bình thường
        ///
        /// INPUT:
        /// - productionOrderId hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Ok("Đơn sản xuất đã được phê duyệt và hoàn thành")
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCFD06_ChangeToFinished_Success_ReturnsOk()
        {
            const int productionOrderId = 5;
            const int productId = 20;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.WaitingApproval);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            var finishProducts = new[] { CreateFinishProduct(productionOrderId, productId, 10) };
            SetupFinishproductQueryable(finishProducts);
            SetupMaterialQueryable(Array.Empty<Material>());

            _stockBatchServiceMock
                .Setup(s => s.GetByName(It.IsAny<string>()))
                .ReturnsAsync((StockBatchDto?)null);

            _mapperMock
                .Setup(m => m.Map<StockBatchProductionCreateVM, StockBatch>(It.IsAny<StockBatchProductionCreateVM>()))
                .Returns(new StockBatch { BatchId = 1 });

            _stockBatchServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<StockBatch>()))
                .Returns(Task.CompletedTask);

            _stockBatchServiceMock
                .Setup(s => s.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new StockBatch { BatchId = 1 });

            _inventoryServiceMock
                .Setup(i => i.GetEntityByWarehouseAndProductIdAsync(1, productId))
                .ReturnsAsync(new Inventory { InventoryId = 1, Quantity = 100m });

            _inventoryServiceMock
                .Setup(i => i.UpdateNoTracking(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.ChangeToFinished(productionOrderId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Đơn sản xuất đã được phê duyệt và hoàn thành", response.Data);
        }

        #endregion

        #region UpdateProductionOrder Tests

        /// <summary>
        /// TUPD01: UpdateProductionOrder với request null
        ///
        /// PRECONDITION:
        /// - request = null
        ///
        /// INPUT:
        /// - productionOrderId = 1
        /// - request = null
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Dữ liệu request không được để trống", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TUPD01_UpdateProductionOrder_RequestNull_ReturnsBadRequest()
        {
            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(1, null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu request không được để trống", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TUPD02: UpdateProductionOrder với ModelState invalid
        ///
        /// PRECONDITION:
        /// - ModelState có lỗi
        ///
        /// INPUT:
        /// - productionOrderId = 1
        /// - request hợp lệ nhưng ModelState invalid
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Dữ liệu không hợp lệ", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TUPD02_UpdateProductionOrder_InvalidModelState_ReturnsBadRequest()
        {
            var request = CreateUpdateRequest();
            _controller.ModelState.AddModelError("Test", "invalid");
            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TUPD03: UpdateProductionOrder với id không hợp lệ
        ///
        /// PRECONDITION:
        /// - productionOrderId <= 0
        ///
        /// INPUT:
        /// - productionOrderId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Id đơn sản xuất không hợp lệ", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TUPD03_UpdateProductionOrder_InvalidId_ReturnsBadRequest()
        {
            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());
            var result = await _controller.UpdateProductionOrder(0, CreateUpdateRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Id đơn sản xuất không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TUPD04: UpdateProductionOrder với đơn không tồn tại
        ///
        /// PRECONDITION:
        /// - productionOrderService.GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - productionOrderId = 1
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound chứa ApiResponse.Fail("Không tìm thấy đơn sản xuất", 404)
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TUPD04_UpdateProductionOrder_NotFound_ReturnsNotFound()
        {
            const int productionOrderId = 1;
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync((ProductionOrder?)null);
            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(productionOrderId, CreateUpdateRequest());

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy đơn sản xuất", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TUPD05: UpdateProductionOrder khi status khác Pending
        ///
        /// PRECONDITION:
        /// - ProductionOrder.Status != Pending
        ///
        /// INPUT:
        /// - productionOrderId với status Processing
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Chỉ có thể chỉnh sửa ... Pending", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TUPD05_UpdateProductionOrder_StatusNotPending_ReturnsBadRequest()
        {
            const int productionOrderId = 2;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Processing);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(productionOrderId, CreateUpdateRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Chỉ có thể chỉnh sửa đơn sản xuất khi đơn đang ở trạng thái Pending (Đang chờ xử lý)", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TUPD06: UpdateProductionOrder với sản phẩm nguyên liệu không tồn tại
        ///
        /// PRECONDITION:
        /// - request.MaterialProductId > 0, MaterialQuantity > 0
        /// - ProductService.GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - request với MaterialProductId = 5, MaterialQuantity = 10
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Sản phẩm nguyên liệu không tồn tại", 404)
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TUPD06_UpdateProductionOrder_MaterialMissing_ReturnsBadRequest()
        {
            const int productionOrderId = 3;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            var request = CreateUpdateRequest();
            request.MaterialProductId = 5;
            request.MaterialQuantity = 10;

            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            _productServiceMock.Setup(p => p.GetByIdAsync(request.MaterialProductId.Value)).ReturnsAsync((ProductDto?)null);

            var result = await _controller.UpdateProductionOrder(productionOrderId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Sản phẩm nguyên liệu không tồn tại", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TUPD07: UpdateProductionOrder với FinishProduct.Id không hợp lệ
        ///
        /// PRECONDITION:
        /// - request.ListFinishProduct chứa product có ProductId <= 0
        ///
        /// INPUT:
        /// - FinishProduct.ProductId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("ID sản phẩm thành phẩm không hợp lệ", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TUPD07_UpdateProductionOrder_InvalidFinishProductId_ReturnsBadRequest()
        {
            const int productionOrderId = 4;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            var request = CreateUpdateRequest();
            request.ListFinishProduct = new List<ProductProductionDto> { new() { ProductId = 0, Quantity = 1 } };

            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(productionOrderId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("ID sản phẩm thành phẩm không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TUPD08: UpdateProductionOrder với FinishProduct.Quantity không hợp lệ
        ///
        /// PRECONDITION:
        /// - FinishProduct.Quantity <= 0
        ///
        /// INPUT:
        /// - FinishProduct.Quantity = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail($"Số lượng thành phẩm với ID {id} phải lớn hơn 0", 400)
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TUPD08_UpdateProductionOrder_FinishProductQuantityInvalid_ReturnsBadRequest()
        {
            const int productionOrderId = 5;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            var request = CreateUpdateRequest();
            request.ListFinishProduct = new List<ProductProductionDto> { new() { ProductId = 1, Quantity = 0 } };

            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(productionOrderId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal($"Số lượng thành phẩm với ID {request.ListFinishProduct[0].ProductId} phải lớn hơn 0", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TUPD09: UpdateProductionOrder khi finish product không tồn tại
        ///
        /// PRECONDITION:
        /// - ProductService.GetByIds không trả về sản phẩm
        ///
        /// INPUT:
        /// - ListFinishProduct chứa ProductId = 2
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail($"Sản phẩm hoàn thiện với ID {id} không tồn tại", 404)
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TUPD09_UpdateProductionOrder_FinishProductNotFound_ReturnsBadRequest()
        {
            const int productionOrderId = 6;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            var request = CreateUpdateRequest();
            request.ListFinishProduct = new List<ProductProductionDto> { new() { ProductId = 2, Quantity = 1 } };

            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            _productServiceMock.Setup(p => p.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto>());

            var result = await _controller.UpdateProductionOrder(productionOrderId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal($"Sản phẩm hoàn thiện với ID {request.ListFinishProduct[0].ProductId} không tồn tại", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TUPD10: UpdateProductionOrder thành công
        ///
        /// PRECONDITION:
        /// - request cung cấp nguyên liệu mới và thành phẩm
        /// - ProductionOrder ở trạng thái Pending
        ///
        /// INPUT:
        /// - productionOrderId = 7
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Ok("Cập nhật đơn sản xuất thành công")
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TUPD10_UpdateProductionOrder_Success_ReturnsOk()
        {
            const int productionOrderId = 7;
            const int materialProductId = 10;
            const int finishProductId = 20;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            SetupMaterialQueryable(new[] { CreateMaterial(productionOrderId, materialProductId, 5) });
            SetupFinishproductQueryable(new[] { CreateFinishProduct(productionOrderId, finishProductId, 5) });

            var request = CreateUpdateRequest();
            request.MaterialProductId = materialProductId;
            request.MaterialQuantity = 15;
            request.ListFinishProduct = new List<ProductProductionDto>
            {
                new() { ProductId = finishProductId, Quantity = 10 }
            };

            _productServiceMock.Setup(p => p.GetByIdAsync(materialProductId)).ReturnsAsync(new ProductDto { ProductId = materialProductId, ProductName = "Mat", WeightPerUnit = 2 });
            _productServiceMock.Setup(p => p.GetByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductDto> { new() { ProductId = finishProductId, ProductName = "Fin", WeightPerUnit = 1 } });

            _mapperMock.Setup(m => m.Map<MaterialCreateVM, Material>(It.IsAny<MaterialCreateVM>())).Returns(new Material());
            _mapperMock.Setup(m => m.Map<FinishproductCreateVM, Finishproduct>(It.IsAny<FinishproductCreateVM>())).Returns(new Finishproduct());

            var result = await _controller.UpdateProductionOrder(productionOrderId, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Cập nhật đơn sản xuất thành công", response.Data);
        }

        /// <summary>
        /// TUPD11: UpdateProductionOrder khi có exception
        ///
        /// PRECONDITION:
        /// - _productionOrderService.UpdateAsync ném exception
        ///
        /// INPUT:
        /// - productionOrderId = 8
        ///
        /// EXPECTED OUTPUT:
        /// - StatusCode 500 với ApiResponse.Fail chứa message exception
        /// - Type: A (Abnormal)
        /// </summary>
        [Fact]
        public async Task TUPD11_UpdateProductionOrder_ExceptionThrown_ReturnsServerError()
        {
            const int productionOrderId = 8;
            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            _productionOrderServiceMock.Setup(s => s.GetByIdAsync(productionOrderId)).ReturnsAsync(productionOrder);
            _productionOrderServiceMock.Setup(s => s.UpdateAsync(It.IsAny<ProductionOrder>())).ThrowsAsync(new InvalidOperationException("db"));

            SetupMaterialQueryable(Array.Empty<Material>());
            SetupFinishproductQueryable(Array.Empty<Finishproduct>());

            var result = await _controller.UpdateProductionOrder(productionOrderId, CreateUpdateRequest());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var response = Assert.IsType<ApiResponse<ProductionOrder>>(objectResult.Value);

            Assert.False(response.Success);
            Assert.Contains("Có lỗi xảy ra khi cập nhật đơn sản xuất: db", response.Error?.Message);
            Assert.Equal(500, response.StatusCode);
        }

        #endregion

        #region GetData Tests

        /// <summary>
        /// TGD01: GetData khi không có đơn sản xuất
        ///
        /// PRECONDITION:
        /// - Service trả về PagedList không chứa phần tử nào
        ///
        /// INPUT:
        /// - search: new ProductionOrderSearch()
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult chứa ApiResponse.Ok với PagedList rỗng
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TGD01_GetData_WithNoItems_ReturnsOk()
        {
            // Arrange - INPUT: search tối thiểu
            var search = new ProductionOrderSearch();
            var pagedResult = PagedList<ProductionOrderDto>.CreateFromList(new List<ProductionOrderDto>(), search);
            _productionOrderServiceMock
                .Setup(s => s.GetData(It.IsAny<ProductionOrderSearch>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: OkObjectResult với PagedList rỗng
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Same(pagedResult, response.Data);
            Assert.Empty(response.Data.Items);
        }

        /// <summary>
        /// TGD02: GetData khi có đơn sản xuất và cần gắn StatusName
        ///
        /// PRECONDITION:
        /// - Service trả về danh sách có Status
        ///
        /// INPUT:
        /// - search: ProductionOrderSearch tối thiểu
        ///
        /// EXPECTED OUTPUT:
        /// - OkObjectResult với StatusName khớp mô tả enum
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TGD02_GetData_WithItems_PopulatesStatusName()
        {
            // Arrange - INPUT: search tối thiểu với 2 đơn
            var search = new ProductionOrderSearch();
            const string existingLabel = "Preexisting";
            var sourceItems = new List<ProductionOrderDto>
            {
                new() { Id = 1, Status = (int)ProductionOrderStatus.Finished },
                new() { Id = 2, Status = null, StatusName = existingLabel }
            };
            var pagedResult = PagedList<ProductionOrderDto>.CreateFromList(sourceItems, search);
            _productionOrderServiceMock
                .Setup(s => s.GetData(It.IsAny<ProductionOrderSearch>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: OkObjectResult với StatusName được gán đúng
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(2, response.Data.Items.Count);
            Assert.Equal(ProductionOrderStatus.Finished.GetDescription(), response.Data.Items[0].StatusName);
            Assert.Equal(existingLabel, response.Data.Items[1].StatusName);
        }

        /// <summary>
        /// TGD03: GetData khi service ném exception
        ///
        /// PRECONDITION:
        /// - _productionOrderService.GetData ném exception
        ///
        /// INPUT:
        /// - search: ProductionOrderSearch mặc định
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequestObjectResult với ApiResponse.Fail("Có lỗi xảy ra khi lấy dữ liệu")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TGD03_GetData_WhenServiceThrows_ReturnsBadRequest()
        {
            // Arrange - INPUT: search tối thiểu
            var search = new ProductionOrderSearch();
            _productionOrderServiceMock
                .Setup(s => s.GetData(It.IsAny<ProductionOrderSearch>()))
                .ThrowsAsync(new InvalidOperationException("Service failure"));

            // Act
            var result = await _controller.GetData(search);

            // Assert - EXPECTED OUTPUT: BadRequestObjectResult với ApiResponse.Fail
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ProductionOrderDto>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region GetDetail Tests

        /// <summary>
        /// TGDL01: GetDetail khi ModelState không hợp lệ
        ///
        /// PRECONDITION:
        /// - ModelState chứa lỗi
        ///
        /// INPUT:
        /// - Id = 1 (giá trị hợp lệ nhưng ModelState invalid)
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Dữ liệu không hợp lệ")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TGDL01_GetDetail_ModelStateInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("test", "invalid");

            try
            {
                var result = await _controller.GetDetail(1);

                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                var response = Assert.IsType<ApiResponse<FullProductionOrderVM>>(badRequest.Value);

                Assert.False(response.Success);
                Assert.Equal("Dữ liệu không hợp lệ", response.Error?.Message);
                Assert.Equal(400, response.StatusCode);
            }
            finally
            {
                _controller.ModelState.Clear();
            }
        }

        /// <summary>
        /// TGDL02: GetDetail khi Id không hợp lệ (<= 0)
        ///
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - Id = 0
        ///
        /// INPUT:
        /// - Id = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Id không hợp lệ")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TGDL02_GetDetail_InvalidId_ReturnsBadRequest()
        {
            _controller.ModelState.Clear();

            var result = await _controller.GetDetail(0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullProductionOrderVM>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Id không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TGDL03: GetDetail khi đơn sản xuất không tồn tại
        ///
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - _productionOrderService.GetByIdAsync trả về null
        ///
        /// INPUT:
        /// - Id = 5
        ///
        /// EXPECTED OUTPUT:
        /// - NotFound chứa ApiResponse.Fail("Không tìm thấy đơn sản xuất.")
        /// - Type: A (Abnormal)
        /// - Status: 404 Not Found
        /// </summary>
        [Fact]
        public async Task TGDL03_GetDetail_NotFound_ReturnsNotFound()
        {
            const int productionOrderId = 5;
            _controller.ModelState.Clear();

            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync((ProductionOrder?)null);

            var result = await _controller.GetDetail(productionOrderId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullProductionOrderVM>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Không tìm thấy đơn sản xuất.", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TGDL04: GetDetail thành công khi có đủ dữ liệu
        ///
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - ProductionOrder tồn tại
        /// - Các dịch vụ liên quan trả về dữ liệu cần thiết
        ///
        /// INPUT:
        /// - Id = 7
        ///
        /// EXPECTED OUTPUT:
        /// - Ok với ApiResponse.Ok chứa FullProductionOrderVM
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TGDL04_GetDetail_WithValidId_ReturnsDetailedResponse()
        {
            const int productionOrderId = 7;
            const int finishProductId = 10;
            const int materialProductId = 20;

            _controller.ModelState.Clear();

            var productionOrder = CreateProductionOrder(productionOrderId, (int)ProductionOrderStatus.Pending);
            productionOrder.ResponsibleId = 99;
            productionOrder.Note = "Testing note";
            productionOrder.StartDate = DateTime.Today.AddDays(-1);
            productionOrder.CreatedAt = DateTime.Today.AddDays(-2);

            var finishProducts = new[]
            {
                CreateFinishProduct(productionOrderId, finishProductId, 3)
            };
            finishProducts[0].Id = 101;
            finishProducts[0].CreatedAt = DateTime.Today;

            var materials = new[]
            {
                CreateMaterial(productionOrderId, materialProductId, 5)
            };
            materials[0].Id = 202;
            materials[0].CreatedAt = DateTime.Today;
            materials[0].LastUpdated = DateTime.Today.AddHours(-1);

            SetupFinishproductQueryable(finishProducts);
            SetupMaterialQueryable(materials);

            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(productionOrderId))
                .ReturnsAsync(productionOrder);

            _userServiceMock
                .Setup(u => u.GetByIdAsync(productionOrder.ResponsibleId.Value))
                .ReturnsAsync(new UserDto { FullName = "Nguyen Van A" });

            _productServiceMock
                .Setup(p => p.GetByIds(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ProductDto>
                {
                    new() { ProductId = finishProductId, ProductName = "Finish Product", ProductCode = "FP-01", WeightPerUnit = 1 },
                    new() { ProductId = materialProductId, ProductName = "Material Product", ProductCode = "MP-01", WeightPerUnit = 2 }
                });

            _warehouseServiceMock
                .Setup(w => w.GetByListWarehouseId(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<WarehouseDto?>
                {
                    new WarehouseDto { WarehouseId = 1, WarehouseName = "Finished Warehouse" },
                    new WarehouseDto { WarehouseId = RawMaterialWarehouseId, WarehouseName = "Raw Warehouse" }
                });

            var result = await _controller.GetDetail(productionOrderId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullProductionOrderVM>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(productionOrderId, response.Data.Id);
            Assert.Equal("Testing note", response.Data.Note);
            Assert.Equal(ProductionOrderStatus.Pending.GetDescription(), response.Data.StatusName);
            Assert.Equal("Nguyen Van A", response.Data.ResponsibleEmployeeFullName);
            Assert.Single(response.Data.FinishProducts);
            Assert.Equal("Finish Product", response.Data.FinishProducts[0].ProductName);
            Assert.Single(response.Data.Materials);
            Assert.Equal("Material Product", response.Data.Materials[0].ProductName);
            Assert.Equal("Finished Warehouse", response.Data.FinishProducts[0].WarehouseName);
            Assert.Equal("Raw Warehouse", response.Data.Materials[0].WarehouseName);
        }

        /// <summary>
        /// TGDL05: GetDetail khi có exception từ service
        ///
        /// PRECONDITION:
        /// - _productionOrderService.GetByIdAsync ném exception
        ///
        /// INPUT:
        /// - Id = 7
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest chứa ApiResponse.Fail("Có lỗi xảy ra khi lấy dữ liệu")
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TGDL05_GetDetail_WhenServiceThrows_ReturnsBadRequest()
        {
            _controller.ModelState.Clear();

            _productionOrderServiceMock
                .Setup(s => s.GetByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await _controller.GetDetail(7);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<FullProductionOrderVM>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi lấy dữ liệu", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        private static ChangeToProcessingRequest CreateProcessingRequest(string deviceCode = "DEVICE-1")
        {
            return new ChangeToProcessingRequest
            {
                DeviceCode = deviceCode
            };
        }

        private void SetupProductionOrderQueryable(IEnumerable<ProductionOrder> orders)
        {
            _productionOrderServiceMock
                .Setup(s => s.GetQueryable())
                .Returns(AsAsyncQueryable(orders ?? Enumerable.Empty<ProductionOrder>()));
        }

        private void SetupMaterialQueryable(IEnumerable<Material> materials)
        {
            _materialServiceMock
                .Setup(m => m.GetQueryable())
                .Returns(AsAsyncQueryable(materials ?? Enumerable.Empty<Material>()));
        }

        private void SetupIoTDeviceQueryable(IEnumerable<IoTdevice> devices)
        {
            _iotDeviceRepositoryMock
                .Setup(r => r.GetQueryable())
                .Returns(AsAsyncQueryable(devices ?? Enumerable.Empty<IoTdevice>()));
        }

        private void SetupFinishproductQueryable(IEnumerable<Finishproduct> finishProducts)
        {
            _finishproductServiceMock
                .Setup(f => f.GetQueryable())
                .Returns(AsAsyncQueryable(finishProducts ?? Enumerable.Empty<Finishproduct>()));
        }

        private static IQueryable<T> AsAsyncQueryable<T>(IEnumerable<T> source)
        {
            return new TestAsyncEnumerable<T>(source ?? Enumerable.Empty<T>());
        }

        private static ProductionOrder CreateProductionOrder(int id, int status)
        {
            return new ProductionOrder
            {
                Id = id,
                Status = status
            };
        }

        private static Material CreateMaterial(int productionOrderId, int productId, int quantity)
        {
            return new Material
            {
                ProductionId = productionOrderId,
                ProductId = productId,
                Quantity = quantity,
                WarehouseId = RawMaterialWarehouseId
            };
        }

        private static Finishproduct CreateFinishProduct(int productionOrderId, int productId, int quantity)
        {
            return new Finishproduct
            {
                ProductionId = productionOrderId,
                ProductId = productId,
                Quantity = quantity,
                WarehouseId = 1
            };
        }

        private static StockBatchDto CreateStockBatchDto(int batchId, int warehouseId, int productId, decimal quantityIn, decimal quantityOut)
        {
            return new StockBatchDto
            {
                BatchId = batchId,
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityIn = quantityIn,
                QuantityOut = quantityOut,
                ImportDate = DateTime.Today.AddDays(-1),
                ExpireDate = DateTime.Today.AddDays(10)
            };
        }

        private static ProductionRequest CreateValidProductionRequest()
        {
            return new ProductionRequest
            {
                MaterialProductId = 1,
                MaterialQuantity = 10,
                responsibleId = 1,
                ListFinishProduct = new List<ProductProductionDto>
                {
                    new ProductProductionDto
                    {
                        ProductId = 1,
                        Quantity = 5
                    }
                }
            };
        }

        private static UpdateProductionOrderRequest CreateUpdateRequest()
        {
            return new UpdateProductionOrderRequest();
        }
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression)!;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments().FirstOrDefault()
                ?? throw new InvalidOperationException("Unable to determine result type.");

            var executeMethod = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })
                ?? throw new InvalidOperationException("Unable to locate Execute method.");

            var executionResult = executeMethod
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new[] { expression });

            var fromResultMethod = typeof(Task)
                .GetMethod(nameof(Task.FromResult))
                ?? throw new InvalidOperationException("Unable to locate Task.FromResult.");

            return (TResult)fromResultMethod
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult })!;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }
}
