using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NB.Service.Core.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _smtpFromEmail;
        private readonly string _smtpFromName;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
            _smtpFromEmail = _configuration["Email:FromEmail"] ?? "";
            _smtpFromName = _configuration["Email:FromName"] ?? "NutriBarn";
            _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpFromName, _smtpFromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, _enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string userName = "")
        {
            var displayName = string.IsNullOrEmpty(userName) ? "Người dùng" : userName;
            var subject = "Mã OTP đặt lại mật khẩu - NutriBarn";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background-color: #f9f9f9;
            border-radius: 10px;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            background-color: #4CAF50;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 10px 10px 0 0;
        }}
        .otp-code {{
            background-color: #fff;
            border: 2px dashed #4CAF50;
            border-radius: 5px;
            padding: 20px;
            text-align: center;
            font-size: 32px;
            font-weight: bold;
            color: #4CAF50;
            letter-spacing: 5px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 12px;
            color: #666;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Đặt lại mật khẩu</h1>
        </div>
        <p>Xin chào {displayName},</p>
        <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình. Vui lòng sử dụng mã OTP sau đây:</p>
        
        <div class='otp-code'>
            {otpCode}
        </div>
        
        <div class='warning'>
            <strong>Lưu ý:</strong>
            <ul>
                <li>Mã OTP này có hiệu lực trong <strong>10 phút</strong></li>
                <li>Mã OTP chỉ được sử dụng một lần</li>
                <li>Không chia sẻ mã OTP này với bất kỳ ai</li>
            </ul>
        </div>
        
        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
        
        <div class='footer'>
            <p>Trân trọng,<br>Đội ngũ NutriBarn</p>
            <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody, true);
        }

        public async Task<bool> SendNewAccountEmailAsync(string toEmail, string username, string password)
        {
            var subject = "Tài khoản của bạn đã được tạo - NutriBarn";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background-color: #f9f9f9;
            border-radius: 10px;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            background-color: #4CAF50;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 10px 10px 0 0;
        }}
        .content {{
            background-color: #fff;
            padding: 20px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .credentials {{
            background-color: #f0f8ff;
            border-left: 4px solid #4CAF50;
            padding: 15px;
            margin: 15px 0;
        }}
        .credentials-item {{
            margin: 10px 0;
        }}
        .credentials-label {{
            font-weight: bold;
            color: #555;
        }}
        .credentials-value {{
            font-size: 16px;
            color: #000;
            margin-left: 10px;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 15px 0;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 12px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Chào mừng đến với NutriBarn!</h1>
        </div>
        <div class='content'>
            <p>Xin chào,</p>
            <p>Tài khoản của bạn đã được tạo thành công trên hệ thống NutriBarn. Dưới đây là thông tin đăng nhập của bạn:</p>

            <div class='credentials'>
                <div class='credentials-item'>
                    <span class='credentials-label'>Username:</span>
                    <span class='credentials-value'>{username}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Email:</span>
                    <span class='credentials-value'>{toEmail}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Mật khẩu:</span>
                    <span class='credentials-value'>{password}</span>
                </div>
            </div>

            <div class='warning'>
                <strong>⚠️ Quan trọng:</strong>
                <ul>
                    <li>Vui lòng <strong>đổi mật khẩu</strong> ngay sau lần đăng nhập đầu tiên để bảo mật tài khoản</li>
                    <li>Không chia sẻ thông tin đăng nhập với bất kỳ ai</li>
                    <li>Giữ mật khẩu của bạn ở nơi an toàn</li>
                </ul>
            </div>

            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>
        </div>

        <div class='footer'>
            <p>Trân trọng,<br>Đội ngũ NutriBarn</p>
            <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody, true);
        }
    }
}

