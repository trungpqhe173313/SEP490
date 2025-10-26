using NB.Service.AccountService.Dto;
using NB.Service.Dto;
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
        Task<UserInfo?> GetUserByIdAsync(string userId);
        Task<bool> ValidateUserAsync(string username, string password);
    }
}
