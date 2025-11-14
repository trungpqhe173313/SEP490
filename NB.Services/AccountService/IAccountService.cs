using NB.Service.AccountService.Dto;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService
{
    public interface IAccountService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password);
        Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(string email);
        Task<ApiResponse<VerifyOtpResponse>> VerifyOtpAsync(string email, string otpCode);
        Task<ApiResponse<bool>> ResetPasswordAsync(string resetToken, string newPassword);
        Task<ApiResponse<bool>> LogoutAsync(int userId);
        Task<ApiResponse<UserInfo>> GetProfileAsync(int userId);
        Task<ApiResponse<bool>> UpdateProfileAsync(int userId, UpdateProfileDto request);
    }
}
