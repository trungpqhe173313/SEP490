using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.SupplierService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;

namespace NB.API.Controllers
{
    [Route("api/stockoutput")]
    public class StockOutputController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IStockBatchService _stockBatchService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IMapper _mapper;
        private readonly string transactionType = "Export";
        public StockOutputController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IStockBatchService stockBatchService,
            IUserService userService,
            IInventoryService inventoryService,
            IMapper mapper,
            ILogger<EmployeeController> logger)
        {
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _productService = productService;
            _userService = userService;
            _stockBatchService = stockBatchService;
            _inventoryService = inventoryService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] TransactionSearch search)
        {
            try
            {
                search.Type = transactionType;
                var result = await _transactionService.GetData(search);
                return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("GetByTransactionId")]
        public async Task<IActionResult> GetByTransactionId(int id)
        {
            try
            {
                var result = await _transactionService.GetByTransactionId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404));
                }
                return Ok(ApiResponse<TransactionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<TransactionDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpGet("GetTransactionDetailByTransactionId")]
        public async Task<IActionResult> GetTransactionDetailByTransactionId(int id)
        {
            try
            {
                //lay don hàng
                var result = await _transactionDetailService.GetByTransactionId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404));
                }
                //lay danh sach product id
                List<int> listProductId = result.Select(td => td.ProductId).ToList();
                //lay ra các sản phẩm trong listProductId
                var listProduct = await _productService.GetByIds(listProductId);
                foreach (var t in result)
                {
                    var product = listProduct.FirstOrDefault(p => p.ProductId == t.ProductId);
                    if (product is not null)
                    {
                        t.ProductName = product.ProductName;
                        t.ImageUrl = product.ImageUrl;
                    }
                }

                return Ok(ApiResponse<List<TransactionDetailDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<TransactionDto>.Fail("Có lỗi xảy ra"));
            }
        }

        //[HttpPost("CreateOrder/{userId}")]
        //public async Task<IActionResult> CreateOrder(int userId, List<ProductOrder> listProductOrder)
        //{
        //    var exsitingUser = _userService.GetByUserId(userId);
        //    if (exsitingUser == null)
        //    {
        //        return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy khách hàng", 404));
        //    }
        //    if (!listProductOrder.Any())
        //    {
        //        return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào", 404));
        //    }
        //    //lấy ra tất cả các product có trong listProductOrder
        //    var listProduct = await _productService.GetByIds(listProductOrder.Select(po => po.ProductId).ToList());
        //    if (!listProduct.Any())
        //    {
        //        return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào", 404));
        //    }
        //    try
        //    {
        //        //danh sach cac id của product
        //        var listProductId = listProduct.Select(p => p.ProductId).ToList();
        //        //lay ra tat car cac stock batch cua cac san pham, nhung lo hang van con hang ton va van con hạn sử dụng
        //        var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);

        //        // result sẽ là danh sách chứa:
        //        // - Mỗi sản phẩm trong đơn hàng (ProductOrder)
        //        // - Danh sách các lô hàng (StockBatchDto) được chọn cùng số lượng lấy từ mỗi lô
        //        var result = new List<(ProductOrder Order, List<(StockBatchDto Batch, decimal TakeQuantity)> BatchesTaken)>();

        //        // Duyệt qua từng sản phẩm trong đơn hàng
        //        foreach (var po in listProductOrder)
        //        {
        //            // Lọc các lô hàng có cùng ProductId, còn hàng tồn (QuantityIn > QuantityOut)
        //            // và vẫn còn hạn sử dụng
        //            var batches = listStockBatch
        //                .Where(sb => sb.ProductId == po.ProductId
        //                             && sb.QuantityIn > sb.QuantityOut
        //                             && sb.ExpireDate > DateTime.Today)
        //                // Sắp xếp theo ngày nhập tăng dần (FIFO – nhập trước xuất trước)
        //                .OrderBy(sb => sb.ImportDate)
        //                .ToList();

        //            // Số lượng sản phẩm cần xuất còn lại phải lấy từ kho
        //            decimal remaining = po.Quantity ?? 0;

        //            // Danh sách tạm lưu các lô đã lấy và số lượng lấy từ mỗi lô
        //            var taken = new List<(StockBatchDto, decimal)>();

        //            // Duyệt từng lô hàng theo thứ tự nhập
        //            foreach (var batch in batches)
        //            {
        //                // Tính số lượng còn lại trong lô (đã nhập trừ đi đã xuất)
        //                decimal available = (batch.QuantityIn - batch.QuantityOut) ?? 0;

        //                // Nếu lô này hết hàng thì bỏ qua
        //                if (available <= 0) continue;

        //                // Xác định số lượng cần lấy từ lô này (lấy ít nhất giữa số còn lại và số cần)
        //                decimal take = Math.Min(available, remaining);

        //                // Thêm thông tin lô và số lượng lấy vào danh sách kết quả
        //                taken.Add((batch, take));

        //                // Giảm số lượng còn cần lấy
        //                remaining -= take;

        //                // Nếu đã đủ hàng theo yêu cầu thì dừng
        //                if (remaining <= 0)
        //                    break;
        //            }

        //            // Thêm kết quả của sản phẩm hiện tại vào danh sách tổng
        //            result.Add((po, taken));

        //            //cap nhat lại db
        //            foreach (var item in result)
        //            {
        //                foreach (var (batch, qtyTaken) in item.BatchesTaken)
        //                {
        //                    // Lấy bản ghi thật trong DB (batch hiện tại)
        //                    var entity = await _stockBatchService.GetByIdAsync(batch.BatchId);

        //                    if (entity != null)
        //                    {
        //                        // Cập nhật số lượng đã xuất
        //                        entity.QuantityOut += qtyTaken;

        //                        // Cập nhật thời gian thay đổi (nếu có cột LastUpdated)
        //                        entity.LastUpdated = DateTime.Now;
        //                        await _stockBatchService.UpdateAsync(entity);
        //                    }
        //                }
        //            }
        //            var tranCreate = new TransactionCreateVM()
        //            {
        //                CustomerId = userId,
        //                //tam thoi lay warehouseId ở stockbatch của sản phẩm
        //                WarehouseId = listStockBatch[0].WarehouseId
        //            };
        //            var transactionEntity = _mapper.Map<TransactionCreateVM, Transaction>(tranCreate);
        //            //tam thoi de trang thai la 1(dang xu ly)
        //            transactionEntity.Status = 1;
        //            transactionEntity.TransactionDate = DateTime.Now;
        //            transactionEntity.Type = "Export";


        //            await _transactionService.CreateAsync(transactionEntity);

        //            //cập nhật lại số lượng của product trong inventory
        //            var listInvenGetByProduct = await _inventoryService.GetByProductIds(listProductId);
        //            foreach (var po2 in listProductOrder)
        //            {
        //                var inventory = listInvenGetByProduct.FirstOrDefault(i => i.ProductId == po2.ProductId);
        //                if (inventory != null)
        //                {
        //                    inventory.Quantity = inventory.Quantity - po2.Quantity;
        //                    await _inventoryService.UpdateAsync(inventory);
        //                }
        //            }

        //            //tao tranDetail
        //            foreach (var po3 in listProductOrder)
        //            {
        //                var inventory = listInvenGetByProduct.FirstOrDefault(i => i.ProductId == po3.ProductId);
        //                if (inventory != null)
        //                {
        //                    var tranDetailCreate = new TransactionDetailCreateVM()
        //                    {
        //                        ProductId = po3.ProductId,
        //                        TransactionId = transactionEntity.TransactionId,
        //                        Quantity = (int)po3.Quantity,
        //                        UnitPrice = inventory?.AverageCost * po3.Quantity ?? 0
        //                    };
        //                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetailCreate);
        //                    await _transactionDetailService.CreateAsync(tranDetailEntity);
        //                }

        //            }

        //            return Ok(ApiResponse<Transaction>.Ok(transactionEntity));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi tao don hang");
        //        return BadRequest(ApiResponse<User>.Fail("Có lỗi xảy ra khi tao don hang"));
        //    }
        //}

        [HttpPost("CreateOrder/{userId}")]
        public async Task<IActionResult> CreateOrder(int userId, [FromBody] List<ProductOrder> listProductOrder)
        {
            var existingUser = await _userService.GetByUserId(userId);
            if (existingUser == null)
                return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy khách hàng", 404));

            if (listProductOrder == null || !listProductOrder.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào", 404));

            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào", 404));

            try
            {
                // 1️⃣ Lấy tất cả các lô hàng còn hàng và còn hạn
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);

                // 2️⃣ Tạo transaction (đơn hàng)
                var tranCreate = new TransactionCreateVM
                {
                    CustomerId = userId,
                    WarehouseId = listStockBatch.First().WarehouseId, // hoặc lấy theo input từ FE
                };
                var transactionEntity = _mapper.Map<TransactionCreateVM, Transaction>(tranCreate);

                transactionEntity.Status = 1; // đang xử lý
                transactionEntity.TransactionDate = DateTime.Now;
                transactionEntity.Type = "Export";
                await _transactionService.CreateAsync(transactionEntity);

                // 3️⃣ Duyệt từng sản phẩm để lấy lô & cập nhật tồn
                foreach (var po in listProductOrder)
                {
                    var batches = listStockBatch
                        .Where(sb => sb.ProductId == po.ProductId
                                     && sb.QuantityIn > sb.QuantityOut
                                     && sb.ExpireDate > DateTime.Today)
                        .OrderBy(sb => sb.ImportDate)
                        .ToList();

                    decimal remaining = po.Quantity ?? 0;
                    var taken = new List<(StockBatchDto, decimal)>();

                    foreach (var batch in batches)
                    {
                        decimal available = (batch.QuantityIn - batch.QuantityOut) ?? 0;
                        if (available <= 0) continue;

                        decimal take = Math.Min(available, remaining);
                        taken.Add((batch, take));

                        // cập nhật lại lô hàng
                        var entity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                        if (entity != null)
                        {
                            entity.QuantityOut += take;
                            entity.LastUpdated = DateTime.Now;
                            await _stockBatchService.UpdateAsync(entity);
                        }

                        remaining -= take;
                        if (remaining <= 0) break;
                    }

                    // 4️⃣ Cập nhật inventory (tồn kho)
                    var inventory = await _inventoryService.GetByProductIdRetriveOneObject(po.ProductId);
                    if (inventory != null)
                    {
                        inventory.Quantity -= po.Quantity ?? 0;
                        await _inventoryService.UpdateAsync(inventory);
                    }

                    // 5️⃣ Tạo transaction detail
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionEntity.TransactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (inventory?.AverageCost ?? 0) * (po.Quantity ?? 0)
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }

                // 6️⃣ Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<Transaction>.Ok(new Transaction
                {
                    TransactionId = transactionEntity.TransactionId,

                    CustomerId = transactionEntity.CustomerId,

                    WarehouseInId = transactionEntity.WarehouseInId,

                    SupplierId = transactionEntity.SupplierId,

                    WarehouseId = transactionEntity.WarehouseId,

                    ConversionRate = transactionEntity.ConversionRate,

                    Type = transactionEntity.Type,

                    Status = transactionEntity.Status,

                    TransactionDate = transactionEntity.TransactionDate,

                    Note = transactionEntity.Note
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi tạo đơn hàng"));
            }
        }



    }
}
