using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ProductionOrderService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductionOrderService
{
    public interface IProductionOrderService : IService<ProductionOrder>
    {
        Task<PagedList<ProductionOrderDto>> GetData(ProductionOrderSearch search);
    }
}
