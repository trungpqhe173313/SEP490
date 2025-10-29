using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
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
    }
}
