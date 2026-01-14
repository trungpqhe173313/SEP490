using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NB.API.Controllers;
using NB.Service.Dto;
using NB.Service.ProductionIotService.Dto;
using NB.Services.ProductionIotService;
using Xunit;

namespace NB.Test.Controllers
{
    public class ProductionIotControllerTest
    {
        private readonly Mock<IProductionIotService> _productionIotServiceMock;
        private readonly ProductionIotController _controller;

        public ProductionIotControllerTest()
        {
            _productionIotServiceMock = new Mock<IProductionIotService>();
            _controller = new ProductionIotController(_productionIotServiceMock.Object);
        }

        #region GetCurrentProduction Tests

        /// <summary>
        /// TCID01: GetCurrentProduction với deviceCode trống
        ///
        /// PRECONDITION:
        /// - deviceCode = string.Empty
        ///
        /// INPUT:
        /// - deviceCode: ""
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Device code is required"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_GetCurrentProduction_WithEmptyDeviceCode_ReturnsBadRequest()
        {
            // Arrange - INPUT: deviceCode = empty string
            string deviceCode = string.Empty;

            // Act
            var result = await _controller.GetCurrentProduction(deviceCode);

            // Assert - EXPECTED OUTPUT: BadRequest with message "Device code is required"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);

            var messageProperty = badRequestResult.Value?
                .GetType()
                .GetProperty("message", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(messageProperty);
            Assert.Equal("Device code is required", messageProperty.GetValue(badRequestResult.Value));

            _productionIotServiceMock.Verify(s => s.GetCurrentProductionAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// TCID02: GetCurrentProduction khi service trả về thất bại
        ///
        /// PRECONDITION:
        /// - deviceCode hợp lệ
        /// - Dịch vụ trả về ApiResponse.Fail với success = false
        ///
        /// INPUT:
        /// - deviceCode: "SCALE_01"
        ///
        /// EXPECTED OUTPUT:
        /// - Return: StatusCode (503) với ApiResponse.Fail
        /// - Type: A (Abnormal)
        /// - Status: 503 Service Unavailable
        /// </summary>
        [Fact]
        public async Task TCID02_GetCurrentProduction_ServiceFailure_ReturnsServiceStatusCode()
        {
            // Arrange - INPUT: deviceCode = "SCALE_01"
            string deviceCode = "SCALE_01";
            var failureResponse = ApiResponse<CurrentProductionResponseDto>.Fail("Device offline", 503);

            _productionIotServiceMock
                .Setup(s => s.GetCurrentProductionAsync(deviceCode))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _controller.GetCurrentProduction(deviceCode);

            // Assert - EXPECTED OUTPUT: StatusCode (503) với ApiResponse.Fail
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(failureResponse.StatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse<CurrentProductionResponseDto>>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(failureResponse.Error?.Message, response.Error?.Message);

            _productionIotServiceMock.Verify(s => s.GetCurrentProductionAsync(deviceCode), Times.Once);
        }

        /// <summary>
        /// TCID03: GetCurrentProduction thành công
        ///
        /// PRECONDITION:
        /// - deviceCode hợp lệ
        /// - Dịch vụ trả về ApiResponse.Ok
        ///
        /// INPUT:
        /// - deviceCode: "SCALE_02"
        ///
        /// EXPECTED OUTPUT:
        /// - Return: Ok với CurrentProductionResponseDto
        /// - Type: N (Normal)
        /// - Status: 200 OK
        /// </summary>
        [Fact]
        public async Task TCID03_GetCurrentProduction_WithValidDevice_ReturnsCurrentProductionData()
        {
            // Arrange - INPUT: deviceCode = "SCALE_02"
            string deviceCode = "SCALE_02";
            var currentProduction = new CurrentProductionResponseDto
            {
                Status = "Running",
                ProductionId = 123,
                Products = new List<ProductItemDto>
                {
                    new()
                    {
                        ProductId = 321,
                        ProductName = "Gạo thơm",
                        TargetWeight = 50m
                    }
                }
            };

            var successResponse = ApiResponse<CurrentProductionResponseDto>.Ok(currentProduction);

            _productionIotServiceMock
                .Setup(s => s.GetCurrentProductionAsync(deviceCode))
                .ReturnsAsync(successResponse);

            // Act
            var result = await _controller.GetCurrentProduction(deviceCode);

            // Assert - EXPECTED OUTPUT: Ok with CurrentProductionResponseDto
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var responseData = Assert.IsType<CurrentProductionResponseDto>(okResult.Value);
            Assert.Equal(currentProduction.ProductionId, responseData.ProductionId);
            Assert.Equal(currentProduction.Status, responseData.Status);
            Assert.Single(responseData.Products);
            Assert.Equal(currentProduction.Products[0].ProductName, responseData.Products[0].ProductName);

            _productionIotServiceMock.Verify(s => s.GetCurrentProductionAsync(deviceCode), Times.Once);
        }

        #endregion

        #region SubmitPackage Tests

        /// <summary>
        /// TCID01: SubmitPackage với dữ liệu không hợp lệ (ModelState invalid)
        ///
        /// PRECONDITION:
        /// - Request thiếu DeviceCode
        /// - ModelState bị đánh dấu lỗi
        ///
        /// INPUT:
        /// - request: new PackageSubmitRequestDto()
        ///
        /// EXPECTED OUTPUT:
        /// - Return: BadRequest với message "Invalid request data"
        /// - Type: A (Abnormal)
        /// - Status: 400 Bad Request
        /// </summary>
        [Fact]
        public async Task TCID01_SubmitPackage_ModelStateInvalid_ReturnsBadRequest()
        {
            // Arrange - INPUT: request missing required fields
            var request = new PackageSubmitRequestDto();
            _controller.ModelState.AddModelError(nameof(PackageSubmitRequestDto.DeviceCode), "Device code is required");

            // Act
            var result = await _controller.SubmitPackage(request);

            // Assert - EXPECTED OUTPUT: BadRequest with message "Invalid request data"
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);

            var responseValue = badRequestResult.Value;
            Assert.NotNull(responseValue);

            var successProp = responseValue.GetType().GetProperty("success", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var messageProp = responseValue.GetType().GetProperty("message", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var errorsProp = responseValue.GetType().GetProperty("errors", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(successProp);
            Assert.False((bool)successProp.GetValue(responseValue)!);
            Assert.NotNull(messageProp);
            Assert.Equal("Invalid request data", messageProp.GetValue(responseValue));

            Assert.NotNull(errorsProp);
            var errors = Assert.IsAssignableFrom<IEnumerable<string>>(errorsProp.GetValue(responseValue)!);
            Assert.Contains("Device code is required", errors);

            _productionIotServiceMock.Verify(s => s.SubmitPackageAsync(It.IsAny<PackageSubmitRequestDto>()), Times.Never);
        }

        /// <summary>
        /// TCID02: SubmitPackage khi service trả về kết quả thất bại
        ///
        /// PRECONDITION:
        /// - Request hợp lệ
        /// - Service trả về ApiResponse.Fail
        ///
        /// INPUT:
        /// - request: có deviceCode, productionId, productId, weight
        ///
        /// EXPECTED OUTPUT:
        /// - Return: StatusCode theo ApiResponse.Fail
        /// - Type: A (Abnormal)
        /// - Status: theo ApiResponse.Fail (ví dụ 503)
        /// </summary>
        [Fact]
        public async Task TCID02_SubmitPackage_ServiceFailure_ReturnsStatusCodeWithError()
        {
            // Arrange - INPUT: valid request
            var request = new PackageSubmitRequestDto
            {
                DeviceCode = "SCALE_01",
                ProductionId = 10,
                ProductId = 20,
                Weight = 5m
            };

            var failureResponse = ApiResponse<PackageSubmitResponseDto>.Fail("Production is not active", 503);

            _productionIotServiceMock
                .Setup(s => s.SubmitPackageAsync(It.Is<PackageSubmitRequestDto>(r =>
                    r.DeviceCode == request.DeviceCode &&
                    r.ProductionId == request.ProductionId &&
                    r.ProductId == request.ProductId &&
                    r.Weight == request.Weight)))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _controller.SubmitPackage(request);

            // Assert - EXPECTED OUTPUT: StatusCode Failure with ApiResponse error
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(failureResponse.StatusCode, objectResult.StatusCode);

            var responseValue = objectResult.Value;
            Assert.NotNull(responseValue);

            var successProp = responseValue.GetType().GetProperty("success", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var messageProp = responseValue.GetType().GetProperty("message", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(successProp);
            Assert.False((bool)successProp.GetValue(responseValue)!);
            Assert.NotNull(messageProp);
            Assert.Equal(failureResponse.Error?.Message, messageProp.GetValue(responseValue));

            _productionIotServiceMock.Verify(s => s.SubmitPackageAsync(It.IsAny<PackageSubmitRequestDto>()), Times.Once);
        }

        /// <summary>
        /// TCID03: SubmitPackage thành công
        ///
        /// PRECONDITION:
        /// - Request hợp lệ
        /// - Service trả về ApiResponse.Ok với dữ liệu hoàn chỉnh
        ///
        /// INPUT:
        /// - request: hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - Return: StatusCode 201 với payload success
        /// - Type: N (Normal)
        /// - Status: 201 Created
        /// </summary>
        [Fact]
        public async Task TCID03_SubmitPackage_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange - INPUT: valid request
            var request = new PackageSubmitRequestDto
            {
                DeviceCode = "SCALE_02",
                ProductionId = 99,
                ProductId = 123,
                Weight = 10m
            };

            var responseDto = new PackageSubmitResponseDto
            {
                ProductionId = request.ProductionId,
                ProductId = request.ProductId,
                BagIndex = 1,
                ActualWeight = request.Weight,
                TargetWeight = 15m
            };

            var successResponse = ApiResponse<PackageSubmitResponseDto>.Ok(responseDto);
            successResponse.StatusCode = 201;

            _productionIotServiceMock
                .Setup(s => s.SubmitPackageAsync(It.Is<PackageSubmitRequestDto>(r =>
                    r.DeviceCode == request.DeviceCode &&
                    r.ProductionId == request.ProductionId &&
                    r.ProductId == request.ProductId)))
                .ReturnsAsync(successResponse);

            // Act
            var result = await _controller.SubmitPackage(request);

            // Assert - EXPECTED OUTPUT: StatusCode 201 with success payload
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, objectResult.StatusCode);

            var responseValue = objectResult.Value;
            Assert.NotNull(responseValue);

            var successProp = responseValue.GetType().GetProperty("success", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var messageProp = responseValue.GetType().GetProperty("message", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var dataProp = responseValue.GetType().GetProperty("data", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(successProp);
            Assert.True((bool)successProp.GetValue(responseValue)!);
            Assert.NotNull(messageProp);
            Assert.Equal("Package submitted successfully", messageProp.GetValue(responseValue));

            Assert.NotNull(dataProp);
            var returnedData = Assert.IsType<PackageSubmitResponseDto>(dataProp.GetValue(responseValue)!);
            Assert.Equal(responseDto.ProductionId, returnedData.ProductionId);
            Assert.Equal(responseDto.ProductId, returnedData.ProductId);
            Assert.Equal(responseDto.BagIndex, returnedData.BagIndex);

            _productionIotServiceMock.Verify(s => s.SubmitPackageAsync(It.IsAny<PackageSubmitRequestDto>()), Times.Once);
        }

        #endregion
    }
}
