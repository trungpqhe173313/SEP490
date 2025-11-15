using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.PriceListDetailService.Dto;
using NB.Service.PriceListDetailService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.PriceListDetailService
{
    public interface IPriceListDetailService :IService<PriceListDetail>
    {
        Task<List<PriceListDetailOutputVM?>> GetByRange(int rangeFrom, int rangeTo, int PriceListId);

        Task<List<PriceListDetailOutputVM?>> GetByPriceListId(int? priceListId);
        Task<List<PriceListDetailDto?>> GetById(int? priceListId);

    }
}
