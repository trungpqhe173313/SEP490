namespace NB.Service.JobService.Dto
{
    public class JobDto
    {
        public int Id { get; set; }
        public string JobName { get; set; } = null!;
        public string PayType { get; set; } = null!;
        public decimal Rate { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
