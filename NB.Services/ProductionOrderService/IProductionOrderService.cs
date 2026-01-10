using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.ProductionOrderService.ViewModels;
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
        Task<PagedList<ProductionOrderDto>> GetDataByResponsibleId(int responsibleId, ProductionOrderSearch search);
        Task<ApiResponse<ProductionOrder>> CreateProductionOrderAsync(ProductionRequest request);
        Task<ApiResponse<FullProductionOrderVM>> GetDetailById(int id);
        Task<ApiResponse<object>> ChangeToProcessingAsync(int id, ChangeToProcessingRequest request, int userId);
        Task<ApiResponse<object>> SubmitForApprovalAsync(int id, SubmitForApprovalRequest request, int userId);
        Task<ApiResponse<object>> ChangeToRejectedAsync(int id, ChangeToRejectedRequest request);
    }
}
