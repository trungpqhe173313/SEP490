using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.WorklogService.Dto;
using NB.Service.WorklogService.ViewModels;

namespace NB.Service.WorklogService
{
    public class WorklogService : Service<Worklog>, IWorklogService
    {
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _userRoleRepository;

        public WorklogService(
            IRepository<Worklog> worklogRepository,
            IRepository<Job> jobRepository,
            IRepository<User> userRepository,
            IRepository<UserRole> userRoleRepository) : base(worklogRepository)
        {
            _jobRepository = jobRepository;
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<WorklogResponseVM> CreateWorklogAsync(int employeeId, int jobId, decimal? quantity, DateTime? workDate, string? note)
        {
            // Kiểm tra employee tồn tại
            var employee = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == employeeId);
            // Kiểm tra job tồn tại
            var job = await _jobRepository.GetQueryable()
                .FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
            {
                throw new Exception("Công việc không tồn tại");
            }

            if (job.IsActive != true)
            {
                throw new Exception("Công việc không còn hoạt động");
            }

            var workDateValue = workDate ?? DateTime.Now;
            var startOfDay = workDateValue.Date;
            var endOfDay = startOfDay.AddDays(1);

            // Kiểm tra đã tồn tại worklog cho employee + job + ngày này chưa
            var existingWorklog = await GetQueryable()
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId 
                    && w.JobId == jobId 
                    && w.WorkDate >= startOfDay 
                    && w.WorkDate < endOfDay);

            if (existingWorklog != null)
            {
                throw new Exception("Nhân viên đã có worklog cho công việc này trong ngày");
            }

            decimal quantityValue;

            // Xử lý logic PayType
            if (job.PayType == "Per_Ngay")
            {
                // Tự động set Quantity = 1
                quantityValue = 1;
            }
            else if (job.PayType == "Per_Tan")
            {
                // User phải nhập Quantity
                if (!quantity.HasValue || quantity.Value <= 0)
                {
                    throw new Exception("Vui lòng nhập số tấn (Quantity) cho công việc tính theo tấn");
                }
                quantityValue = quantity.Value;
            }
            else
            {
                throw new Exception("Loại tính công không hợp lệ");
            }

            // Tạo worklog mới
            var worklog = new Worklog
            {
                EmployeeId = employeeId,
                JobId = jobId,
                Quantity = quantityValue,
                Rate = job.Rate,
                WorkDate = workDateValue,
                Note = note
            };

            await CreateAsync(worklog);

            // Trả về WorklogResponseVM
            var response = new WorklogResponseVM
            {
                Id = worklog.Id,
                EmployeeId = worklog.EmployeeId,
                EmployeeName = employee.FullName ?? string.Empty,
                JobId = worklog.JobId,
                JobName = job.JobName,
                PayType = job.PayType,
                Quantity = worklog.Quantity,
                Rate = worklog.Rate,
                TotalAmount = worklog.Quantity * worklog.Rate,
                Note = worklog.Note,
                WorkDate = worklog.WorkDate
            };

            return response;
        }

        public async Task<CreateWorklogBatchResponseVM> CreateWorklogBatchAsync(CreateWorklogBatchDto dto)
        {
            // Kiểm tra employee tồn tại
            var employee = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == dto.EmployeeId);
            if (employee == null)
            {
                throw new Exception("Nhân viên không tồn tại");
            }

            // Kiểm tra user có role Employee (RoleId = 3) không
            var hasEmployeeRole = await _userRoleRepository.GetQueryable()
                .AnyAsync(ur => ur.UserId == dto.EmployeeId && ur.RoleId == 3);
            if (!hasEmployeeRole)
            {
                throw new Exception("User này không phải là nhân viên (Employee)");
            }

            var workDateValue = dto.WorkDate ?? DateTime.Now;
            var response = new CreateWorklogBatchResponseVM
            {
                EmployeeId = dto.EmployeeId,
                EmployeeName = employee.FullName ?? string.Empty,
                WorkDate = workDateValue,
                SuccessfulWorklogs = new List<WorklogResponseVM>(),
                FailedWorklogs = new List<WorklogErrorVM>()
            };

            // Xử lý từng job
            foreach (var jobItem in dto.Jobs)
            {
                try
                {
                    // Gọi lại method CreateWorklogAsync đã có
                    var worklog = await CreateWorklogAsync(
                        dto.EmployeeId,
                        jobItem.JobId,
                        jobItem.Quantity,
                        workDateValue,
                        jobItem.Note);

                    response.SuccessfulWorklogs.Add(worklog);
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    // Lấy tên job để hiển thị lỗi
                    var job = await _jobRepository.GetQueryable()
                        .FirstOrDefaultAsync(j => j.Id == jobItem.JobId);

                    response.FailedWorklogs.Add(new WorklogErrorVM
                    {
                        JobId = jobItem.JobId,
                        JobName = job?.JobName ?? $"Job #{jobItem.JobId}",
                        ErrorMessage = ex.Message
                    });
                    response.FailedCount++;
                }
            }

            return response;
        }

        public async Task<List<WorklogResponseVM>> GetWorklogsByEmployeeAndDateAsync(int employeeId, DateTime workDate)
        {
            // Kiểm tra employee tồn tại
            var employee = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == employeeId);
            if (employee == null)
            {
                throw new Exception("Nhân viên không tồn tại");
            }

            // Kiểm tra user có role Employee (RoleId = 3) không
            var hasEmployeeRole = await _userRoleRepository.GetQueryable()
                .AnyAsync(ur => ur.UserId == employeeId && ur.RoleId == 3);
            if (!hasEmployeeRole)
            {
                throw new Exception("User này không phải là nhân viên (Employee)");
            }

            var startOfDay = workDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            // Lấy tất cả worklog của nhân viên trong ngày
            var worklogs = await GetQueryable()
                .Include(w => w.Job)
                .Where(w => w.EmployeeId == employeeId 
                    && w.WorkDate >= startOfDay 
                    && w.WorkDate < endOfDay)
                .OrderBy(w => w.WorkDate)
                .ToListAsync();

            // Map sang WorklogResponseVM
            var result = worklogs.Select(w => new WorklogResponseVM
            {
                Id = w.Id,
                EmployeeId = w.EmployeeId,
                EmployeeName = employee.FullName ?? string.Empty,
                JobId = w.JobId,
                JobName = w.Job.JobName,
                PayType = w.Job.PayType,
                Quantity = w.Quantity,
                Rate = w.Rate,
                TotalAmount = w.Quantity * w.Rate,
                Note = w.Note,
                WorkDate = w.WorkDate
            }).ToList();

            return result;
        }
    }
}

