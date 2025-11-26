namespace NB.Service.WorklogService.ViewModels
{
    public class WorklogResponseVM
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal TotalAmount { get; set; } // = Quantity * Rate
        public string? Note { get; set; }
        public DateTime? WorkDate { get; set; }
    }
}
