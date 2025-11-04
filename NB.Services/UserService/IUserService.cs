using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.UserService
{
    public interface IUserService : IService<User>
    {
        Task<PagedList<UserDto>> GetData(UserSearch search);
        Task<UserDto?> GetByUserId(int id);
        Task<List<UserDto>?> GetAllUser(UserSearch search);
        Task<UserDto?> GetByEmail(string email);
        Task<UserDto?> GetByUsername(string username);
        Task<UserDto?> GetByRefreshTokenAsync(string RefreshToken);
        Task<bool> CheckPasswordAsync(UserDto user, string password);
    }
}
