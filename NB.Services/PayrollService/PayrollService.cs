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
        private readonly IRepository<FinancialTransaction> _financialTransactionRepository;

        public PayrollService(
            IRepository<Payroll> repository,
            IRepository<Worklog> worklogRepository,
            IRepository<User> userRepository,
            IRepository<FinancialTransaction> financialTransactionRepository) : base(repository)
        {
            _worklogRepository = worklogRepository;
            _userRepository = userRepository;
            _financialTransactionRepository = financialTransactionRepository;
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

        public async Task<Payroll> CreatePayrollAsync(CreatePayrollDto dto, int createdBy)
        {
            // Validate tháng/năm
            if (dto.Year < 2000 || dto.Year > 2100)
            {
                throw new ArgumentException("Năm không hợp lệ");
            }

            if (dto.Month < 1 || dto.Month > 12)
            {
                throw new ArgumentException("Tháng không hợp lệ (1-12)");
            }

            // Xác định ngày bắt đầu và kết thúc
            var startDate = new DateTime(dto.Year, dto.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Kiểm tra nhân viên tồn tại và có role Employee (RoleId = 3)
            var employee = await _userRepository.GetQueryable()
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == dto.EmployeeId);

            if (employee == null)
            {
                throw new ArgumentException($"Không tìm thấy nhân viên với ID {dto.EmployeeId}");
            }

            var isEmployee = employee.UserRoles.Any(ur => ur.RoleId == 3);
            if (!isEmployee)
            {
                throw new ArgumentException($"User {employee.FullName} không phải là nhân viên ");
            }

            // Kiểm tra đã tồn tại bảng lương trong tháng này chưa
            var existingPayroll = await GetQueryable()
                .FirstOrDefaultAsync(p => p.EmployeeId == dto.EmployeeId
                    && p.StartDate >= DateOnly.FromDateTime(startDate)
                    && p.EndDate <= DateOnly.FromDateTime(endDate));

            if (existingPayroll != null)
            {
                throw new InvalidOperationException($"Bảng lương cho nhân viên {employee.FullName} tháng {dto.Month}/{dto.Year} đã tồn tại");
            }

            // Tính tổng tiền từ WorkLog (chỉ lấy IsActive = true)
            var worklogs = await _worklogRepository.GetQueryable()
                .Where(w => w.EmployeeId == dto.EmployeeId
                    && w.WorkDate >= startDate
                    && w.WorkDate <= endDate
                    && w.IsActive == true)
                .ToListAsync();

            var totalAmount = worklogs.Sum(w => w.Quantity * w.Rate);

            // Tạo Payroll mới
            var payroll = new Payroll
            {
                EmployeeId = dto.EmployeeId,
                StartDate = DateOnly.FromDateTime(startDate),
                EndDate = DateOnly.FromDateTime(endDate),
                TotalAmount = totalAmount,
                IsPaid = false,
                PaidDate = null,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                Note = dto.Note,
                LastUpdated = DateTime.Now
            };

            await CreateAsync(payroll);
            return payroll;
        }

        public async Task<PayPayrollResponseDto> PayPayrollAsync(PayPayrollDto dto, int paidBy)
        {
            // Validate PaymentMethod
            if (dto.PaymentMethod != "TienMat" && dto.PaymentMethod != "NganHang")
            {
                throw new ArgumentException("Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: TienMat, NganHang");
            }

            // Lấy Payroll
            var payroll = await GetQueryable()
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.PayrollId == dto.PayrollId);

            if (payroll == null)
            {
                throw new ArgumentException($"Không tìm thấy bảng lương với ID {dto.PayrollId}");
            }

            // Kiểm tra đã thanh toán chưa
            if (payroll.IsPaid == true)
            {
                throw new InvalidOperationException($"Bảng lương này đã được thanh toán vào {payroll.PaidDate:dd/MM/yyyy}");
            }

            // Cập nhật Payroll
            payroll.IsPaid = true;
            payroll.PaidDate = DateTime.Now;
            payroll.LastUpdated = DateTime.Now;
            await UpdateAsync(payroll);

            // Tạo FinancialTransaction
            var transaction = new FinancialTransaction
            {
                TransactionDate = DateTime.Now,
                Type = TransactionType.ThanhToanLuong.ToString(),
                Amount = payroll.TotalAmount,
                Description = dto.Note ?? $"Thanh toán lương tháng {payroll.StartDate.Month}/{payroll.StartDate.Year} cho {payroll.Employee.FullName}",
                PaymentMethod = dto.PaymentMethod,
                PayrollId = payroll.PayrollId,
                CreatedBy = paidBy
            };

            _financialTransactionRepository.Add(transaction);
            await _financialTransactionRepository.SaveAsync();

            return new PayPayrollResponseDto
            {
                EmployeeId = payroll.EmployeeId,
                EmployeeName = payroll.Employee.FullName ?? string.Empty,
                PaidDate = payroll.PaidDate ?? DateTime.Now,
                PaymentMethod = dto.PaymentMethod,
                TotalAmount = payroll.TotalAmount
            };
        }

        public async Task<PayrollDetailDto> GetPayrollDetailAsync(int payrollId)
        {
            // Lấy Payroll với thông tin liên quan
            var payroll = await GetQueryable()
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.PayrollId == payrollId);

            if (payroll == null)
            {
                throw new ArgumentException($"Không tìm thấy bảng lương với ID {payrollId}");
            }

            // Lấy thông tin người tạo
            string? createdByName = null;
            if (payroll.CreatedBy.HasValue)
            {
                var creator = await _userRepository.GetByIdAsync(payroll.CreatedBy.Value);
                createdByName = creator?.FullName;
            }

            // Lấy WorkLog chi tiết theo từng Job trong khoảng thời gian của Payroll
            var startDate = payroll.StartDate.ToDateTime(TimeOnly.MinValue);
            var endDate = payroll.EndDate.ToDateTime(TimeOnly.MinValue);

            var worklogs = await _worklogRepository.GetQueryable()
                .Include(w => w.Job)
                .Where(w => w.EmployeeId == payroll.EmployeeId
                    && w.WorkDate >= startDate
                    && w.WorkDate <= endDate
                    && w.IsActive == true)
                .ToListAsync();

            // Group theo Job để tính tổng
            var jobDetails = worklogs
                .GroupBy(w => w.JobId)
                .Select(g => new JobDetailDto
                {
                    JobId = g.Key,
                    JobName = g.First().Job.JobName,
                    PayType = g.First().Job.PayType,
                    Quantity = g.Sum(w => w.Quantity),
                    Rate = g.First().Rate,
                    Amount = g.Sum(w => w.Quantity * w.Rate)
                })
                .ToList();

            return new PayrollDetailDto
            {
                PayrollId = payroll.PayrollId,
                EmployeeId = payroll.EmployeeId,
                EmployeeName = payroll.Employee.FullName ?? string.Empty,
                StartDate = startDate,
                EndDate = endDate,
                TotalAmount = payroll.TotalAmount,
                Status = payroll.IsPaid == true ? PayrollStatus.Paid.GetDescription() : PayrollStatus.Generated.GetDescription(),
                PaidDate = payroll.PaidDate,
                CreatedAt = payroll.CreatedAt,
                CreatedByName = createdByName,
                Note = payroll.Note,
                JobDetails = jobDetails
            };
        }
    }
}
