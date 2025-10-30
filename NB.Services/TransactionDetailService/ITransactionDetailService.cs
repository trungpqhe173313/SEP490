using NB.Model.Entities;
using NB.Service.Common;
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
    }
}
