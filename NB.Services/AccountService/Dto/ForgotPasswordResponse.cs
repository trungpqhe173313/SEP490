using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService.Dto
{
    public class ForgotPasswordResponse
    {
        public string OtpCode { get; set; } = string.Empty; // Chỉ trả về trong development, production sẽ gửi qua email
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


