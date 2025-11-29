using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.PayrollService.Dto;

namespace NB.Service.PayrollService
{
    public interface IPayrollService : IService<Payroll>
    {
        Task<List<PayrollOverviewDto>> GetPayrollOverviewAsync(int year, int month);
    }
}
