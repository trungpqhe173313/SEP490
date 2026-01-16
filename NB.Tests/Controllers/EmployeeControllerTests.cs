using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.RoleService.Dto;
using NB.Service.UserRoleService;
using NB.Service.UserRoleService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;

namespace NB.Tests.Controllers
{
    public class EmployeeControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserRoleService> _mockUserRoleService;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<ILogger<EmployeeController>> _mockLogger;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly EmployeeController _controller;

        // Reusable Test Data Constants
        private const int ValidUserId = 1;
        private const int NonExistentUserId = 999;
        private const int ValidRoleId = 1;
        private const string ValidUsername = "employee1";
        private const string ValidEmail = "employee1@example.com";
        private const string ValidFullName = "John Employee";
        private const string ValidPhone = "0123456789";
        private const string ValidPassword = "123";
        private const string ExistingEmail = "existing@example.com";
        private const string ExistingUsername = "existinguser";
        private const string ValidImageUrl = "https://example.com/image.jpg";
        private const string RoleName = "Employee";

        public EmployeeControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockLogger = new Mock<ILogger<EmployeeController>>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockMapper = new Mock<IMapper>();
            _controller = new EmployeeController(
                _mockUserService.Object,
                _mockUserRoleService.Object,
                _mockRoleService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockCloudinaryService.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: Lay danh sach nhan vien thanh cong
        /// Input: UserSearch voi PageIndex = 1, PageSize = 10
        /// Expected: HTTP 200 OK, Success = true, tra ve PagedList<UserDto>
        /// </summary>
        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new UserSearch { PageIndex = 1, PageSize = 10 };
            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = ValidUserId,
                    Username = ValidUsername,
                    FullName = ValidFullName,
                    Email = ValidEmail
                }
            };

            var role = new RoleDto { RoleId = ValidRoleId, RoleName = RoleName };
            var userRoles = new List<UserRole>
            {
                new UserRole { UserId = ValidUserId, RoleId = ValidRoleId }
            };

            _mockUserService.Setup(x => x.GetAllUser(search)).ReturnsAsync(users);
            _mockRoleService.Setup(x => x.GetByRoleName(RoleName)).ReturnsAsync(role);
            _mockUserRoleService.Setup(x => x.GetByRoleId(ValidRoleId)).ReturnsAsync(userRoles);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().HaveCount(1);
            response.Data.Items.First().RoleName.Should().Be(RoleName);
        }

        /// <summary>
        /// TCID02: Lay danh sach nhan vien khi service nem ngoai le
        /// Input: UserSearch hop le, service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi lấy dữ liệu"
        /// </summary>
        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new UserSearch { PageIndex = 1, PageSize = 10 };
            _mockUserService.Setup(x => x.GetAllUser(search)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<UserDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy dữ liệu");
        }

        #endregion

        #region GetByUserId Tests

        /// <summary>
        /// TCID03: Lay nhan vien theo UserId hop le
        /// Input: id = ValidUserId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve UserDto voi RoleName = Employee
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithValidId_ReturnsOkWithUser()
        {
            // Arrange
            var user = new UserDto
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                FullName = ValidFullName,
                Email = ValidEmail
            };

            var role = new RoleDto { RoleId = ValidRoleId, RoleName = RoleName };
            var userRoles = new List<UserRole>
            {
                new UserRole { UserId = ValidUserId, RoleId = ValidRoleId }
            };

            _mockUserService.Setup(x => x.GetByUserId(ValidUserId)).ReturnsAsync(user);
            _mockRoleService.Setup(x => x.GetByRoleName(RoleName)).ReturnsAsync(role);
            _mockUserRoleService.Setup(x => x.GetByRoleId(ValidRoleId)).ReturnsAsync(userRoles);

            // Act
            var result = await _controller.GetByUserId(ValidUserId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.UserId.Should().Be(ValidUserId);
            response.Data.RoleName.Should().Be(RoleName);
        }

        /// <summary>
        /// TCID04: Lay nhan vien voi UserId khong ton tai
        /// Input: id = NonExistentUserId (999)
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy nhân viên"
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetByUserId(NonExistentUserId)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetByUserId(NonExistentUserId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy nhân viên");
        }

        /// <summary>
        /// TCID05: Lay user khong phai nhan vien
        /// Input: id = ValidUserId (1), user khong co role Employee
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Người dùng không phải nhân viên"
        /// </summary>
        [Fact]
        public async Task GetByUserId_WithNonEmployeeUser_ReturnsBadRequest()
        {
            // Arrange
            var user = new UserDto { UserId = ValidUserId, Username = ValidUsername };
            var role = new RoleDto { RoleId = ValidRoleId, RoleName = RoleName };
            var userRoles = new List<UserRole>(); // Empty - user not in Employee role

            _mockUserService.Setup(x => x.GetByUserId(ValidUserId)).ReturnsAsync(user);
            _mockRoleService.Setup(x => x.GetByRoleName(RoleName)).ReturnsAsync(role);
            _mockUserRoleService.Setup(x => x.GetByRoleId(ValidRoleId)).ReturnsAsync(userRoles);

            // Act
            var result = await _controller.GetByUserId(ValidUserId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<UserDto>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Người dùng không phải nhân viên");
        }

        #endregion

        #region CreateEmployee Tests

        /// <summary>
        /// TCID06: Tao nhan vien moi thanh cong
        /// Input: UserCreateVM voi Username = ValidUsername, Email = ValidEmail
        /// Expected: HTTP 200 OK, Success = true, tra ve User da tao
        /// </summary>
        [Fact]
        public async Task CreateEmployee_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new UserCreateVM
            {
                Username = ValidUsername,
                Email = ValidEmail,
                FullName = ValidFullName,
                Phone = ValidPhone
            };

            var user = new User
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                Email = ValidEmail,
                Password = ValidPassword,
                IsActive = true
            };

            var role = new RoleDto { RoleId = ValidRoleId, RoleName = RoleName };

            _mockUserService.Setup(x => x.GetByEmail(ValidEmail)).ReturnsAsync((UserDto?)null);
            _mockUserService.Setup(x => x.GetByUsername(ValidUsername)).ReturnsAsync((UserDto?)null);
            _mockMapper.Setup(x => x.Map<UserCreateVM, User>(model)).Returns(user);
            _mockUserService.Setup(x => x.CreateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRoleService.Setup(x => x.GetByRoleName(RoleName)).ReturnsAsync(role);
            _mockMapper.Setup(x => x.Map<UserRoleCreateVM, UserRole>(It.IsAny<UserRoleCreateVM>())).Returns(new UserRole());
            _mockUserRoleService.Setup(x => x.CreateAsync(It.IsAny<UserRole>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateEmployee(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Username.Should().Be(ValidUsername);
        }

        /// <summary>
        /// TCID07: Tao nhan vien voi email da ton tai
        /// Input: UserCreateVM voi Email = ExistingEmail
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Email đã tồn tại"
        /// </summary>
        [Fact]
        public async Task CreateEmployee_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var model = new UserCreateVM
            {
                Username = ValidUsername,
                Email = ExistingEmail,
                FullName = ValidFullName
            };

            var existingUser = new User { UserId = 2, Email = ExistingEmail };

            var existingUserDto = new UserDto { UserId = 2, Email = ExistingEmail };
            _mockUserService.Setup(x => x.GetByEmail(ExistingEmail)).ReturnsAsync(existingUserDto);

            // Act
            var result = await _controller.CreateEmployee(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Email đã tồn tại");
        }

        /// <summary>
        /// TCID08: Tao nhan vien voi username da ton tai
        /// Input: UserCreateVM voi Username = ExistingUsername
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Username đã tồn tại"
        /// </summary>
        [Fact]
        public async Task CreateEmployee_WithExistingUsername_ReturnsBadRequest()
        {
            // Arrange
            var model = new UserCreateVM
            {
                Username = ExistingUsername,
                Email = ValidEmail,
                FullName = ValidFullName
            };

            var existingUser = new User { UserId = 2, Username = ExistingUsername };

            _mockUserService.Setup(x => x.GetByEmail(ValidEmail)).ReturnsAsync((UserDto?)null);
            var existingUserDto = new UserDto { UserId = 2, Username = ExistingUsername };
            _mockUserService.Setup(x => x.GetByUsername(ExistingUsername)).ReturnsAsync(existingUserDto);

            // Act
            var result = await _controller.CreateEmployee(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Username đã tồn tại");
        }

        /// <summary>
        /// TCID09: Tao nhan vien voi file anh khong hop le
        /// Input: UserCreateVM voi Image co extension .txt
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message chứa "PNG, JPG hoặc JPEG"
        /// </summary>
        [Fact]
        public async Task CreateEmployee_WithInvalidImageExtension_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            var model = new UserCreateVM
            {
                Username = ValidUsername,
                Email = ValidEmail,
                FullName = ValidFullName,
                Image = mockFile.Object
            };

            // Act
            var result = await _controller.CreateEmployee(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("PNG, JPG hoặc JPEG");
        }

        /// <summary>
        /// TCID10: Tao nhan vien voi anh thanh cong
        /// Input: UserCreateVM voi Image co extension .jpg
        /// Expected: HTTP 200 OK, Success = true, Image URL duoc gan
        /// </summary>
        [Fact]
        public async Task CreateEmployee_WithValidImage_ReturnsOkWithImageUrl()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");

            var model = new UserCreateVM
            {
                Username = ValidUsername,
                Email = ValidEmail,
                FullName = ValidFullName,
                Image = mockFile.Object
            };

            var user = new User
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                Email = ValidEmail
            };

            var role = new RoleDto { RoleId = ValidRoleId, RoleName = RoleName };

            _mockUserService.Setup(x => x.GetByEmail(ValidEmail)).ReturnsAsync((UserDto?)null);
            _mockUserService.Setup(x => x.GetByUsername(ValidUsername)).ReturnsAsync((UserDto?)null);
            _mockCloudinaryService.Setup(x => x.UploadImageAsync(mockFile.Object, It.IsAny<string>())).ReturnsAsync(ValidImageUrl);
            _mockMapper.Setup(x => x.Map<UserCreateVM, User>(model)).Returns(user);
            _mockUserService.Setup(x => x.CreateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRoleService.Setup(x => x.GetByRoleName(RoleName)).ReturnsAsync(role);
            _mockMapper.Setup(x => x.Map<UserRoleCreateVM, UserRole>(It.IsAny<UserRoleCreateVM>())).Returns(new UserRole());
            _mockUserRoleService.Setup(x => x.CreateAsync(It.IsAny<UserRole>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateEmployee(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Image.Should().Be(ValidImageUrl);
        }

        #endregion

        #region UpdateEmployee Tests

        /// <summary>
        /// TCID11: Cap nhat nhan vien thanh cong
        /// Input: id = ValidUserId (1), UserEditVM voi FullName moi
        /// Expected: HTTP 200 OK, Success = true, tra ve User da cap nhat
        /// </summary>
        [Fact]
        public async Task UpdateEmployee_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new UserEditVM
            {
                FullName = "Updated Name",
                Phone = ValidPhone
            };

            var user = new User
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                Email = ValidEmail,
                FullName = ValidFullName
            };

            _mockUserService.Setup(x => x.GetByIdAsync(ValidUserId)).ReturnsAsync(user);
            _mockUserService.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateEmployee(ValidUserId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.FullName.Should().Be("Updated Name");
        }

        /// <summary>
        /// TCID12: Cap nhat nhan vien voi ID khong ton tai
        /// Input: id = NonExistentUserId (999), UserEditVM hop le
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy nhân viên"
        /// </summary>
        [Fact]
        public async Task UpdateEmployee_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var model = new UserEditVM { FullName = "New Name" };
            _mockUserService.Setup(x => x.GetByIdAsync(NonExistentUserId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.UpdateEmployee(NonExistentUserId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy nhân viên");
        }

        /// <summary>
        /// TCID13: Cap nhat nhan vien voi username da ton tai (username khac)
        /// Input: id = ValidUserId (1), UserEditVM voi Username = ExistingUsername
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Username đã tồn tại"
        /// </summary>
        [Fact]
        public async Task UpdateEmployee_WithExistingUsername_ReturnsBadRequest()
        {
            // Arrange
            var model = new UserEditVM
            {
                Username = ExistingUsername,
                FullName = ValidFullName
            };

            var user = new User
            {
                UserId = ValidUserId,
                Username = ValidUsername
            };

            var existingUser = new User
            {
                UserId = 2,
                Username = ExistingUsername
            };

            _mockUserService.Setup(x => x.GetByIdAsync(ValidUserId)).ReturnsAsync(user);
            var existingUserDto = new UserDto { UserId = 2, Username = ExistingUsername };
            _mockUserService.Setup(x => x.GetByUsername(ExistingUsername)).ReturnsAsync(existingUserDto);

            // Act
            var result = await _controller.UpdateEmployee(ValidUserId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Username đã tồn tại");
        }

        /// <summary>
        /// TCID14: Cap nhat nhan vien voi email da ton tai (email khac)
        /// Input: id = ValidUserId (1), UserEditVM voi Email = ExistingEmail
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Email đã tồn tại"
        /// </summary>
        [Fact]
        public async Task UpdateEmployee_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var model = new UserEditVM
            {
                Email = ExistingEmail,
                FullName = ValidFullName
            };

            var user = new User
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                Email = ValidEmail
            };

            var existingUser = new User
            {
                UserId = 2,
                Email = ExistingEmail
            };

            _mockUserService.Setup(x => x.GetByIdAsync(ValidUserId)).ReturnsAsync(user);
            var existingUserDto = new UserDto { UserId = 2, Email = ExistingEmail };
            _mockUserService.Setup(x => x.GetByEmail(ExistingEmail)).ReturnsAsync(existingUserDto);

            // Act
            var result = await _controller.UpdateEmployee(ValidUserId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Email đã tồn tại");
        }

        /// <summary>
        /// TCID15: Cap nhat nhan vien voi file anh khong hop le
        /// Input: id = ValidUserId (1), UserEditVM voi Image co extension .txt
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task UpdateEmployee_WithInvalidImageExtension_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            var model = new UserEditVM
            {
                FullName = ValidFullName,
                Image = mockFile.Object
            };

            // Act
            var result = await _controller.UpdateEmployee(ValidUserId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("PNG, JPG hoặc JPEG");
        }

        #endregion

        #region DeleteEmployee Tests

        /// <summary>
        /// TCID16: Xoa nhan vien thanh cong (soft delete)
        /// Input: id = ValidUserId (1)
        /// Expected: HTTP 200 OK, Success = true, Data = true
        /// </summary>
        [Fact]
        public async Task DeleteEmployee_WithValidId_ReturnsOk()
        {
            // Arrange
            var user = new User
            {
                UserId = ValidUserId,
                Username = ValidUsername,
                IsActive = true
            };

            _mockUserService.Setup(x => x.GetByIdAsync(ValidUserId)).ReturnsAsync(user);
            _mockUserService.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteEmployee(ValidUserId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        /// <summary>
        /// TCID17: Xoa nhan vien voi ID khong ton tai
        /// Input: id = NonExistentUserId (999)
        /// Expected: HTTP 404 NotFound, Success = false, Error.Message = "Không tìm thấy nhân viên"
        /// </summary>
        [Fact]
        public async Task DeleteEmployee_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetByIdAsync(NonExistentUserId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.DeleteEmployee(NonExistentUserId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy nhân viên");
        }

        /// <summary>
        /// TCID18: Xoa nhan vien khi service nem ngoai le
        /// Input: id = ValidUserId (1), service throw Exception
        /// Expected: HTTP 400 BadRequest, Success = false, Error.Message = "Có lỗi xảy ra khi xóa nhân viên"
        /// </summary>
        [Fact]
        public async Task DeleteEmployee_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetByIdAsync(ValidUserId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteEmployee(ValidUserId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<User>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi xóa nhân viên");
        }

        #endregion
    }
}
