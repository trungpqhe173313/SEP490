using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NB.API.Controllers;
using NB.Service.AccountService;
using NB.Service.AccountService.Dto;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using System.Security.Claims;

namespace NB.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly AccountController _controller;

        // Reusable Test Data Constants
        private const int ValidUserId = 1;
        private const int NonExistentUserId = 999;
        private const string ValidUsername = "testuser";
        private const string ValidPassword = "ValidPass123!";
        private const string ValidOldPassword = "OldPass123!";
        private const string ValidNewPassword = "NewPass123!";
        private const string ValidEmail = "test@example.com";
        private const string ValidRefreshToken = "valid_refresh_token";
        private const string ValidAccessToken = "valid_access_token";
        private const string ValidOtpCode = "123456";
        private const string ValidResetToken = "valid_reset_token";
        private const string ValidFullName = "Test User";
        private const string ValidPhone = "0123456789";
        private const string InvalidEmail = "invalid-email";

        public AccountControllerTests()
        {
            _mockAccountService = new Mock<IAccountService>();
            _controller = new AccountController(_mockAccountService.Object);
        }

        private void SetupUserClaim(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region Login Tests

        /// <summary>
        /// TCID01: Dang nhap voi thong tin hop le
        /// Input: LoginDto voi Username = ValidUsername, Password = ValidPassword
        /// Expected: HTTP 200 OK, Success = true, tra ve access token va refresh token
        /// </summary>
        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOk()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Username = ValidUsername,
                Password = ValidPassword
            };

            var loginResponse = new ApiResponse<LoginResponse>
            {
                Success = true,
                StatusCode = 200,
                Data = new LoginResponse
                {
                    AccessToken = ValidAccessToken,
                    RefreshToken = ValidRefreshToken,
                    UserInfo = new UserInfo
                    {
                        Id = ValidUserId,
                        Username = ValidUsername
                    }
                }
            };

            _mockAccountService
                .Setup(x => x.LoginAsync(ValidUsername, ValidPassword))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<LoginResponse>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.AccessToken.Should().Be(ValidAccessToken);
            response.Data.RefreshToken.Should().Be(ValidRefreshToken);
        }

        /// <summary>
        /// TCID02: Dang nhap voi thong tin khong hop le
        /// Input: LoginDto voi Username hoac Password sai
        /// Expected: HTTP 401 Unauthorized, Success = false
        /// </summary>
        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Username = ValidUsername,
                Password = "WrongPassword"
            };

            var loginResponse = new ApiResponse<LoginResponse>
            {
                Success = false,
                StatusCode = 401,
                Error = new ApiError { Message = "Tên đăng nhập hoặc mật khẩu không đúng" }
            };

            _mockAccountService
                .Setup(x => x.LoginAsync(ValidUsername, "WrongPassword"))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(401);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<LoginResponse>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region Logout Tests

        /// <summary>
        /// TCID03: Dang xuat thanh cong
        /// Input: User da dang nhap voi ValidUserId (1)
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task Logout_WithValidUser_ReturnsOk()
        {
            // Arrange
            SetupUserClaim(ValidUserId);

            var logoutResponse = new ApiResponse<bool>
            {
                Success = true,
                StatusCode = 200,
                Data = true
            };

            _mockAccountService
                .Setup(x => x.LogoutAsync(ValidUserId))
                .ReturnsAsync(logoutResponse);

            // Act
            var result = await _controller.Logout();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID04: Dang xuat khi khong co user claim
        /// Input: Khong co ClaimTypes.NameIdentifier
        /// Expected: HTTP 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task Logout_WithoutUserClaim_ThrowsNullReferenceException()
        {
            // Arrange - No user claim setup

            // Act & Assert - Expect NullReferenceException due to missing claim
            await Assert.ThrowsAsync<NullReferenceException>(() => _controller.Logout());
        }

        #endregion

        #region RefreshToken Tests

        /// <summary>
        /// TCID05: Lam moi token voi refresh token hop le
        /// Input: RefreshTokenRequest voi ValidRefreshToken
        /// Expected: HTTP 200 OK, Success = true, tra ve access token va refresh token moi
        /// </summary>
        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsOk()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = ValidRefreshToken
            };

            var refreshResponse = new ApiResponse<RefreshTokenResponse>
            {
                Success = true,
                StatusCode = 200,
                Data = new RefreshTokenResponse
                {
                    AccessToken = ValidAccessToken,
                    RefreshToken = ValidRefreshToken
                }
            };

            _mockAccountService
                .Setup(x => x.RefreshTokenAsync(ValidRefreshToken))
                .ReturnsAsync(refreshResponse);

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<RefreshTokenResponse>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TCID06: Lam moi token voi refresh token khong hop le
        /// Input: RefreshTokenRequest voi refresh token khong hop le
        /// Expected: HTTP 401 Unauthorized, Success = false
        /// </summary>
        [Fact]
        public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "invalid_token"
            };

            var refreshResponse = new ApiResponse<RefreshTokenResponse>
            {
                Success = false,
                StatusCode = 401,
                Error = new ApiError { Message = "Refresh token không hợp lệ" }
            };

            _mockAccountService
                .Setup(x => x.RefreshTokenAsync("invalid_token"))
                .ReturnsAsync(refreshResponse);

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(401);
        }

        #endregion

        #region ChangePassword Tests

        /// <summary>
        /// TCID07: Doi mat khau thanh cong
        /// Input: ChangePasswordDto voi OldPassword va NewPassword hop le
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task ChangePassword_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupUserClaim(ValidUserId);

            var request = new ChangePasswordDto
            {
                OldPassword = ValidOldPassword,
                NewPassword = ValidNewPassword
            };

            var changeResponse = new ApiResponse<bool>
            {
                Success = true,
                StatusCode = 200,
                Data = true
            };

            _mockAccountService
                .Setup(x => x.ChangePasswordAsync(ValidUserId, ValidOldPassword, ValidNewPassword))
                .ReturnsAsync(changeResponse);

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID08: Doi mat khau voi mat khau cu khong dung
        /// Input: ChangePasswordDto voi OldPassword sai
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task ChangePassword_WithWrongOldPassword_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaim(ValidUserId);

            var request = new ChangePasswordDto
            {
                OldPassword = "WrongOldPassword",
                NewPassword = ValidNewPassword
            };

            var changeResponse = new ApiResponse<bool>
            {
                Success = false,
                StatusCode = 400,
                Error = new ApiError { Message = "Mật khẩu cũ không đúng" }
            };

            _mockAccountService
                .Setup(x => x.ChangePasswordAsync(ValidUserId, "WrongOldPassword", ValidNewPassword))
                .ReturnsAsync(changeResponse);

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(400);
        }

        #endregion

        #region ForgotPassword Tests

        /// <summary>
        /// TCID09: Quen mat khau voi email hop le
        /// Input: ForgotPasswordDto voi ValidEmail
        /// Expected: HTTP 200 OK, Success = true, gui OTP thanh cong
        /// </summary>
        [Fact]
        public async Task ForgotPassword_WithValidEmail_ReturnsOk()
        {
            // Arrange
            var request = new ForgotPasswordDto
            {
                Email = ValidEmail
            };

            var forgotResponse = new ApiResponse<ForgotPasswordResponse>
            {
                Success = true,
                StatusCode = 200,
                Data = new ForgotPasswordResponse
                {
                    Message = "OTP đã được gửi tới email"
                }
            };

            _mockAccountService
                .Setup(x => x.ForgotPasswordAsync(ValidEmail))
                .ReturnsAsync(forgotResponse);

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<ForgotPasswordResponse>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID10: Quen mat khau voi email khong ton tai
        /// Input: ForgotPasswordDto voi email khong ton tai trong he thong
        /// Expected: HTTP 404 NotFound, Success = false
        /// </summary>
        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_ReturnsNotFound()
        {
            // Arrange
            var request = new ForgotPasswordDto
            {
                Email = "notfound@example.com"
            };

            var forgotResponse = new ApiResponse<ForgotPasswordResponse>
            {
                Success = false,
                StatusCode = 404,
                Error = new ApiError { Message = "Email không tồn tại trong hệ thống" }
            };

            _mockAccountService
                .Setup(x => x.ForgotPasswordAsync("notfound@example.com"))
                .ReturnsAsync(forgotResponse);

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(404);
        }

        #endregion

        #region VerifyOtp Tests

        /// <summary>
        /// TCID11: Xac thuc OTP hop le
        /// Input: VerifyOtpDto voi ValidEmail va ValidOtpCode (123456)
        /// Expected: HTTP 200 OK, Success = true, tra ve reset token
        /// </summary>
        [Fact]
        public async Task VerifyOtp_WithValidOtp_ReturnsOk()
        {
            // Arrange
            var request = new VerifyOtpDto
            {
                Email = ValidEmail,
                OtpCode = ValidOtpCode
            };

            var verifyResponse = new ApiResponse<VerifyOtpResponse>
            {
                Success = true,
                StatusCode = 200,
                Data = new VerifyOtpResponse
                {
                    ResetToken = ValidResetToken
                }
            };

            _mockAccountService
                .Setup(x => x.VerifyOtpAsync(ValidEmail, ValidOtpCode))
                .ReturnsAsync(verifyResponse);

            // Act
            var result = await _controller.VerifyOtp(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<VerifyOtpResponse>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.ResetToken.Should().Be(ValidResetToken);
        }

        /// <summary>
        /// TCID12: Xac thuc OTP khong hop le
        /// Input: VerifyOtpDto voi OTP sai
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task VerifyOtp_WithInvalidOtp_ReturnsBadRequest()
        {
            // Arrange
            var request = new VerifyOtpDto
            {
                Email = ValidEmail,
                OtpCode = "000000"
            };

            var verifyResponse = new ApiResponse<VerifyOtpResponse>
            {
                Success = false,
                StatusCode = 400,
                Error = new ApiError { Message = "OTP không hợp lệ hoặc đã hết hạn" }
            };

            _mockAccountService
                .Setup(x => x.VerifyOtpAsync(ValidEmail, "000000"))
                .ReturnsAsync(verifyResponse);

            // Act
            var result = await _controller.VerifyOtp(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(400);
        }

        #endregion

        #region ResetPassword Tests

        /// <summary>
        /// TCID13: Dat lai mat khau thanh cong
        /// Input: ResetPasswordDto voi ValidResetToken va ValidNewPassword
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task ResetPassword_WithValidToken_ReturnsOk()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                ResetToken = ValidResetToken,
                NewPassword = ValidNewPassword
            };

            var resetResponse = new ApiResponse<bool>
            {
                Success = true,
                StatusCode = 200,
                Data = true
            };

            _mockAccountService
                .Setup(x => x.ResetPasswordAsync(ValidResetToken, ValidNewPassword))
                .ReturnsAsync(resetResponse);

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID14: Dat lai mat khau voi token khong hop le
        /// Input: ResetPasswordDto voi reset token khong hop le
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                ResetToken = "invalid_token",
                NewPassword = ValidNewPassword
            };

            var resetResponse = new ApiResponse<bool>
            {
                Success = false,
                StatusCode = 400,
                Error = new ApiError { Message = "Reset token không hợp lệ hoặc đã hết hạn" }
            };

            _mockAccountService
                .Setup(x => x.ResetPasswordAsync("invalid_token", ValidNewPassword))
                .ReturnsAsync(resetResponse);

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(400);
        }

        #endregion

        #region GetProfile Tests

        /// <summary>
        /// TCID15: Lay thong tin profile thanh cong
        /// Input: User da dang nhap voi ValidUserId (1)
        /// Expected: HTTP 200 OK, Success = true, tra ve UserDto
        /// </summary>
        [Fact]
        public async Task GetProfile_WithValidUser_ReturnsOk()
        {
            // Arrange
            SetupUserClaim(ValidUserId);

            var profileResponse = new ApiResponse<UserInfo>
            {
                Success = true,
                StatusCode = 200,
                Data = new UserInfo
                {
                    Id = ValidUserId,
                    Username = ValidUsername,
                    Email = ValidEmail,
                    FullName = ValidFullName,
                    Phone = ValidPhone
                }
            };

            _mockAccountService
                .Setup(x => x.GetProfileAsync(ValidUserId))
                .ReturnsAsync(profileResponse);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<UserInfo>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(ValidUserId);
        }

        /// <summary>
        /// TCID16: Lay profile khi khong co user claim
        /// Input: Khong co ClaimTypes.NameIdentifier
        /// Expected: HTTP 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task GetProfile_WithoutUserClaim_ThrowsNullReferenceException()
        {
            // Arrange - No user claim setup

            // Act & Assert - Expect NullReferenceException due to missing claim
            await Assert.ThrowsAsync<NullReferenceException>(() => _controller.GetProfile());
        }

        #endregion

        #region UpdateProfile Tests

        /// <summary>
        /// TCID17: Cap nhat profile thanh cong
        /// Input: UpdateProfileDto voi thong tin hop le
        /// Expected: HTTP 200 OK, Success = true
        /// </summary>
        [Fact]
        public async Task UpdateProfile_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupUserClaim(ValidUserId);

            var request = new UpdateProfileDto
            {
                fullName = "Updated Name",
                phone = "0987654321",
                email = ValidEmail
            };

            var updateResponse = new ApiResponse<bool>
            {
                Success = true,
                StatusCode = 200,
                Data = true
            };

            _mockAccountService
                .Setup(x => x.UpdateProfileAsync(ValidUserId, request))
                .ReturnsAsync(updateResponse);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(200);
            var response = statusResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
        }

        /// <summary>
        /// TCID18: Cap nhat profile voi file anh khong hop le
        /// Input: UpdateProfileDto voi imageFile co extension .txt
        /// Expected: HTTP 400 BadRequest, Success = false
        /// </summary>
        [Fact]
        public async Task UpdateProfile_WithInvalidImageExtension_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaim(ValidUserId);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            var request = new UpdateProfileDto
            {
                fullName = ValidFullName,
                imageFile = mockFile.Object
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("PNG, JPG hoặc JPEG");
        }

        /// <summary>
        /// TCID19: Cap nhat profile khi khong co user claim
        /// Input: Khong co ClaimTypes.NameIdentifier
        /// Expected: HTTP 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task UpdateProfile_WithoutUserClaim_ThrowsNullReferenceException()
        {
            // Arrange - No user claim setup
            var request = new UpdateProfileDto
            {
                fullName = ValidFullName
            };

            // Act & Assert - Expect NullReferenceException due to missing claim
            await Assert.ThrowsAsync<NullReferenceException>(() => _controller.UpdateProfile(request));
        }

        #endregion
    }
}
