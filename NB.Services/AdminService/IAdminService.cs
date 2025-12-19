using NB.Model.Entities;
using NB.Service.AccountService.Dto;
using NB.Service.AdminService.Dto;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.UserService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AdminService
{
    public interface IAdminService: IService<User>
    {
        Task<PagedList<AccountDto>> GetData(AccountSearch search);
        Task<ApiResponse<bool>> UpdateAccountAsync(int id, UpdateAccountDto dto);
        Task<ApiResponse<string>> ResetUserPasswordAsync(int userId, string newPassword);
    }
}
