using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
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
    public class PriceListService : Service<PriceList>, IPriceListService
    {
        private readonly IRepository<PriceList> _PriceListRepository;

        public PriceListService(IRepository<PriceList> priceListRepository) : base(priceListRepository)
        {
            _PriceListRepository = priceListRepository;
        }

        public async Task<PriceListDto?> GetData(PriceListSearch search)
        {
            var query = from pl in GetQueryable()
                        select new PriceListDto{
                            PriceListId = pl.PriceListId,
                            PriceListName = pl.PriceListName,
                            IsActive = pl.IsActive,
                            StartDate = pl.StartDate,
                            EndDate = pl.EndDate,
                            CreatedAt = pl.CreatedAt
                        };
            if (search.PriceListId.HasValue)
            {
                if(search.PriceListId.Value > 0)
                {
                    query = query.Where(x => x.PriceListId == search.PriceListId.Value);
                }

                if(search.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == search.IsActive.Value);
                }

                if(!string.IsNullOrEmpty(search.PriceListName))
                {
                    var keyword = search.PriceListName.Trim();
                    query = query.Where(x => EF.Functions.Collate(x.PriceListName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }

                if(search.FromDate.HasValue)
                {
                    query = query.Where(x => x.StartDate >= search.FromDate.Value);
                }

                if(search.ToDate.HasValue)
                {
                    query = query.Where(x => x.EndDate <= search.ToDate.Value);
                }
            }
            query = query.OrderByDescending(x => x.CreatedAt);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<PriceListDto?>> GetAllData(PriceListSearch search)
        {
            var query = from pl in GetQueryable()
                        select new PriceListDto
                        {
                            PriceListId = pl.PriceListId,
                            PriceListName = pl.PriceListName,
                            IsActive = pl.IsActive,
                            StartDate = pl.StartDate,
                            EndDate = pl.EndDate,
                            CreatedAt = pl.CreatedAt
                        };
            if (search != null)
            {
                if (search.PriceListId.HasValue && search.PriceListId.Value > 0)
                {
                    query = query.Where(x => x.PriceListId == search.PriceListId.Value);
                }
                if (!string.IsNullOrEmpty(search.PriceListName))
                {
                    var keyword = search.PriceListName.Trim();
                    query = query.Where(x => EF.Functions.Collate(x.PriceListName, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (search.FromDate.HasValue)
                {
                    query = query.Where(x => x.StartDate >= search.FromDate.Value);
                }
                if (search.ToDate.HasValue)
                {
                    query = query.Where(x => x.EndDate <= search.ToDate.Value);
                }
            }
            query = query.OrderByDescending(x => x.CreatedAt);
            return await query.ToListAsync();
        }
    }
}
