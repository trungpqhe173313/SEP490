using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.TransactionDetailService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionDetailService
{
    public interface ITransactionDetailService : IService<TransactionDetail>
    {
        Task<List<TransactionDetail>> GetById(int Id);


        Task<List<TransactionDetailDto>> GetByTransactionId(int? Id);
        Task<bool> DeleteByTransactionId(int transactionId);
        Task<bool> HasProductInActiveExportOrders(int productId);
    }
}
