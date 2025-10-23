using NB.Model.Entities;
using NB.Service.EmployeeService.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.EmployeeService
{
    public interface IEmployeeService : IService<Employee>
    {
        Task<PagedList<EmployeeDto>> GetData(EmployeeSearch search);
        Task<EmployeeDto?> GetDto(int id);
        Task<EmployeeDto?> GetByUserId(int id);
        Task<EmployeeDto?> GetByPhone(string phone);
    }
}
