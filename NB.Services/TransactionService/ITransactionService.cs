using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService
{
    public interface ITransactionService : IService<Transaction>
    {
        Task<List<TransactionDto>> GetById(int? id);
        Task<TransactionDto?> GetByTransactionId(int? id);
        Task<PagedList<TransactionDto>> GetData(TransactionSearch search);
        Task<PagedList<TransactionDto>> GetDataForExport(TransactionSearch search);
        Task<PagedList<TransactionDto>> GetByListStatus(TransactionSearch search, List<int> listStatus);
        Task<TransactionDetailResponseDto> GetTransactionDetailById(int transactionId);
        Task<ImportWeightSummaryDto> GetImportWeightAsync(DateTime fromDate, DateTime toDate);
        Task<ExportWeightSummaryDto> GetExportWeightAsync(DateTime fromDate, DateTime toDate);

        // New approval workflow methods
        Task<SubmitForApprovalResult> SubmitForApprovalAsync(int transactionId, SubmitForApprovalVM model);
        Task<ApproveImportResult> ApproveImportAsync(int transactionId, ApproveImportVM model);
        Task<RejectImportResult> RejectImportAsync(int transactionId, RejectImportVM model);
    }
}
