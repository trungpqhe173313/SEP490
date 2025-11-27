using System;

namespace NB.Service.UserService.Dto
{
    /// <summary>
    /// DTO cho khách hàng mua hàng nhiều nhất
    /// </summary>
    public class TopCustomerDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Image { get; set; }
        public decimal TotalSpent { get; set; }
        public int NumberOfOrders { get; set; }
        public decimal? AverageOrderValue { get; set; }
    }
}

