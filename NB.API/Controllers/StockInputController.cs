using Microsoft.AspNetCore.Mvc;
using NB.Repository.WarehouseRepository;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionService;
using NB.Service.WarehouseService;

namespace NB.API.Controllers
{
    [Route("api/stockinput")]
    public class StockInputController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IWarehouseService _warehouseService;
        private readonly IProductService _productService;
        private readonly IStockBatchService _stockBatchService;
        private readonly ILogger<StockInputController> _logger;
        private readonly IMapper _mapper;

        public StockInputController(IInventoryService inventoryService, 
                                    ITransactionService transactionService, 
                                    ITransactionDetailService transactionDetailService,
                                    IWarehouseService warehouseService,
                                    IProductService productService,
                                    IStockBatchService stockBatchService,
                                    ILogger<StockInputController> logger,
                                    IMapper mapper)
        {
            _inventoryService = inventoryService;
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _warehouseService = warehouseService;
            _productService = productService;
            _stockBatchService = stockBatchService;
            _logger = logger;
            _mapper = mapper;
        }
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] StockBatchSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            try
            {
                var list = await _stockBatchService.GetData(search);
                List<StockOutputVM>? result = new List<StockOutputVM>();
                if (list != null)
                {
                    foreach (var item in list.Items)
                    {
                        StockOutputVM resultItem = new StockOutputVM();
                        resultItem.BatchId = item.BatchId;
                        resultItem.WarehouseName = (await _warehouseService.GetById(item.WarehouseId))?.WarehouseName;
                        resultItem.ProductName = (await _productService.GetById(item.ProductId))?.ProductName;
                        resultItem.TransactionId = item.TransactionId;
                        resultItem.ProductionFinishName = item.ProductionFinishId != null ? (await _productService.GetById(item.ProductionFinishId.Value))?.ProductName : null;
                        resultItem.BatchCode = item.BatchCode;
                        resultItem.ImportDate = item.ImportDate;
                        resultItem.ExpireDate = item.ExpireDate;
                        resultItem.QuantityIn = item.QuantityIn;
                        resultItem.Status = item.Status;
                        resultItem.IsActive = item.IsActive ?? false;
                        resultItem.Note = item.Note;
                        result.Add(resultItem);
                    }

                }
                else if (result == null || result.Count == 0)
                {
                    return NotFound(ApiResponse<PagedList<StockOutputVM>>.Fail("Không tìm thấy lô hàng nào.", 404));
                }
                var pagedResult = PagedList<StockOutputVM>.CreateFromList(result, search);
                return Ok(ApiResponse<PagedList<StockOutputVM>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu lô hàng");
                return BadRequest(ApiResponse<PagedList<StockOutputVM>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        
            
        [HttpPost("CreateStockInputs")]
        public async Task<IActionResult> CreateStockInputs([FromBody] StockBatchCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (model.WarehouseId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"ID kho {model.WarehouseId} không hợp lệ", 400));
            }
            var existWarehouse = await _warehouseService.GetById(model.WarehouseId);
            if (existWarehouse == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy kho với ID: {model.WarehouseId}", 404));
            }

            if (model.TransactionId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"ID đơn nhập {model.TransactionId} không hợp lệ", 400));
            }
            var existTransaction = await _transactionService.GetById(model.TransactionId);
            if (existTransaction.Count == 0)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy đơn nhập với ID: {model.TransactionId}", 404));
            }
            if (model.ProductionFinishId != null)
            {
                return BadRequest(ApiResponse<object>.Fail("Đơn nhập không có sản phẩm sau xử lí.", 400));
            }


            if (model.BatchCode == null || model.BatchCode.Trim().Replace(" ", "").Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã lô không được để trống.", 400));
            }
            var existBatchCode = $"{model.BatchCode}{1:D4}";
            if(await _stockBatchService.GetByName(existBatchCode) != null)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã lô này đã tồn tại"));
            }

            if (model.ExpireDate == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Ngày hết hạn không được để trống.", 400));
            }

            if (model.ExpireDate <= DateTime.UtcNow)
            {
                return BadRequest(ApiResponse<object>.Fail("Ngày hết hạn phải sau ngày nhập.", 400));
            }


            try
            {
                var listTransactionDetails = await _transactionDetailService.GetById(model.TransactionId);
                int batchCounter = 1;
                List<StockOutputVM> list = new List<StockOutputVM>();
                foreach (var item in listTransactionDetails)
                {
                    
                    if (await _productService.GetById(item.ProductId) == null)
                    {
                        return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm với ID: {item.ProductId}", 404));
                    }else if (item.Quantity <= 0)
                    {
                        return BadRequest(ApiResponse<object>.Fail($"Số lượng sản phẩm với ID: {item.ProductId} không hợp lệ", 400));
                    }
                    else
                    {
                        string uniqueBatchCode = $"{model.BatchCode.Trim().Replace(" ", "")}{batchCounter:D4}"; // D4 = 4 chữ số: 0001, 0002, 0003...
                        var newStockBatch = _mapper.Map<StockBatchCreateVM, StockBatchDto>(model);
                        {
                            newStockBatch.WarehouseId = model.WarehouseId;
                            newStockBatch.ProductId = item.ProductId;
                            newStockBatch.TransactionId = model.TransactionId;
                            newStockBatch.BatchCode = uniqueBatchCode;
                            newStockBatch.ImportDate = DateTime.UtcNow;
                            newStockBatch.ExpireDate = model.ExpireDate;
                            newStockBatch.QuantityIn = item.Quantity;
                            newStockBatch.Status = 1; // Đặt trạng thái là 'Đã nhập kho'
                            newStockBatch.IsActive = true;
                            newStockBatch.LastUpdated = DateTime.UtcNow;
                            newStockBatch.Note = model.Note;
                        }

                        await _stockBatchService.CreateAsync(newStockBatch);

                        var existInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, item.ProductId);
                        if (existInventory == null)
                        {
                            // Tạo mới tồn kho nếu chưa có
                            var newInventory = _mapper.Map<StockBatchCreateVM, InventoryDto>(model);
                            {
                                newInventory.WarehouseId = model.WarehouseId;
                                newInventory.ProductId = item.ProductId;
                                newInventory.Quantity = item.Quantity;
                                newInventory.AverageCost = newInventory.AverageCost; // Giữ nguyên giá vốn trung bình ban đầu
                                newInventory.LastUpdated = DateTime.UtcNow;
                            }
                            ;
                            await _inventoryService.CreateAsync(newInventory);

                        }
                        else
                        {
                            // Cập nhật tồn kho nếu đã có
                            existInventory.AverageCost = existInventory.AverageCost;
                            existInventory.Quantity += item.Quantity;
                            existInventory.LastUpdated = DateTime.UtcNow;
                            await _inventoryService.UpdateAsync(existInventory);
                        }
                        StockOutputVM resultItem = new StockOutputVM();
                        resultItem.BatchId = newStockBatch.BatchId;
                        resultItem.WarehouseName = (await _warehouseService.GetById(newStockBatch.WarehouseId))?.WarehouseName;
                        resultItem.ProductName = (await _productService.GetById(newStockBatch.ProductId))?.ProductName;
                        resultItem.TransactionId = newStockBatch.TransactionId;
                        resultItem.ProductionFinishName = newStockBatch.ProductionFinishId != null ? (await _productService.GetById(newStockBatch.ProductionFinishId.Value))?.ProductName : null;
                        resultItem.BatchCode = newStockBatch.BatchCode;
                        resultItem.ImportDate = newStockBatch.ImportDate;
                        resultItem.ExpireDate = newStockBatch.ExpireDate;
                        resultItem.QuantityIn = newStockBatch.QuantityIn;
                        resultItem.QuantityOut = newStockBatch.QuantityOut;
                        resultItem.Status = newStockBatch.Status;
                        resultItem.IsActive = newStockBatch.IsActive ?? false;
                        resultItem.Note = newStockBatch.Note;
                        list.Add(resultItem);
                        batchCounter++;
                    }
                }

                return Ok(ApiResponse<List<StockOutputVM>>.Ok(list));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lô nhập kho mới");
                return BadRequest(ApiResponse<StockBatchDto>.Fail("Có lỗi xảy ra khi tạo lô nhập kho.", 400));
            }
        }

        
    }
}
