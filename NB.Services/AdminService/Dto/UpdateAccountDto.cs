using System;
using System.Collections.Generic;

namespace NB.Service.AdminService.Dto
{
    public class UpdateAccountDto
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<string>? Roles { get; set; }
        public bool? IsActive { get; set; }
        public string? FullName { get; set; }
        // Thêm các trường khác nếu cần thiết
        public string? Password { get; set; }
    }
}