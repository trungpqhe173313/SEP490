using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService.Dto
{
    public class VerifyOtpResponse
    {
        public string ResetToken { get; set; } = string.Empty; // Token để reset password sau khi verify OTP thành công
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}




