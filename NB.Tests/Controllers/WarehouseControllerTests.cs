using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using NB.Service.WarehouseService.ViewModels;

namespace NB.Tests.Controllers
{
    public class WarehouseControllerTests
    {
        private readonly Mock<IWarehouseService> _mockWarehouseService;
        private readonly Mock<ILogger<WarehouseController>> _mockLogger;
        private readonly WarehouseController _controller;

        public WarehouseControllerTests()
        {
            _mockWarehouseService = new Mock<IWarehouseService>();
            _mockLogger = new Mock<ILogger<WarehouseController>>();
            _controller = new WarehouseController(_mockWarehouseService.Object, _mockLogger.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: Kiem tra lay danh sach kho hang voi tim kiem hop le
        /// Input: WarehouseSearch voi PageIndex 1, PageSize 10, WarehouseName warehouse
        /// Expected: Tra ve OkResult voi danh sach 2 kho hang, Success true, Items co 2 phan tu
        /// </summary>
        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new WarehouseSearch
            {
                PageIndex = 1,
                PageSize = 10,
                WarehouseName = "warehouse"
            };

            var warehouses = new List<WarehouseDto>
            {
                new WarehouseDto
                {
                    WarehouseId = 1,
                    WarehouseName = "Warehouse 1",
                    Location = "Location 1",
                    Capacity = 1000,
                    Status = 1
                },
                new WarehouseDto
                {
                    WarehouseId = 2,
                    WarehouseName = "Warehouse 2",
                    Location = "Location 2",
                    Capacity = 2000,
                    Status = 1
                }
            };

            var pagedList = new PagedList<WarehouseDto>(warehouses, 2, 1, 10);

            _mockWarehouseService
                .Setup(x => x.GetData(It.IsAny<WarehouseSearch>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<WarehouseDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(2);
            response.Data.Items.First().WarehouseName.Should().Be("Warehouse 1");
        }

        /// <summary>
        /// TCID02: Kiem tra lay danh sach kho hang khi service nem ngoai le
        /// Input: WarehouseSearch voi PageIndex 1, PageSize 10, service nem exception Database error
        /// Expected: Tra ve BadRequestResult, Success false, Error Message Co loi xay ra
        /// </summary>
        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new WarehouseSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockWarehouseService
                .Setup(x => x.GetData(It.IsAny<WarehouseSearch>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<WarehouseDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Có lỗi xảy ra");
        }

        #endregion

        #region GetById Tests

        /// <summary>
        /// TCID03: Kiem tra lay thong tin kho hang theo ID hop le
        /// Input: WarehouseId 1, service tra ve WarehouseDto hop le
        /// Expected: Tra ve OkResult, Success true, Data chua thong tin kho hang voi ID 1
        /// </summary>
        [Fact]
        public async Task GetById_WithValidId_ReturnsOkWithWarehouse()
        {
            // Arrange
            var warehouseId = 1;
            var warehouse = new WarehouseDto
            {
                WarehouseId = warehouseId,
                WarehouseName = "Warehouse 1",
                Location = "Location 1",
                Capacity = 1000,
                Status = 1
            };

            _mockWarehouseService
                .Setup(x => x.GetById(warehouseId))
                .ReturnsAsync(warehouse);

            // Act
            var result = await _controller.GetById(warehouseId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<WarehouseDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.WarehouseId.Should().Be(warehouseId);
            response.Data.WarehouseName.Should().Be("Warehouse 1");
        }

        /// <summary>
        /// TCID04: Kiem tra lay thong tin kho hang voi ID khong ton tai
        /// Input: WarehouseId 999, service tra ve null
        /// Expected: Tra ve NotFoundResult, Success false, Error Message Khong tim thay kho
        /// </summary>
        [Fact]
        public async Task GetById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var warehouseId = 999;

            _mockWarehouseService
                .Setup(x => x.GetById(warehouseId))
                .ReturnsAsync((WarehouseDto?)null);

            // Act
            var result = await _controller.GetById(warehouseId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<WarehouseDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Message.Should().Be("Không tìm thấy kho");
        }

        /// <summary>
        /// TCID05: Kiem tra lay thong tin kho hang khi service nem ngoai le
        /// Input: WarehouseId 1, service nem exception Database error
        /// Expected: Tra ve BadRequestResult, Success false, Error Message Co loi xay ra
        /// </summary>
        [Fact]
        public async Task GetById_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var warehouseId = 1;

            _mockWarehouseService
                .Setup(x => x.GetById(warehouseId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetById(warehouseId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<WarehouseDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra");
        }

        #endregion

        #region Create Tests

        /// <summary>
        /// TCID06: Kiem tra tao moi kho hang voi du lieu hop le
        /// Input: WarehouseCreateVM voi WarehouseName New Warehouse, Location New Location, Capacity 1500, Status 1
        /// Expected: Tra ve OkResult, Success true, Data chua thong tin kho hang vua tao
        /// </summary>
        [Fact]
        public async Task Create_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new WarehouseCreateVM
            {
                WarehouseName = "New Warehouse",
                Location = "New Location",
                Capacity = 1500,
                Status = 1,
                Note = "Test note"
            };

            _mockWarehouseService
                .Setup(x => x.CreateAsync(It.IsAny<WarehouseDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<WarehouseDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.WarehouseName.Should().Be("New Warehouse");
            response.Data.Location.Should().Be("New Location");
            response.Data.Capacity.Should().Be(1500);
        }

        /// <summary>
        /// TCID07: Kiem tra tao moi kho hang khi service nem ngoai le
        /// Input: WarehouseCreateVM hop le, service nem exception Database error
        /// Expected: Tra ve BadRequestResult, Success false, Error Message Co loi xay ra khi tao kho
        /// </summary>
        [Fact]
        public async Task Create_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var model = new WarehouseCreateVM
            {
                WarehouseName = "New Warehouse",
                Location = "New Location",
                Capacity = 1500,
                Status = 1
            };

            _mockWarehouseService
                .Setup(x => x.CreateAsync(It.IsAny<WarehouseDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<WarehouseDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi tạo kho");
        }

        #endregion

        #region Update Tests

        /// <summary>
        /// TCID08: Kiem tra cap nhat kho hang voi du lieu hop le
        /// Input: WarehouseId 1, WarehouseUpdateVM voi WarehouseName Updated Warehouse, Location Updated Location, Capacity 2000
        /// Expected: Tra ve OkResult, Success true, Data chua thong tin kho hang da cap nhat
        /// </summary>
        [Fact]
        public async Task Update_WithValidData_ReturnsOk()
        {
            // Arrange
            var warehouseId = 1;
            var model = new WarehouseUpdateVM
            {
                WarehouseName = "Updated Warehouse",
                Location = "Updated Location",
                Capacity = 2000,
                Status = 1,
                Note = "Updated note"
            };

            var existingWarehouse = new Warehouse
            {
                WarehouseId = warehouseId,
                WarehouseName = "Old Warehouse",
                Location = "Old Location",
                Capacity = 1000,
                Status = 1
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(warehouseId))
                .ReturnsAsync(existingWarehouse);

            _mockWarehouseService
                .Setup(x => x.UpdateAsync(It.IsAny<Warehouse>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(warehouseId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<Warehouse>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.WarehouseName.Should().Be("Updated Warehouse");
            response.Data.Location.Should().Be("Updated Location");
            response.Data.Capacity.Should().Be(2000);
        }

        /// <summary>
        /// TCID09: Kiem tra cap nhat kho hang voi ID khong ton tai
        /// Input: WarehouseId 999, WarehouseUpdateVM hop le, service tra ve null
        /// Expected: Tra ve NotFoundResult, Success false, Error Message Khong tim thay kho hang
        /// </summary>
        [Fact]
        public async Task Update_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var warehouseId = 999;
            var model = new WarehouseUpdateVM
            {
                WarehouseName = "Updated Warehouse",
                Location = "Updated Location",
                Capacity = 2000,
                Status = 1
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(warehouseId))
                .ReturnsAsync((Warehouse?)null);

            // Act
            var result = await _controller.Update(warehouseId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Không tìm thấy kho hàng");
        }

        /// <summary>
        /// TCID10: Kiem tra cap nhat kho hang khi service nem ngoai le
        /// Input: WarehouseId 1, WarehouseUpdateVM hop le, service nem exception Database error
        /// Expected: Tra ve BadRequestResult, Success false, Error Message Database error
        /// </summary>
        [Fact]
        public async Task Update_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var warehouseId = 1;
            var model = new WarehouseUpdateVM
            {
                WarehouseName = "Updated Warehouse",
                Location = "Updated Location",
                Capacity = 2000,
                Status = 1
            };

            var existingWarehouse = new Warehouse
            {
                WarehouseId = warehouseId,
                WarehouseName = "Old Warehouse"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(warehouseId))
                .ReturnsAsync(existingWarehouse);

            _mockWarehouseService
                .Setup(x => x.UpdateAsync(It.IsAny<Warehouse>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Update(warehouseId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Database error");
        }

        #endregion

        #region Delete Tests

        /// <summary>
        /// TCID11: Kiem tra xoa kho hang voi ID hop le
        /// Input: WarehouseId 1, service tra ve Warehouse hop le
        /// Expected: Tra ve OkResult, Success true, Data true
        /// </summary>
        [Fact]
        public async Task Delete_WithValidId_ReturnsOk()
        {
            // Arrange
            var warehouseId = 1;
            var warehouse = new Warehouse
            {
                WarehouseId = warehouseId,
                WarehouseName = "Warehouse to Delete"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouse);

            _mockWarehouseService
                .Setup(x => x.DeleteAsync(It.IsAny<Warehouse>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(warehouseId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        /// <summary>
        /// TCID12: Kiem tra xoa kho hang voi ID khong ton tai
        /// Input: WarehouseId 999, service tra ve null
        /// Expected: Tra ve NotFoundResult, Success false, Error Message Khong tim thay kho de xoa
        /// </summary>
        [Fact]
        public async Task Delete_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var warehouseId = 999;

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(warehouseId))
                .ReturnsAsync((Warehouse?)null);

            // Act
            var result = await _controller.Delete(warehouseId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy kho để xóa");
        }

        /// <summary>
        /// TCID13: Kiem tra xoa kho hang khi service nem ngoai le
        /// Input: WarehouseId 1, service tra ve Warehouse hop le nhung DeleteAsync nem exception
        /// Expected: Tra ve BadRequestResult, Success false, Error Message Cannot delete warehouse with existing inventory
        /// </summary>
        [Fact]
        public async Task Delete_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var warehouseId = 1;
            var warehouse = new Warehouse
            {
                WarehouseId = warehouseId,
                WarehouseName = "Warehouse to Delete"
            };

            _mockWarehouseService
                .Setup(x => x.GetByIdAsync(warehouseId))
                .ReturnsAsync(warehouse);

            _mockWarehouseService
                .Setup(x => x.DeleteAsync(It.IsAny<Warehouse>()))
                .ThrowsAsync(new Exception("Cannot delete warehouse with existing inventory"));

            // Act
            var result = await _controller.Delete(warehouseId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Cannot delete warehouse with existing inventory");
        }

        #endregion

        #region DownloadTemplate Tests

        /// <summary>
        /// TCID14: Kiem tra tai xuong file mau kho hang
        /// Input: Khong co tham so dau vao
        /// Expected: Tra ve FileStreamResult voi ContentType xlsx, FileDownloadName chua Warehouse Import Template
        /// </summary>
        [Fact]
        public void DownloadTemplate_ReturnsFileResult()
        {
            // Act
            var result = _controller.DownloadTemplate();

            // Assert
            var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().Contain("Warehouse_Import_Template_");
            fileResult.FileDownloadName.Should().EndWith(".xlsx");
        }

        #endregion
    }
}
