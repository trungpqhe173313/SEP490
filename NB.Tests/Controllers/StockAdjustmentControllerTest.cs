using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.Dto;
using NB.Service.StockAdjustmentService;
using NB.Service.StockAdjustmentService.ViewModels;
using Xunit;

namespace NB.Tests.Controllers
{
    public class StockAdjustmentControllerTest
    {
        private readonly Mock<IStockAdjustmentService> _stockAdjustmentServiceMock;
        private readonly Mock<ILogger<StockAdjustmentController>> _loggerMock;
        private readonly StockAdjustmentController _controller;

        public StockAdjustmentControllerTest()
        {
            _stockAdjustmentServiceMock = new Mock<IStockAdjustmentService>();
            _loggerMock = new Mock<ILogger<StockAdjustmentController>>();
            _controller = new StockAdjustmentController(_stockAdjustmentServiceMock.Object, _loggerMock.Object);
        }

        #region CreateDraft Tests

        /// <summary>
        /// TCID01: CreateDraft khi model không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState chứa validation error
        /// 
        /// INPUT:
        /// - model: new StockAdjustmentDraftCreateVM() (Details kosong)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message lấy từ ModelState
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_CreateDraft_InvalidModelState_ReturnsBadRequest()
        {
            var model = new StockAdjustmentDraftCreateVM();
            _controller.ModelState.AddModelError("Details", "Details là bắt buộc");

            var result = await _controller.CreateDraft(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Details là bắt buộc", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateDraft thành công
        /// 
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - Service trả về draft mới
        /// 
        /// INPUT:
        /// - model: WarehouseId hợp lệ và ít nhất 1 detail
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: OK với ApiResponse.Success = true
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID02_CreateDraft_ValidRequest_ReturnsOk()
        {
            var model = new StockAdjustmentDraftCreateVM
            {
                WarehouseId = 1,
                Details = new List<StockAdjustmentDetailItemVM>
                {
                    new StockAdjustmentDetailItemVM
                    {
                        ProductId = 11,
                        ActualQuantity = 5
                    }
                }
            };

            var draftResponse = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = 100,
                WarehouseId = 1,
                WarehouseName = "Kho test",
                Status = 1,
                StatusDescription = "Draft",
                Details = new List<StockAdjustmentDetailResponseVM>
                {
                    new StockAdjustmentDetailResponseVM
                    {
                        DetailId = 5,
                        ProductId = 11,
                        ProductCode = "P11",
                        ProductName = "Sản phẩm 11",
                        ActualQuantity = 5,
                        SystemQuantity = 3,
                        Difference = 2,
                        Note = "Ghi chú"
                    }
                }
            };

            _stockAdjustmentServiceMock
                .Setup(s => s.CreateDraftAsync(model))
                .ReturnsAsync(draftResponse);

            var result = await _controller.CreateDraft(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Null(response.Error);
            Assert.Equal(200, response.StatusCode);
            Assert.Same(draftResponse, response.Data);
        }

        /// <summary>
        /// TCID03: CreateDraft khi service throw exception
        /// 
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - Service bất ngờ throw exception
        /// 
        /// INPUT:
        /// - model: WarehouseId hợp lệ và details tối thiểu
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message từ exception
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID03_CreateDraft_ServiceThrows_ReturnsBadRequest()
        {
            var model = new StockAdjustmentDraftCreateVM
            {
                WarehouseId = 1,
                Details = new List<StockAdjustmentDetailItemVM>
                {
                    new StockAdjustmentDetailItemVM
                    {
                        ProductId = 22,
                        ActualQuantity = 2
                    }
                }
            };

            _stockAdjustmentServiceMock
                .Setup(s => s.CreateDraftAsync(It.IsAny<StockAdjustmentDraftCreateVM>()))
                .ThrowsAsync(new Exception("Database unavailable"));

            var result = await _controller.CreateDraft(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Database unavailable", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region UpdateDraft Tests

        /// <summary>
        /// TCID04: UpdateDraft khi model không hợp lệ
        /// 
        /// PRECONDITION:
        /// - ModelState chứa validation error
        /// 
        /// INPUT:
        /// - id: 5, model: new StockAdjustmentDraftUpdateVM() (Details trống)
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message lấy từ ModelState
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID04_UpdateDraft_InvalidModelState_ReturnsBadRequest()
        {
            var model = new StockAdjustmentDraftUpdateVM();
            _controller.ModelState.AddModelError("Details", "Details là bắt buộc");

            var result = await _controller.UpdateDraft(5, model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Details là bắt buộc", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: UpdateDraft thành công
        /// 
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - Service cập nhật thành công
        /// 
        /// INPUT:
        /// - id: 7, model: new StockAdjustmentDraftUpdateVM với 1 detail
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: OK với ApiResponse.Success = true
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID05_UpdateDraft_ValidRequest_ReturnsOk()
        {
            const int draftId = 7;
            var model = new StockAdjustmentDraftUpdateVM
            {
                Details = new List<StockAdjustmentDetailItemVM>
                {
                    new StockAdjustmentDetailItemVM
                    {
                        ProductId = 33,
                        ActualQuantity = 12
                    }
                }
            };

            var draftResponse = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = draftId,
                WarehouseId = 2,
                WarehouseName = "Kho cập nhật",
                Status = 1,
                StatusDescription = "Draft",
                Details = new List<StockAdjustmentDetailResponseVM>
                {
                    new StockAdjustmentDetailResponseVM
                    {
                        DetailId = 10,
                        ProductId = 33,
                        ProductCode = "P33",
                        ProductName = "Sản phẩm 33",
                        ActualQuantity = 12,
                        SystemQuantity = 12,
                        Difference = 0
                    }
                }
            };

            _stockAdjustmentServiceMock
                .Setup(s => s.UpdateDraftAsync(draftId, model))
                .ReturnsAsync(draftResponse);

            var result = await _controller.UpdateDraft(draftId, model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Null(response.Error);
            Assert.Equal(200, response.StatusCode);
            Assert.Same(draftResponse, response.Data);
        }

        /// <summary>
        /// TCID06: UpdateDraft khi service throw exception
        /// 
        /// PRECONDITION:
        /// - ModelState hợp lệ
        /// - Service throw exception
        /// 
        /// INPUT:
        /// - id: 8, model: new StockAdjustmentDraftUpdateVM với 1 detail
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message từ exception
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID06_UpdateDraft_ServiceThrows_ReturnsBadRequest()
        {
            const int draftId = 8;
            var model = new StockAdjustmentDraftUpdateVM
            {
                Details = new List<StockAdjustmentDetailItemVM>
                {
                    new StockAdjustmentDetailItemVM
                    {
                        ProductId = 44,
                        ActualQuantity = 1
                    }
                }
            };

            _stockAdjustmentServiceMock
                .Setup(s => s.UpdateDraftAsync(draftId, It.IsAny<StockAdjustmentDraftUpdateVM>()))
                .ThrowsAsync(new Exception("Update failed"));

            var result = await _controller.UpdateDraft(draftId, model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Update failed", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region Resolve Tests

        /// <summary>
        /// TCID07: Resolve thành công
        /// 
        /// PRECONDITION:
        /// - Service trả về StockAdjustmentDraftResponseVM
        /// 
        /// INPUT:
        /// - id: 9
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với ApiResponse.Success = true
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID07_Resolve_Success_ReturnsOk()
        {
            const int draftId = 9;
            var draftResponse = new StockAdjustmentDraftResponseVM
            {
                AdjustmentId = draftId,
                WarehouseId = 3,
                WarehouseName = "Kho Resolve",
                Status = 2,
                StatusDescription = "Resolved"
            };

            _stockAdjustmentServiceMock
                .Setup(s => s.ResolveAsync(draftId))
                .ReturnsAsync(draftResponse);

            var result = await _controller.Resolve(draftId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Null(response.Error);
            Assert.Equal(200, response.StatusCode);
            Assert.Same(draftResponse, response.Data);
        }

        /// <summary>
        /// TCID08: Resolve khi service throw exception
        /// 
        /// PRECONDITION:
        /// - Service bất ngờ throw exception
        /// 
        /// INPUT:
        /// - id: 10
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message từ exception
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID08_Resolve_ServiceThrows_ReturnsBadRequest()
        {
            const int draftId = 10;

            _stockAdjustmentServiceMock
                .Setup(s => s.ResolveAsync(draftId))
                .ThrowsAsync(new Exception("Resolve failed"));

            var result = await _controller.Resolve(draftId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<StockAdjustmentDraftResponseVM>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Resolve failed", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

        #region DeleteDraft Tests

        /// <summary>
        /// TCID09: DeleteDraft thành công
        /// 
        /// PRECONDITION:
        /// - Service xóa draft thành công (trả về true)
        /// 
        /// INPUT:
        /// - id: 11
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với ApiResponse.Success = true, Data = true
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID09_DeleteDraft_Success_ReturnsOkWithTrue()
        {
            const int draftId = 11;

            _stockAdjustmentServiceMock
                .Setup(s => s.DeleteDraftAsync(draftId))
                .ReturnsAsync(true);

            var result = await _controller.DeleteDraft(draftId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Null(response.Error);
            Assert.Equal(200, response.StatusCode);
            Assert.True(response.Data);
        }

        /// <summary>
        /// TCID10: DeleteDraft khi service trả về false
        /// 
        /// PRECONDITION:
        /// - Service không thể xóa (trả về false)
        /// 
        /// INPUT:
        /// - id: 12
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: Ok với ApiResponse.Success = true, Data = false (không xóa được)
        /// - Type: A (Abnormal) – vẫn 200 nhưng dữ liệu báo không thực hiện
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID10_DeleteDraft_ReturnsOkWithFalse()
        {
            const int draftId = 12;

            _stockAdjustmentServiceMock
                .Setup(s => s.DeleteDraftAsync(draftId))
                .ReturnsAsync(false);

            var result = await _controller.DeleteDraft(draftId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Null(response.Error);
            Assert.Equal(200, response.StatusCode);
            Assert.False(response.Data);
        }

        /// <summary>
        /// TCID11: DeleteDraft khi service throw exception
        /// 
        /// PRECONDITION:
        /// - Service throw exception (ví dụ forbidden, database lỗi)
        /// 
        /// INPUT:
        /// - id: 13
        /// 
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message từ exception
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID11_DeleteDraft_ServiceThrows_ReturnsBadRequest()
        {
            const int draftId = 13;

            _stockAdjustmentServiceMock
                .Setup(s => s.DeleteDraftAsync(draftId))
                .ThrowsAsync(new Exception("Delete failed"));

            var result = await _controller.DeleteDraft(draftId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("Delete failed", response.Error.Message);
            Assert.Equal(400, response.StatusCode);
        }

        #endregion

    }
}
