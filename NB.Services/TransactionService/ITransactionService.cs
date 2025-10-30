using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.TransactionService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService
{
    public interface ITransactionService : IService<Transaction>
    {
        Task<List<TransactionDto>> GetById(int? id);
    }
}
