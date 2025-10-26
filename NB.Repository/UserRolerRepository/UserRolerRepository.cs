using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.UserRolerRepository
{
    public class UserRolerRepository : Repository<UserRole>, IUserRolerRepository
    {
        public UserRolerRepository(DbContext context) : base(context)
        {
        }
    }
}
