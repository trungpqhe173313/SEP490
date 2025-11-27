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
            if (employee == null)
            {
                throw new Exception("Nhân viên không tồn tại");
            }

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
                Note = note,
                IsActive = false // Chưa xác nhận chấm công
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
                WorkDate = worklog.WorkDate,
                IsActive = worklog.IsActive
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
            var startOfDay = workDateValue.Date;
            var endOfDay = startOfDay.AddDays(1);

            //VALIDATE TẤT CẢ JOBS TRƯỚC =====
            var validationErrors = new List<string>();
            
            foreach (var jobItem in dto.Jobs)
            {
                // Kiểm tra job tồn tại
                var job = await _jobRepository.GetQueryable()
                    .FirstOrDefaultAsync(j => j.Id == jobItem.JobId);
                
                if (job == null)
                {
                    validationErrors.Add($"Job #{jobItem.JobId}: Công việc không tồn tại");
                    continue;
                }

                if (job.IsActive != true)
                {
                    validationErrors.Add($"Job '{job.JobName}': Công việc không còn hoạt động");
                    continue;
                }

                // Kiểm tra đã tồn tại worklog cho employee + job + ngày này chưa
                var existingWorklog = await GetQueryable()
                    .FirstOrDefaultAsync(w => w.EmployeeId == dto.EmployeeId 
                        && w.JobId == jobItem.JobId 
                        && w.WorkDate >= startOfDay 
                        && w.WorkDate < endOfDay);

                if (existingWorklog != null)
                {
                    validationErrors.Add($"Job '{job.JobName}': Nhân viên đã có worklog cho công việc này trong ngày");
                    continue;
                }

                // Kiểm tra Quantity theo PayType
                if (job.PayType == "Per_Tan")
                {
                    if (!jobItem.Quantity.HasValue || jobItem.Quantity.Value <= 0)
                    {
                        validationErrors.Add($"Job '{job.JobName}': Vui lòng nhập số tấn (Quantity) cho công việc tính theo tấn");
                    }
                }
            }

            // Nếu có lỗi validation → THROW EXCEPTION, KHÔNG TẠO GÌ CẢ
            if (validationErrors.Any())
            {
                throw new Exception(string.Join("; ", validationErrors));
            }

            // TẤT CẢ ĐÃ OK → TẠO TẤT CẢ WORKLOG =====
            var successfulWorklogs = new List<WorklogResponseVM>();
            
            foreach (var jobItem in dto.Jobs)
            {
                // Lấy lại job (đã validate ở phase 1)
                var job = await _jobRepository.GetQueryable()
                    .FirstOrDefaultAsync(j => j.Id == jobItem.JobId);

                decimal quantityValue = job!.PayType == "Per_Ngay" ? 1 : jobItem.Quantity!.Value;

                // Tạo worklog mới
                var worklog = new Worklog
                {
                    EmployeeId = dto.EmployeeId,
                    JobId = jobItem.JobId,
                    Quantity = quantityValue,
                    Rate = job.Rate,
                    WorkDate = workDateValue,
                    Note = jobItem.Note,
                    IsActive = false // Chưa xác nhận chấm công
                };

                await CreateAsync(worklog);

                // Thêm vào response
                successfulWorklogs.Add(new WorklogResponseVM
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
                    WorkDate = worklog.WorkDate,
                    IsActive = worklog.IsActive
                });
            }

            return new CreateWorklogBatchResponseVM
            {
                EmployeeId = dto.EmployeeId,
                EmployeeName = employee.FullName ?? string.Empty,
                WorkDate = workDateValue,
                TotalCount = successfulWorklogs.Count,
                Worklogs = successfulWorklogs
            };
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
                WorkDate = w.WorkDate,
                IsActive = w.IsActive
            }).ToList();

            return result;
        }

        public async Task<List<WorklogResponseVM>> GetWorklogsByDateAsync(DateTime workDate)
        {
            var startOfDay = workDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            // Lấy tất cả worklog trong ngày của tất cả nhân viên
            var worklogs = await GetQueryable()
                .Include(w => w.Job)
                .Include(w => w.Employee)
                .Where(w => w.WorkDate >= startOfDay && w.WorkDate < endOfDay)
                .OrderBy(w => w.Employee.FullName)
                .ThenBy(w => w.WorkDate)
                .ToListAsync();

            // Map sang WorklogResponseVM
            var result = worklogs.Select(w => new WorklogResponseVM
            {
                Id = w.Id,
                EmployeeId = w.EmployeeId,
                EmployeeName = w.Employee.FullName ?? string.Empty,
                JobId = w.JobId,
                JobName = w.Job.JobName,
                PayType = w.Job.PayType,
                Quantity = w.Quantity,
                Rate = w.Rate,
                TotalAmount = w.Quantity * w.Rate,
                Note = w.Note,
                WorkDate = w.WorkDate,
                IsActive = w.IsActive
            }).ToList();

            return result;
        }

        public async Task<WorklogResponseVM> GetWorklogByIdAsync(int id)
        {
            var worklog = await GetQueryable()
                .Include(w => w.Job)
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (worklog == null)
            {
                throw new Exception("Worklog không tồn tại");
            }

            return new WorklogResponseVM
            {
                Id = worklog.Id,
                EmployeeId = worklog.EmployeeId,
                EmployeeName = worklog.Employee.FullName ?? string.Empty,
                JobId = worklog.JobId,
                JobName = worklog.Job.JobName,
                PayType = worklog.Job.PayType,
                Quantity = worklog.Quantity,
                Rate = worklog.Rate,
                TotalAmount = worklog.Quantity * worklog.Rate,
                Note = worklog.Note,
                WorkDate = worklog.WorkDate,
                IsActive = worklog.IsActive
            };
        }

        public async Task<WorklogResponseVM> UpdateWorklogAsync(UpdateWorklogDto dto)
        {
            var worklog = await GetQueryable()
                .Include(w => w.Job)
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.Id == dto.Id);

            if (worklog == null)
            {
                throw new Exception("Worklog không tồn tại");
            }

            // Kiểm tra job
            var job = await _jobRepository.GetQueryable()
                .FirstOrDefaultAsync(j => j.Id == worklog.JobId);

            if (job == null)
            {
                throw new Exception("Công việc không tồn tại");
            }

            // Xử lý Quantity theo PayType
            if (job.PayType == "Per_Tan")
            {
                // Chỉ cho phép sửa Quantity khi PayType = Per_Tan
                if (dto.Quantity.HasValue)
                {
                    if (dto.Quantity.Value <= 0)
                    {
                        throw new Exception("Số tấn phải lớn hơn 0");
                    }
                    worklog.Quantity = dto.Quantity.Value;
                }
            }
            // Nếu PayType = Per_Ngay thì giữ nguyên Quantity = 1, bỏ qua dto.Quantity

            // Cập nhật Note
            worklog.Note = dto.Note;

            await UpdateAsync(worklog);

            return new WorklogResponseVM
            {
                Id = worklog.Id,
                EmployeeId = worklog.EmployeeId,
                EmployeeName = worklog.Employee.FullName ?? string.Empty,
                JobId = worklog.JobId,
                JobName = job.JobName,
                PayType = job.PayType,
                Quantity = worklog.Quantity,
                Rate = worklog.Rate,
                TotalAmount = worklog.Quantity * worklog.Rate,
                Note = worklog.Note,
                WorkDate = worklog.WorkDate,
                IsActive = worklog.IsActive
            };
        }
    }
}

