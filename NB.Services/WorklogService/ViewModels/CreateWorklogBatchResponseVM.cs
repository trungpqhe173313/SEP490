namespace NB.Service.WorklogService.ViewModels
{
    public class CreateWorklogBatchResponseVM
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime? WorkDate { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<WorklogResponseVM> SuccessfulWorklogs { get; set; } = new List<WorklogResponseVM>();
        public List<WorklogErrorVM> FailedWorklogs { get; set; } = new List<WorklogErrorVM>();
    }

    public class WorklogErrorVM
    {
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
