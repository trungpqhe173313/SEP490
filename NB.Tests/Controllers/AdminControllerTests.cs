using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.AdminService;
using NB.Service.AdminService.Dto;
using NB.Service.Common;
using NB.Service.Core.EmailService;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.RoleService.Dto;
using NB.Service.UserRoleService;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using System.Security.Claims;
using Xunit;

namespace NB.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserRoleService> _mockUserRoleService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly AdminController _controller;

        // Reusable Test Data Constants
        private const int ValidUserId = 1;
        private const int NonExistentUserId = 999;
        private const int ValidRoleId = 1;
        private const int ValidPageIndex = 1;
        private const int ValidPageSize = 10;
        private const string ValidUsername = "employee123";
        private const string ValidEmail = "employee@example.com";
        private const string ValidFullName = "John Doe";

        public AdminControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockUserService = new Mock<IUserService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockLogger = new Mock<ILogger<AdminController>>();

            _controller = new AdminController(
                _mockAdminService.Object,
                _mockRoleService.Object,
                _mockUserService.Object,
                _mockUserRoleService.Object,
                _mockEmailService.Object,
                _mockCloudinaryService.Object,
                _mockLogger.Object
            );

            // Setup HttpContext with admin user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, ValidUserId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetAllAccounts Tests

        [Fact]
        // TCID1: Successfully retrieve paginated accounts
        public async Task GetAllAccounts_ValidFilter_ReturnsOkResultWithData()
        {
            // Arrange
            var filter = new AccountSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            var accounts = new List<AccountDto>
            {
                new AccountDto
                {
                    UserId = ValidUserId,
                    Username = ValidUsername,
                    Email = ValidEmail,
                    FullName = ValidFullName,
                    IsActive = true
                }
            };

            var pagedList = new PagedList<AccountDto>(accounts, ValidPageIndex, ValidPageSize, 1);

            _mockAdminService.Setup(x => x.GetData(filter)).ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetAllAccounts(filter);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<PagedList<AccountDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(1);
            apiResponse.Data.Items.First().UserId.Should().Be(ValidUserId);
        }

        [Fact]
        // TCID2: Handle service exception when retrieving accounts
        public async Task GetAllAccounts_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var filter = new AccountSearch
            {
                PageIndex = ValidPageIndex,
                PageSize = ValidPageSize
            };

            _mockAdminService.Setup(x => x.GetData(filter)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllAccounts(filter);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<PagedList<AccountDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region UpdateAccount Tests

        [Fact]
        // TCID3: Successfully update account
        public async Task UpdateAccount_ValidData_ReturnsOkResult()
        {
            // Arrange
            var dto = new UpdateAccountDto
            {
                FullName = ValidFullName,
                Email = ValidEmail
            };

            var response = new ApiResponse<bool>
            {
                Success = true,
                StatusCode = 200,
                Data = true
            };

            _mockAdminService.Setup(x => x.UpdateAccountAsync(ValidUserId, dto)).ReturnsAsync(response);

            // Act
            var result = await _controller.UpdateAccount(ValidUserId, dto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        // TCID4: Handle service exception when updating account
        public async Task UpdateAccount_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var dto = new UpdateAccountDto
            {
                FullName = ValidFullName,
                Email = ValidEmail
            };

            _mockAdminService.Setup(x => x.UpdateAccountAsync(ValidUserId, dto)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateAccount(ValidUserId, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
        }

        #endregion

        #region GetAllRoles Tests

        [Fact]
        // TCID5: Successfully retrieve all roles
        public async Task GetAllRoles_ReturnsOkResultWithRoles()
        {
            // Arrange
            var roles = new List<RoleDto>
            {
                new RoleDto { RoleId = ValidRoleId, RoleName = "Admin" },
                new RoleDto { RoleId = 2, RoleName = "Manager" },
                new RoleDto { RoleId = 3, RoleName = "Employee" }
            };

            _mockRoleService.Setup(x => x.GetAllRoles()).ReturnsAsync(roles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<List<RoleDto>>;
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(3);
            apiResponse.Data![0].RoleId.Should().Be(ValidRoleId);
        }

        [Fact]
        // TCID6: Handle service exception when retrieving roles
        public async Task GetAllRoles_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _mockRoleService.Setup(x => x.GetAllRoles()).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<List<RoleDto>>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region ResetUserPassword Tests

        [Fact]
        // TCID7: Successfully reset user password
        public async Task ResetUserPassword_ValidUserId_ReturnsOkResult()
        {
            // Arrange
            var dto = new ResetUserPasswordDto
            {
                UserId = ValidUserId,
                Password = "NewPassword123"
            };

            var response = new ApiResponse<string>
            {
                Success = true,
                StatusCode = 200,
                Data = "NewPassword123"
            };

            var userDto = new UserDto
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                Email = ValidEmail
            };

            _mockAdminService.Setup(x => x.ResetUserPasswordAsync(ValidUserId, dto.Password)).ReturnsAsync(response);
            _mockUserService.Setup(x => x.GetByUserId(ValidUserId)).ReturnsAsync(userDto);
            _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(ValidEmail, ValidUsername, "NewPassword123")).ReturnsAsync(true);

            // Act
            var result = await _controller.ResetUserPassword(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<string>;
            apiResponse!.Success.Should().BeTrue();
        }

        [Fact]
        // TCID8: Return bad request when user not found
        public async Task ResetUserPassword_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var dto = new ResetUserPasswordDto
            {
                UserId = NonExistentUserId,
                Password = "NewPassword123"
            };

            var response = new ApiResponse<string>
            {
                Success = true,
                StatusCode = 200,
                Data = "NewPassword123"
            };

            _mockAdminService.Setup(x => x.ResetUserPasswordAsync(NonExistentUserId, dto.Password)).ReturnsAsync(response);
            _mockUserService.Setup(x => x.GetByUserId(NonExistentUserId)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.ResetUserPassword(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<string>;
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        // TCID9: Handle service exception when resetting password
        public async Task ResetUserPassword_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var dto = new ResetUserPasswordDto
            {
                UserId = ValidUserId,
                Password = "NewPassword123"
            };

            _mockAdminService.Setup(x => x.ResetUserPasswordAsync(ValidUserId, dto.Password)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ResetUserPassword(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<string>;
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion
    }
}
