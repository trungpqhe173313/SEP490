using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.JobService.Dto;

namespace NB.Service.JobService
{
    public interface IJobService : IService<Job>
    {
        Task<List<JobDto>> GetAllJobsAsync();
        Task<JobDto> GetJobByIdAsync(int id);
        Task<JobDto> CreateJobAsync(CreateJobDto dto);
        Task<JobDto> UpdateJobAsync(UpdateJobDto dto);
        Task<bool> DeleteJobAsync(int id);
    }
}
