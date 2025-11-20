using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ReturnTransactionService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionService
{
    public interface IReturnTransactionService : IService<ReturnTransaction>
    {
        Task<PagedList<ReturnOrderDto>> GetData(ReturnOrderSearch search);
    }
}
