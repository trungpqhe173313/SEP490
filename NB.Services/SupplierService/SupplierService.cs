using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService
{
    public class SupplierService : Service<Supplier>, ISupplierService
    {
        public SupplierService(IRepository<Supplier> repository) : base(repository)
        {
        }
    }
}
