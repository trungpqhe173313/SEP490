using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NB.API.Controllers;
using NB.Service.Dto;
using NB.Service.IoTDeviceService;
using NB.Service.IoTDeviceService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class IoTDeviceControllerTests
    {
        private readonly Mock<IIoTDeviceService> _mockIoTDeviceService;
        private readonly IoTDeviceController _controller;

        // Test Data Constants
        private const int ValidUserId = 1;

        public IoTDeviceControllerTests()
        {
            _mockIoTDeviceService = new Mock<IIoTDeviceService>();
            _controller = new IoTDeviceController(_mockIoTDeviceService.Object);

            // Setup HttpContext with user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, ValidUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetAllDevices Tests

        [Fact]
        public async Task GetAllDevices_Success_ReturnsOkWithDeviceList()
        {
            // Arrange
            var devices = new List<DeviceListDto>
            {
                new DeviceListDto { DeviceCode = "IOT001", DeviceName = "Temperature Sensor 1" },
                new DeviceListDto { DeviceCode = "IOT002", DeviceName = "Weight Scale 1" },
                new DeviceListDto { DeviceCode = "IOT003", DeviceName = "Humidity Sensor 1" }
            };

            var apiResponse = ApiResponse<List<DeviceListDto>>.Ok(devices);

            _mockIoTDeviceService.Setup(x => x.GetAllDevicesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as List<DeviceListDto>;
            data.Should().NotBeNull();
            data.Should().HaveCount(3);
            data![0].DeviceCode.Should().Be("IOT001");
        }

        [Fact]
        public async Task GetAllDevices_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            var devices = new List<DeviceListDto>();
            var apiResponse = ApiResponse<List<DeviceListDto>>.Ok(devices);

            _mockIoTDeviceService.Setup(x => x.GetAllDevicesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as List<DeviceListDto>;
            data.Should().NotBeNull();
            data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllDevices_ServiceReturnsError_ReturnsStatusCodeWithError()
        {
            // Arrange
            var apiResponse = ApiResponse<List<DeviceListDto>>.Fail("Database connection error", 500);

            _mockIoTDeviceService.Setup(x => x.GetAllDevicesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetAllDevices_ServiceReturnsNotFound_ReturnsStatusCode404()
        {
            // Arrange
            var apiResponse = ApiResponse<List<DeviceListDto>>.Fail("No devices found", 404);

            _mockIoTDeviceService.Setup(x => x.GetAllDevicesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetAllDevices_SingleDevice_ReturnsOkWithSingleDevice()
        {
            // Arrange
            var devices = new List<DeviceListDto>
            {
                new DeviceListDto { DeviceCode = "IOT001", DeviceName = "Production Scale" }
            };

            var apiResponse = ApiResponse<List<DeviceListDto>>.Ok(devices);

            _mockIoTDeviceService.Setup(x => x.GetAllDevicesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var data = okResult!.Value as List<DeviceListDto>;
            data.Should().HaveCount(1);
            data![0].DeviceName.Should().Be("Production Scale");
        }

        [Fact]
        public async Task GetAllDevices_ServiceReturnsUnauthorized_ReturnsStatusCode401()
        {
            // Arrange
            var apiResponse = ApiResponse<List<DeviceListDto>>.Fail("Unauthorized access", 401);

            _mockIoTDeviceService.Setup(x => x.GetAllDevicesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
        }

        #endregion
    }
}
