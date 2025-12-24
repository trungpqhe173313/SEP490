using Microsoft.AspNetCore.Http;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;

namespace NB.Service.CustomerService
{
    public interface ICustomerService
    {
        Task<PagedList<UserDto>> GetCustomersAsync(UserSearch search, bool isAdmin = false);
        Task<UserDto?> GetCustomerByIdAsync(int id);
        Task<User> UpdateCustomerAsync(int id, UserEditVM model, IFormFile? image);
        Task<bool> DeleteCustomerAsync(int id);
        Task<string> CreateCustomerAccountAsync(CreateCustomerAccountVM model, IFormFile? image);
    }
}
