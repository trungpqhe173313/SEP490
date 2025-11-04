using NB.Model.Entities;
using NB.Repository.RoleRepository;
using NB.Repository.UserRolerRepository;
using NB.Service.AccountService.Dto;
using NB.Service.Core.JwtService;
using NB.Service.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService
{
    public class AccountService : IAccountService
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IUserRolerRepository _userRoleRepository;
        private readonly IRoleRepository _roleRepository;
        public AccountService(IUserService userService, 
            IJwtService jwtService, 
            IUserRolerRepository userRolerRepository, 
            IRoleRepository roleRepository)
        {
            _userService = userService;
            _jwtService = jwtService;
            _userRoleRepository = userRolerRepository;
            _roleRepository = roleRepository;
        }
        public Task<UserInfo?> GetUserByIdAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(password))
                return ApiResponse<LoginResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác", 401);

            var user = await _userService.GetByUsername(username);
            if (user == null)
                return ApiResponse<LoginResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác", 401);

            var result = await _userService.CheckPasswordAsync(user, password);
            if (!result)
            {
                return ApiResponse<LoginResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác", 401);
            }

            return await GenToken(user);
        }

        public async Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (!_jwtService.ValidateRefreshToken(refreshToken))
            {
                return ApiResponse<RefreshTokenResponse>.Fail("Invalid refresh token format", 401);
            }
            var user = await _userService.GetByRefreshTokenAsync(refreshToken);
            if (user == null)
                return ApiResponse<RefreshTokenResponse>.Fail("Invalid refresh token", 401);
            if(user.RefreshTokenExpiryDate < DateTime.Now)
                return ApiResponse<RefreshTokenResponse>.Fail("Refresh token has expired", 401);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            var refreshTokenExpiryDate = _jwtService.GetRefreshTokenExpiry();

            var accessTokenExpiry = _jwtService.GetAccessTokenExpiry();

            user.RefreshToken = newRefreshToken;

            user.RefreshTokenExpiryDate = refreshTokenExpiryDate;

            await _userService.UpdateAsync(user);

            var tokenResponse = await GenToken(user);
            var userDto = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
                // Lấy roles tương tự phương thức GenToken
                Roles = _userRoleRepository.GetQueryable()
              .Where(x => x.UserId == user.UserId)
              .Join(_roleRepository.GetQueryable(),
                  userRole => userRole.RoleId,
                  role => role.RoleId,
                  (userRole, role) => role.RoleName)
              .Distinct()
              .ToList()
            };
            var newAccessToken = _jwtService.GenerateToken(userDto);

            return ApiResponse<RefreshTokenResponse>.Ok(new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = accessTokenExpiry
            });
        }

        public Task<bool> ValidateUserAsync(string username, string password)
        {
            throw new NotImplementedException();
        }
        private async Task<ApiResponse<LoginResponse>> GenToken(UserDto? user)
        {
            if (user == null)
                return ApiResponse<LoginResponse>.Fail("User not found", 401);

            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiryDate = _jwtService.GetRefreshTokenExpiry();
            var accessTokenExpiry = _jwtService.GetAccessTokenExpiry();
            var userDto = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
            };

            var userRefreshToken = await _userService.GetByIdAsync(user.UserId);
            if(userRefreshToken != null)
            {
                userRefreshToken.RefreshToken = refreshToken;
                userRefreshToken.RefreshTokenExpiryDate = refreshTokenExpiryDate;
                await _userService.UpdateAsync(userRefreshToken);
            }

            var roleFromUser = _userRoleRepository.GetQueryable()
               .Where(x => x.UserId == user.UserId)
               .Join(_roleRepository.GetQueryable(),
                   userRole => userRole.RoleId,
                   role => role.RoleId,
                   (userRole, role) => role.RoleName)
               .Distinct()
               .ToList();

            var sumRole = roleFromUser.ToList();
            userDto.Roles = sumRole;

            var token = _jwtService.GenerateToken(userDto);

            return ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresAt = accessTokenExpiry, 
                UserInfo = userDto
            });
        }
    }
}
