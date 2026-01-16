using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<Supplier>> _mockLogger;
        private readonly UserController _controller;

        // Reusable Test Data Constants
        private const int ValidUserId = 1;
        private const int ValidPageIndex = 1;
        private const int ValidPageSize = 10;
        private const string ValidFullName = "Test User";
        private const string ValidEmail = "testuser@example.com";

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<Supplier>>();
            _controller = new UserController(_mockUserService.Object, _mockMapper.Object, _mockLogger.Object);

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

        #region GetData Tests

        [Fact]
        // TCID1: Successfully retrieve paginated users
        public async Task GetData_ValidFilter_ReturnsOkResultWithData()
        {
            // Arrange
            var filter = new UserSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = ValidUserId,
                    FullName = ValidFullName,
                    Email = ValidEmail,
                    IsActive = true
                }
            };

            var pagedList = new PagedList<UserDto>(users, ValidPageIndex, ValidPageSize, 1);

            _mockUserService.Setup(x => x.GetData(filter)).ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(filter);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<UserDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(1);
            apiResponse.Data.Items.First().UserId.Should().Be(ValidUserId);
        }

        [Fact]
        // TCID2: Retrieve users with email filter
        public async Task GetData_WithEmailFilter_ReturnsMatchingUsers()
        {
            // Arrange
            var filter = new UserSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize,
                Email = ValidEmail
            };

            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = ValidUserId,
                    FullName = ValidFullName,
                    Email = ValidEmail,
                    IsActive = true
                }
            };

            var pagedList = new PagedList<UserDto>(users, ValidPageIndex, ValidPageSize, 1);

            _mockUserService.Setup(x => x.GetData(filter)).ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(filter);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<UserDto>>;
            apiResponse!.Data!.Items.First().Email.Should().Be(ValidEmail);
        }

        [Fact]
        // TCID3: Retrieve users with full name filter
        public async Task GetData_WithFullNameFilter_ReturnsMatchingUsers()
        {
            // Arrange
            var filter = new UserSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize,
                FullName = ValidFullName
            };

            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = ValidUserId,
                    FullName = ValidFullName,
                    Email = ValidEmail,
                    IsActive = true
                }
            };

            var pagedList = new PagedList<UserDto>(users, ValidPageIndex, ValidPageSize, 1);

            _mockUserService.Setup(x => x.GetData(filter)).ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(filter);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<UserDto>>;
            apiResponse!.Data!.Items.First().FullName.Should().Be(ValidFullName);
        }

        [Fact]
        // TCID4: Retrieve only active users
        public async Task GetData_WithIsActiveFilter_ReturnsActiveUsers()
        {
            // Arrange
            var filter = new UserSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize,
                IsActive = true
            };

            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = ValidUserId,
                    FullName = ValidFullName,
                    Email = ValidEmail,
                    IsActive = true
                }
            };

            var pagedList = new PagedList<UserDto>(users, ValidPageIndex, ValidPageSize, 1);

            _mockUserService.Setup(x => x.GetData(filter)).ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(filter);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<UserDto>>;
            apiResponse!.Data!.Items.First().IsActive.Should().BeTrue();
        }

        [Fact]
        // TCID5: Retrieve empty list when no users match filter
        public async Task GetData_NoMatchingUsers_ReturnsEmptyList()
        {
            // Arrange
            var filter = new UserSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize,
                Email = "nonexistent@example.com"
            };

            var pagedList = new PagedList<UserDto>(new List<UserDto>(), ValidPageIndex, ValidPageSize, 0);

            _mockUserService.Setup(x => x.GetData(filter)).ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetData(filter);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<UserDto>>;
            apiResponse!.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        // TCID6: Handle service exception
        public async Task GetData_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var filter = new UserSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            _mockUserService.Setup(x => x.GetData(filter)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(filter);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<PagedList<UserDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion
    }
}
