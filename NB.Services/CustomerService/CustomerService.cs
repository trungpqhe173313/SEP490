using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CustomerService
{
    public class CustomerService : Service<User>, ICustomerService
    {
        public CustomerService(IRepository<User> repository) : base(repository)
        {
        }
    }
}
