using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.TransactionDetailService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionDetailService
{
    public class TransactionDetailService : Service<TransactionDetail>, ITransactionDetailService
    {
        public TransactionDetailService(IRepository<TransactionDetail> repository) : base(repository)
        {
        }

        public Task<bool> DeleteByTransactionId(int transactionId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<TransactionDetail>> GetById(int Id)
        {
            var query = from td in GetQueryable()
                        where td.TransactionId == Id
                        select new TransactionDetail()
                        {
                            Id = td.Id,
                            TransactionId = td.TransactionId,
                            ProductId = td.ProductId,
                            Quantity = td.Quantity,
                            UnitPrice = td.UnitPrice
                        };
            return await query.ToListAsync();
        }

        public async Task<List<TransactionDetailDto>> GetByTransactionId(int? Id)
        {
            var query = from td in GetQueryable()
                        where td.TransactionId == Id
                        select new TransactionDetailDto()
                        {
                            Id = td.Id,
                            TransactionId = td.TransactionId,
                            ProductName = td.Product!.ProductName,
                            ProductId = td.ProductId,
                            ImageUrl = td.Product.ImageUrl,
                            WeightPerUnit = td.Product.WeightPerUnit,
                            Quantity = td.Quantity,
                            UnitPrice = td.UnitPrice
                        };

            query = query.OrderByDescending(td => td.Id);
            return await query.ToListAsync();
        }

        public async Task<bool> HasProductInActiveExportOrders(int productId)
        {
            // Kiểm tra xem có đơn xuất nào có sản phẩm này với trạng thái không phải:
            // - draft (nháp)
            // - paidInFull (đã thanh toán đủ)
            // - partiallyPaid (thanh toán một phần)
            var allowedStatuses = new[]
            {
                (int)TransactionStatus.draft,
                (int)TransactionStatus.paidInFull,
                (int)TransactionStatus.partiallyPaid
            };

            return await GetQueryable()
                .AnyAsync(td => td.ProductId == productId &&
                               td.Transaction != null &&
                               td.Transaction.Type == "Export" &&
                               !allowedStatuses.Contains(td.Transaction.Status ?? 0));
        }
    }
}
