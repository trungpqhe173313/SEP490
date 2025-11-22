using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.MaterialService
{
    public class MaterialService : Service<Material>, IMaterialService
    {
        public MaterialService(IRepository<Material> repository) : base(repository)
        {
        }
    }
}
