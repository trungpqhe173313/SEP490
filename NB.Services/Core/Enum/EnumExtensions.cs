using System.ComponentModel;
using System.Reflection;

namespace NB.Service.Core.Enum
{
    /// <summary>
    /// Cung cấp các phương thức mở rộng (extension methods) cho kiểu Enum.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Lấy ra nội dung mô tả (Description) của một giá trị enum, nếu enum đó có gán <see cref="DescriptionAttribute"/>.
        /// </summary>
        /// <param name="value">Giá trị enum cần lấy mô tả.</param>
        /// <returns>
        /// Chuỗi mô tả được định nghĩa trong <see cref="DescriptionAttribute"/> của enum. 
        /// Nếu không có, trả về tên mặc định của giá trị enum.
        /// </returns>
        public static string GetDescription(this System.Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }
    }
}
