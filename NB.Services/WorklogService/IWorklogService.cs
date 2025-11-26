using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.WorklogService.Dto;
using NB.Service.WorklogService.ViewModels;

namespace NB.Service.WorklogService
{
    public interface IWorklogService : IService<Worklog>
    {
        Task<WorklogResponseVM> CreateWorklogAsync(int employeeId, int jobId, decimal? quantity, DateTime? workDate, string? note);
        Task<CreateWorklogBatchResponseVM> CreateWorklogBatchAsync(CreateWorklogBatchDto dto);
        Task<List<WorklogResponseVM>> GetWorklogsByEmployeeAndDateAsync(int employeeId, DateTime workDate);
    }
}

