using Microsoft.AspNetCore.Mvc;
using NB.Service.InventoryService;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionService;

namespace NB.API.Controllers
{
    public class StockInputController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;

        public StockInputController(IInventoryService inventoryService, 
                                    ITransactionService transactionService, 
                                    ITransactionDetailService transactionDetailService)
        {
            _inventoryService = inventoryService;
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
        }

    }
}
