namespace NB.Service.WorklogService.ViewModels
{
    public class CreateWorklogBatchResponseVM
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime? WorkDate { get; set; }
        public int TotalCount { get; set; }
        public List<WorklogResponseVM> Worklogs { get; set; } = new List<WorklogResponseVM>();
    }
}
