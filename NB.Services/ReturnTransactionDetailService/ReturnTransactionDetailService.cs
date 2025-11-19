using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionDetailService
{
    public class ReturnTransactionDetailService : Service<ReturnTransactionDetail>, IReturnTransactionDetailService
    {
        public ReturnTransactionDetailService(IRepository<ReturnTransactionDetail> repository) : base(repository)
        {
        }
    }
}
