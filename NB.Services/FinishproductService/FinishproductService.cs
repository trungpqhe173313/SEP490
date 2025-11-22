using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.FinishproductService
{
    public class FinishproductService : Service<Finishproduct>, IFinishproductService
    {
        public FinishproductService(IRepository<Finishproduct> repository) : base(repository)
        {
        }
    }
}
