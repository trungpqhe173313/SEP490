using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
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
    public class PriceListDetailService : Service<PriceListDetail>, IPriceListDetailService
    {
        private readonly IRepository<PriceListDetail> _priceListDetailRepository;

        public PriceListDetailService(IRepository<PriceListDetail> priceListDetailRepository) : base(priceListDetailRepository)
        {
            _priceListDetailRepository = priceListDetailRepository;
        }

        public async Task<List<PriceListDetailOutputVM?>> GetByRange(int rangeFrom, int rangeTo, int PriceListId)
        {
            var query = from pld in GetQueryable()
                        where pld.Price <= rangeFrom
                        && pld.Price >= rangeTo
                        && pld.PriceListId == PriceListId
                        select new PriceListDetailOutputVM
                        {
                            PriceListDetailId = pld.PriceListDetailId,
                            ProductId = pld.ProductId,
                            ProductName = pld.Product.ProductName,
                            ProductCode = pld.Product.ProductCode,
                            Price = pld.Price,
                            Note = pld.Note
                        };
            query = query.OrderByDescending(x => x.Price);
            return await query.ToListAsync();
        }

        public async Task<List<PriceListDetailOutputVM?>> GetByPriceListId(int? priceListId)
        {
            var query = from pld in GetQueryable()
                        .Include(p => p.Product)
                        where pld.PriceListId == priceListId
                        select new PriceListDetailOutputVM
                        {
                            PriceListDetailId = pld.PriceListDetailId,
                            ProductId = pld.ProductId,
                            ProductName = pld.Product.ProductName,
                            ProductCode = pld.Product.ProductCode,
                            Price = pld.Price,
                            Note = pld.Note
                        };
            query = query.OrderByDescending(x => x.Price);
            return await query.ToListAsync();
        }

        public async Task<List<PriceListDetailDto?>> GetById(int? priceListId)
        {
            var query = from pld in GetQueryable()
                        where pld.PriceListId == priceListId
                        select new PriceListDetailDto
                        {
                            PriceListDetailId = pld.PriceListDetailId,
                            PriceListId = pld.PriceListId,
                            ProductId = pld.ProductId,
                            Price = pld.Price,
                            Note = pld.Note
                        };
            return await query.ToListAsync();
        }
    }
}
