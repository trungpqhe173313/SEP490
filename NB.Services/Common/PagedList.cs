using Microsoft.EntityFrameworkCore;
using NB.Service.Dto;
using NB.Service.TransactionService.Dto;

namespace NB.Service.Common
{
    public class PagedList<T>
    {
        public PagedList(List<T> items, int pageIndex, int pageSize, int totalCount)
        {
            Items = items;
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
        public List<T> Items { get; set; }
        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPage => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> query, SearchBase search)
        {
            var totalCount = await query.CountAsync();
            var items = await query.Skip((search.PageIndex - 1) * search.PageSize).Take(search.PageSize).ToListAsync();
            return new PagedList<T>(items, search.PageIndex, search.PageSize, totalCount);
        }

       
        public static PagedList<T> CreateFromList(List<T> source, int pageIndex, int pageSize)
        {
            var totalCount = source.Count;
            var items = source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedList<T>(items, pageIndex, pageSize, totalCount);
        }

        
        public static PagedList<T> CreateFromList(List<T> source, SearchBase search)
        {
            return CreateFromList(source, search.PageIndex, search.PageSize);
        }

    }
}
