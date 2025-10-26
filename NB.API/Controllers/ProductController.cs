using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ProductService.ViewModels;
using NB.Service.SupplierService;

namespace NB.API.Controllers
{
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ISupplierService _supplierService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IInventoryService inventoryService,
            ISupplierService supplierService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _supplierService = supplierService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] InventorySearch search)
        {
            try
            {
                var inventoryList = await _inventoryService.GetData();
                var products = await _productService.GetByInventory(inventoryList);

           
                var filteredProducts = string.IsNullOrEmpty(search.ProductName)
                    ? products
                    : products
                        .Where(p => p.ProductName != null &&
                                   p.ProductName.Contains(search.ProductName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

               
                var pagedResult = PagedList<ProductDto>.CreateFromList(filteredProducts, search);

                return Ok(ApiResponse<PagedList<ProductDto>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm."));
            }
        }

        [HttpGet("GetProductsByWarehouse/{warehouseId}")]
        public async Task<IActionResult> GetDataByWarehouse(int warehouseId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                // Lấy tất cả Inventory thuộc về WarehouseId
                var inventories = await _inventoryService.GetByWarehouseId(warehouseId);

                if (!inventories.Any())
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm nào trong kho ID: {warehouseId}", 404));
                }

                // Tạo list các inventory có Product khác null
                var productList = await _inventoryService.GetFromList(inventories);

                //Trả về Danh sách DTO
                return Ok(ApiResponse<List<ProductInWarehouseDto>>.Ok(productList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm cho Kho {WarehouseId}", warehouseId);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy danh sách sản phẩm."));
            }
        }

        [HttpPost("CreateProduct")]
        public async Task<IActionResult> Create(int warehouseId, [FromBody] ProductCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {

                // Tạo Product
                var newProductEntity = new ProductDto
                {
                    SupplierId = model.SupplierId,
                    CategoryId = model.CategoryId,
                    Code = model.Code,
                    ImageUrl = model.ImageUrl,
                    ProductName = model.ProductName,
                    Description = model.Description,
                    WeightPerUnit = model.Weight,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _productService.CreateAsync(newProductEntity);

                // Tạo Inventory
                var newInventoryEntity = new InventoryDto
                {
                    WarehouseId = warehouseId,
                    ProductId = newProductEntity.ProductId,
                    Quantity = model.Quantity,
                    LastUpdated = DateTime.UtcNow
                };
                await _inventoryService.CreateAsync(newInventoryEntity);

                var productOutputDto = new ProductOutputVM
                {
                    ProductId = newProductEntity.ProductId,
                    ProductName = newProductEntity.ProductName,
                    Code = newProductEntity.Code,
                    Description = newProductEntity.Description,
                    SupplierId = newProductEntity.SupplierId,
                    CategoryId = newProductEntity.CategoryId,
                    WeightPerUnit = newProductEntity.WeightPerUnit,
                    Quantity = newInventoryEntity.Quantity,
                    CreatedAt = newProductEntity.CreatedAt
                };

                return Ok(ApiResponse<ProductOutputVM>.Ok(productOutputDto)); 
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ProductOutputVM>.Fail(ex.Message));
            }
        }


        [HttpPut("UpdateProduct/")]
        public async Task<IActionResult> Update(int warehouseId, int productId, [FromBody] ProductUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                if(await _productService.GetById(productId) == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {productId} không tồn tại."));
                }

                if(await _inventoryService.GetByWarehouseId(warehouseId) == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tồn tại Warehouse {warehouseId}."));
                }
                // Kiểm tra Product có thuộc warehouse không
                bool isInWarehouse = await _inventoryService.IsProductInWarehouse(warehouseId, productId);
                if (!isInWarehouse)
                {
                    return NotFound(ApiResponse<object>.Fail($"Sản phẩm ID {productId} không thuộc Warehouse ID {warehouseId}."));
                }
                if (model.CategoryId <= 0)
                {
                    return NotFound(ApiResponse<object>.Fail($"Category ID {model.CategoryId} không hợp lệ."));
                }

                // Kiểm tra SupplierId có tồn tại không
                if (model.SupplierId > 0)
                {
                    var supplier = await _supplierService.GetBySupplierId(model.SupplierId);
                    if (supplier == null)
                    {
                        return NotFound(ApiResponse<object>.Fail($"Supplier ID {model.SupplierId} không tồn tại."));
                    }
                }

                var targetInventory = await _inventoryService.GetByWarehouseAndProductId(warehouseId, productId);
                var productEntity = await _productService.GetByIdAsync(productId);

                productEntity.Code = model.Code;
                productEntity.ProductName = model.ProductName;
                productEntity.CategoryId = model.CategoryId;
                productEntity.SupplierId = model.SupplierId;
                productEntity.ImageUrl = model.ImageUrl;
                productEntity.Description = model.Description;
                productEntity.IsAvailable = model.IsAvailable;
                productEntity.WeightPerUnit = model.WeightPerUnit;
                productEntity.UpdatedAt = DateTime.UtcNow;

                await _productService.UpdateAsync(productEntity);

                targetInventory.LastUpdated = model.UpdatedAt;
                targetInventory.Quantity = model.Quantity;

                await _inventoryService.UpdateAsync(targetInventory);

                // Chuẩn bị dữ liệu trả về
                ProductOutputVM result = new ProductOutputVM
                {
                    ProductId = productEntity.ProductId,
                    ProductName = productEntity.ProductName,
                    Code = productEntity.Code,
                    Description = productEntity.Description,
                    SupplierId = productEntity.SupplierId,
                    CategoryId = productEntity.CategoryId,
                    WeightPerUnit = productEntity.WeightPerUnit,
                    Quantity = targetInventory.Quantity,
                    CreatedAt = productEntity.CreatedAt,                  
                };

                return Ok(ApiResponse<ProductOutputVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với Id: {Id}", productId);
                return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
            }
        }

        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm", 404));
                }

                product.IsAvailable = false;
                await _productService.UpdateAsync(product);
                return Ok(ApiResponse<object>.Ok("Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm với Id: {Id}", id);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi xóa sản phẩm"));
            }
        }
    }
    /*
     * fail: NB.API.Controllers.ProductController[0]
      L?i khi c?p nh?t s?n ph?m v?i Id: 31
      Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
       ---> Microsoft.Data.SqlClient.SqlException (0x80131904): The UPDATE statement conflicted with the FOREIGN KEY constraint "FK__Product__Supplie__2645B050". The conflict occurred in database "NutriBarn", table "dbo.Supplier", column 'SupplierID'.
         at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
         at Microsoft.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
         at Microsoft.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)
         at Microsoft.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
         at Microsoft.Data.SqlClient.SqlDataReader.TryHasMoreRows(Boolean& moreRows)
         at Microsoft.Data.SqlClient.SqlDataReader.TryReadInternal(Boolean setTimeout, Boolean& more)
         at Microsoft.Data.SqlClient.SqlDataReader.ReadAsyncExecute(Task task, Object state)
         at Microsoft.Data.SqlClient.SqlDataReader.InvokeAsyncCall[T](SqlDataReaderBaseAsyncCallContext`1 context)
      --- End of stack trace from previous location ---
         at Microsoft.EntityFrameworkCore.Update.AffectedCountModificationCommandBatch.ConsumeResultSetWithRowsAffectedOnlyAsync(Int32 commandIndex, RelationalDataReader reader, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Update.AffectedCountModificationCommandBatch.ConsumeAsync(RelationalDataReader reader, CancellationToken cancellationToken)
      ClientConnectionId:0772bb15-9f15-43b4-9af6-41fa1779da2c
      Error Number:547,State:0,Class:16
         --- End of inner exception stack trace ---
         at Microsoft.EntityFrameworkCore.Update.AffectedCountModificationCommandBatch.ConsumeAsync(RelationalDataReader reader, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.SqlServer.Update.Internal.SqlServerModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.RelationalDatabase.SaveChangesAsync(IList`1 entries, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(IList`1 entriesToSave, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(StateManager stateManager, Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
         at NB.Repository.Common.Repository`1.SaveAsync() in D:\Github\SEP490\NB.Repository\Common\Repository.cs:line 60
         at NB.Service.Common.Service`1.UpdateAsync(T entity) in D:\Github\SEP490\NB.Services\Common\Service.cs:line 67
         at NB.API.Controllers.ProductController.Update(Int32 warehouseId, Int32 productId, ProductUpdateVM model) in D:\Github\SEP490\NB.API\Controllers\ProductController.cs:line 189
     */
}