using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.TransactionDetailRepository
{
    public class TransactionDetailRepository : Repository<TransactionDetail>, ITransactionDetailRepository
    {
        public TransactionDetailRepository(DbContext context) : base(context)
        {
        }
    }
}
