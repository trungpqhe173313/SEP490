using NB.Model.Enums;
using NB.Service.Core.Enum;

namespace NB.Service.PayrollService.Dto
{
    public class CreatePayrollDto
    {
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string? Note { get; set; }
    }

    public class PayrollOverviewDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = PayrollStatus.NotGenerated.GetDescription();
        public int? PayrollId { get; set; }
        public DateTime? PaidDate { get; set; }
        public List<JobDetailDto> JobDetails { get; set; } = new List<JobDetailDto>();
    }

    public class JobDetailDto
    {
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty; // Per_Ngay, Per_Tan
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; } // = Quantity * Rate
    }
}
