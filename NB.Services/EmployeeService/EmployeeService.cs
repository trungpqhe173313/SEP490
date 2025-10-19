using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Repository.EmployeeRepository.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.EmployeeService
{
    public class EmployeeService : Service<Employee>, IEmployeeService
    {
        public EmployeeService(IRepository<Employee> repository) : base(repository)
        {
        }

        public async Task<PagedList<EmployeeDto>> GetData(EmployeeSearch search)
        {
            var query = from emp in GetQueryable()
                        select new EmployeeDto()
                        {
                            EmployeeId = emp.EmployeeId,
                            UserId = emp.UserId,
                            FullName = emp.FullName,
                            Phone = emp.Phone,
                            HireDate = emp.HireDate,
                            Status = emp.Status
                        };
            if(search != null)
            {
                if (!string.IsNullOrEmpty(search.FullName))
                {
                    query = query.Where(e => e.FullName != null && e.FullName.Contains(search.FullName));
                }
                if (!string.IsNullOrEmpty(search.PhoneNumber))
                {
                    query = query.Where(e => e.Phone != null && e.Phone == search.PhoneNumber);
                }
            }
            query = query.OrderByDescending(e => e.EmployeeId);
            return await PagedList<EmployeeDto>.CreateAsync(query, search);
        }

        public async Task<EmployeeDto?> GetDto(int id)
        {
            var query = from emp in GetQueryable()
                        where emp.EmployeeId == id
                        select new EmployeeDto()
                        {
                            EmployeeId = emp.EmployeeId,
                            UserId = emp.UserId,
                            FullName = emp.FullName,
                            Phone = emp.Phone,
                            HireDate = emp.HireDate,
                            Status = emp.Status
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<EmployeeDto?> GetByUserId(int id)
        {
            var query = from emp in GetQueryable()
                        where emp.UserId == id
                        select new EmployeeDto()
                        {
                            EmployeeId = emp.EmployeeId,
                            UserId = emp.UserId,
                            FullName = emp.FullName,
                            Phone = emp.Phone,
                            HireDate = emp.HireDate,
                            Status = emp.Status
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<EmployeeDto?> GetByPhone(string phone)
        {
            var query = from emp in GetQueryable()
                        where emp.Phone != null && emp.Phone.Equals(phone)
                        select new EmployeeDto()
                        {
                            EmployeeId = emp.EmployeeId,
                            UserId = emp.UserId,
                            FullName = emp.FullName,
                            Phone = emp.Phone,
                            HireDate = emp.HireDate,
                            Status = emp.Status
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
