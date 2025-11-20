using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.ReturnTransactionDetailService.Dto;
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

        public async Task<List<ReturnTransactionDetailDto>> GetByReturnTransactionId(int returnTransactionId)
        {
            var query = from rtd in GetQueryable()
                       where rtd.ReturnTransactionId == returnTransactionId
                       select new ReturnTransactionDetailDto
                       {
                           Id = rtd.Id,
                           ReturnTransactionId = rtd.ReturnTransactionId,
                           ProductId = rtd.ProductId,
                           Quantity = rtd.Quantity
                       };

            return await query.ToListAsync();
        }
    }
}
