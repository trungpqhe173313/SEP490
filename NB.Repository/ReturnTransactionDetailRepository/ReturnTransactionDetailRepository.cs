using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.ReturnTransactionDetailRepository
{
    public class ReturnTransactionDetailRepository : Repository<ReturnTransactionDetail>, IReturnTransactionDetailRepository
    {
        public ReturnTransactionDetailRepository(DbContext context) : base(context)
        {
        }
    }
}
