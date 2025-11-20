using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ReturnTransactionDetailService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionDetailService
{
    public interface IReturnTransactionDetailService : IService<ReturnTransactionDetail>
    {
        Task<List<ReturnTransactionDetailDto>> GetByReturnTransactionId(int returnTransactionId);
    }
}
