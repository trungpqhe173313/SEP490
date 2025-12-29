using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using static System.DateTime;

namespace NB.Service.Core
{
    /// <summary>
    /// Helper class để tạo mã TransactionCode duy nhất cho các giao dịch
    /// </summary>
    public class TransactionCodeGenerator
    {
        private readonly IRepository<Transaction> _transactionRepository;

        public TransactionCodeGenerator(IRepository<Transaction> transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        /// <summary>
        /// Tạo mã TransactionCode duy nhất
        /// Format: [TYPE]-YYYYMMDD-XXXX (XXXX là số thứ tự trong ngày)
        /// </summary>
        /// <param name="type">Loại giao dịch: Import, Export, Transfer</param>
        /// <returns>Mã TransactionCode duy nhất</returns>
        public async Task<string> GenerateTransactionCode(string type)
        {
            var today = Now.Date;
            var tomorrow = today.AddDays(1);
            var datePrefix = $"{type.ToUpper()}-{Now:yyyyMMdd}";

            // Đếm số lượng transaction cùng loại trong ngày
            var countToday = await _transactionRepository.GetQueryable()
                .Where(t => t.Type == type && t.TransactionDate >= today && t.TransactionDate < tomorrow)
                .CountAsync();

            // Tạo mã với số thứ tự (bắt đầu từ 1)
            var sequenceNumber = (countToday + 1).ToString("D4");
            return $"{datePrefix}-{sequenceNumber}";
        }
    }
}
