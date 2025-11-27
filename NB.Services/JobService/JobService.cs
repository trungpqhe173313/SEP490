using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.JobService.Dto;

namespace NB.Service.JobService
{
    public class JobService : Service<Job>, IJobService
    {
        public JobService(IRepository<Job> repository) : base(repository)
        {
        }

        public async Task<List<JobDto>> GetAllJobsAsync()
        {
            var jobs = await GetQueryable()
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return jobs.Select(j => new JobDto
            {
                Id = j.Id,
                JobName = j.JobName,
                PayType = j.PayType,
                Rate = j.Rate,
                IsActive = j.IsActive,
                CreatedAt = j.CreatedAt
            }).ToList();
        }

        public async Task<JobDto> GetJobByIdAsync(int id)
        {
            var job = await GetQueryable()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                throw new Exception("Công việc không tồn tại");
            }

            return new JobDto
            {
                Id = job.Id,
                JobName = job.JobName,
                PayType = job.PayType,
                Rate = job.Rate,
                IsActive = job.IsActive,
                CreatedAt = job.CreatedAt
            };
        }

        public async Task<JobDto> CreateJobAsync(CreateJobDto dto)
        {
            // Kiểm tra tên công việc đã tồn tại chưa
            var existingJob = await GetQueryable()
                .FirstOrDefaultAsync(j => j.JobName.ToLower() == dto.JobName.ToLower());

            if (existingJob != null)
            {
                throw new Exception("Tên công việc đã tồn tại");
            }

            var job = new Job
            {
                JobName = dto.JobName,
                PayType = dto.PayType,
                Rate = dto.Rate,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await CreateAsync(job);

            return new JobDto
            {
                Id = job.Id,
                JobName = job.JobName,
                PayType = job.PayType,
                Rate = job.Rate,
                IsActive = job.IsActive,
                CreatedAt = job.CreatedAt
            };
        }

        public async Task<JobDto> UpdateJobAsync(UpdateJobDto dto)
        {
            var job = await GetQueryable()
                .FirstOrDefaultAsync(j => j.Id == dto.Id);

            if (job == null)
            {
                throw new Exception("Công việc không tồn tại");
            }

            // Kiểm tra tên công việc đã tồn tại chưa (trừ chính nó)
            var existingJob = await GetQueryable()
                .FirstOrDefaultAsync(j => j.JobName.ToLower() == dto.JobName.ToLower() && j.Id != dto.Id);

            if (existingJob != null)
            {
                throw new Exception("Tên công việc đã tồn tại");
            }

            job.JobName = dto.JobName;
            job.PayType = dto.PayType;
            job.Rate = dto.Rate;
            job.IsActive = dto.IsActive ?? job.IsActive;

            await UpdateAsync(job);

            return new JobDto
            {
                Id = job.Id,
                JobName = job.JobName,
                PayType = job.PayType,
                Rate = job.Rate,
                IsActive = job.IsActive,
                CreatedAt = job.CreatedAt
            };
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            var job = await GetQueryable()
                .Include(j => j.Worklogs)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                throw new Exception("Công việc không tồn tại");
            }
            job.IsActive = false;
            await UpdateAsync(job);
            return true;
        }
    }
}
