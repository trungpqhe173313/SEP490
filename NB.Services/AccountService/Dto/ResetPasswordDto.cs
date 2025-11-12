using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.AccountService.Dto
{
    public class ResetPasswordDto
    {
        public string ResetToken { get; set; } = string.Empty; // Token nhận được sau khi verify OTP
        public string NewPassword { get; set; } = string.Empty;
    }
}


