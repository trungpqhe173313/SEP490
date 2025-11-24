using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.StockAdjustmentService.Dto;
using NB.Service.StockAdjustmentService.ViewModels;

namespace NB.Service.StockAdjustmentService
{
    public interface IStockAdjustmentService : IService<StockAdjustment>
    {
        Task<PagedList<StockAdjustmentListDto>> GetPagedListAsync(StockAdjustmentSearch search);
        Task<StockAdjustmentDraftResponseVM> CreateDraftAsync(StockAdjustmentDraftCreateVM model);
        Task<StockAdjustmentDraftResponseVM> GetDraftByIdAsync(int id);
        Task<StockAdjustmentDraftResponseVM> UpdateDraftAsync(int id, StockAdjustmentDraftUpdateVM model);
        Task<StockAdjustmentDraftResponseVM> ResolveAsync(int id);
        Task<bool> DeleteDraftAsync(int id);
    }
}

