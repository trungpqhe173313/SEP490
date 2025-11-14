using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.PriceListService.Dto;
using NB.Service.PriceListService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListService
{
    public interface IPriceListService : IService<PriceList>
    {
        Task<PriceListDto?> GetData(PriceListSearch search);

        Task<List<PriceListDto?>> GetAllData(PriceListSearch search);
    }
}
