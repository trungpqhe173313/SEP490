using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.StockAdjustmentService.ViewModels;

namespace NB.Service.StockAdjustmentService
{
    public interface IStockAdjustmentService : IService<StockAdjustment>
    {
        Task<StockAdjustmentDraftResponseVM> CreateDraftAsync(StockAdjustmentDraftCreateVM model);
    }
}

