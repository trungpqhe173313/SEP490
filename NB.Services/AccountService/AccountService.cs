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

            return GenToken(user);
        }

        public Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateUserAsync(string username, string password)
        {
            throw new NotImplementedException();
        }
        private ApiResponse<LoginResponse> GenToken(UserDto? user)
        {
            if (user == null)
                return ApiResponse<LoginResponse>.Fail("User not found", 401);

            var refreshToken = _jwtService.GenerateRefreshToken();

            var userDto = new UserInfo
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
            };

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
                ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Set expiration time
                UserInfo = userDto
            });
        }
    }
}
