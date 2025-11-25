using System.Security.Cryptography;
using System.Text;


namespace NB.Service.Common
{
    /// <summary>
    /// Service để hash và verify password sử dụng BCrypt
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Hash password sử dụng BCrypt
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Sử dụng BCrypt với work factor 12 (cân bằng giữa security và performance)
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        /// <summary>
        /// Verify password với BCrypt hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem password có phải là BCrypt hash không
        /// BCrypt hash bắt đầu với $2a$, $2b$, $2y$
        /// </summary>
        public static bool IsBCryptHash(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            return password.StartsWith("$2a$") || 
                   password.StartsWith("$2b$") || 
                   password.StartsWith("$2y$");
        }
    }
}

