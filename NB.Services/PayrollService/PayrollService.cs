using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.PayrollService.Dto;

namespace NB.Service.PayrollService
{
    public class PayrollService : Service<Payroll>, IPayrollService
    {
        private readonly IRepository<Worklog> _worklogRepository;
        private readonly IRepository<User> _userRepository;

        public PayrollService(
            IRepository<Payroll> repository,
            IRepository<Worklog> worklogRepository,
            IRepository<User> userRepository) : base(repository)
        {
            _worklogRepository = worklogRepository;
            _userRepository = userRepository;
        }

        public async Task<List<PayrollOverviewDto>> GetPayrollOverviewAsync(int year, int month)
        {
            // Step 1: Xác định ngày bắt đầu và kết thúc của tháng
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Step 2: Load tất cả WorkLog trong tháng (chỉ lấy IsActive = true)
            var worklogs = await _worklogRepository.GetQueryable()
                .Include(w => w.Employee)
                .Include(w => w.Job)
                .Where(w => w.WorkDate >= startDate 
                    && w.WorkDate <= endDate
                    && w.IsActive == true)
                .ToListAsync();

            // Step 3: Group theo EmployeeId và JobId để tính chi tiết từng công việc
            var employeeWorklogGroups = worklogs
                .GroupBy(w => w.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    EmployeeName = g.First().Employee.FullName ?? string.Empty,
                    JobDetails = g.GroupBy(w => w.JobId)
                        .Select(jobGroup => new
                        {
                            JobId = jobGroup.Key,
                            JobName = jobGroup.First().Job.JobName,
                            PayType = jobGroup.First().Job.PayType,
                            Quantity = jobGroup.Sum(w => w.Quantity),
                            Rate = jobGroup.First().Rate,
                            Amount = jobGroup.Sum(w => w.Quantity * w.Rate)
                        })
                        .ToList(),
                    TotalAmount = g.Sum(w => w.Quantity * w.Rate)
                })
                .ToList();

            // Step 4: Load tất cả Payroll trong tháng (theo StartDate và EndDate)
            var existingPayrolls = await GetQueryable()
                .Where(p => p.StartDate >= DateOnly.FromDateTime(startDate)
                    && p.EndDate <= DateOnly.FromDateTime(endDate))
                .ToListAsync();

            var result = new List<PayrollOverviewDto>();

            // Step 5: Xử lý từng nhân viên có worklog
            foreach (var empGroup in employeeWorklogGroups)
            {
                // Tìm payroll hiện tại của nhân viên trong tháng
                var existingPayroll = existingPayrolls
                    .FirstOrDefault(p => p.EmployeeId == empGroup.EmployeeId);

                var jobDetails = empGroup.JobDetails.Select(jd => new JobDetailDto
                {
                    JobId = jd.JobId,
                    JobName = jd.JobName,
                    PayType = jd.PayType,
                    Quantity = jd.Quantity,
                    Rate = jd.Rate,
                    Amount = jd.Amount
                }).ToList();

                if (existingPayroll != null)
                {
                    // Đã có Payroll → Tự động update TotalAmount
                    existingPayroll.TotalAmount = empGroup.TotalAmount;
                    existingPayroll.LastUpdated = DateTime.Now;
                    await UpdateAsync(existingPayroll);

                    result.Add(new PayrollOverviewDto
                    {
                        EmployeeId = empGroup.EmployeeId,
                        EmployeeName = empGroup.EmployeeName,
                        TotalAmount = empGroup.TotalAmount,
                        Status = existingPayroll.IsPaid == true ? PayrollStatus.Paid.GetDescription() : PayrollStatus.Generated.GetDescription(),
                        PayrollId = existingPayroll.PayrollId,
                        PaidDate = existingPayroll.PaidDate,
                        JobDetails = jobDetails
                    });
                }
                else
                {
                    // Chưa có Payroll → Status = NotGenerated
                    result.Add(new PayrollOverviewDto
                    {
                        EmployeeId = empGroup.EmployeeId,
                        EmployeeName = empGroup.EmployeeName,
                        TotalAmount = empGroup.TotalAmount,
                        Status = PayrollStatus.NotGenerated.GetDescription(),
                        PayrollId = null,
                        PaidDate = null,
                        JobDetails = jobDetails
                    });
                }
            }

            return result.OrderBy(r => r.EmployeeName).ToList();
        }
    }
}
