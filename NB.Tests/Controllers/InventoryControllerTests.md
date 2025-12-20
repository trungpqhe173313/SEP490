# InventoryControllerTests - Unit Test Documentation

## Tổng quan
- **Controller**: `InventoryController`
- **Service Dependencies**:
  - `IInventoryService`
  - `IWarehouseService`
  - `IProductService`
- **Tổng số test cases**: 9 tests
- **Framework**: xUnit + Moq + FluentAssertions

## Error Message Pattern

**InventoryController sử dụng fixed error messages:**

### GetInventoryData
- **Success**: Returns `PagedList<ProductInventoryDto>`
- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi lấy dữ liệu tồn kho: {ex.Message}",
  "data": null,
  "statusCode": 400
}
```

### GetInventoryQuantity
- **Warehouse Not Found**:
```json
{
  "success": false,
  "message": "Kho không tồn tại",
  "data": 0,
  "statusCode": 404
}
```

- **Product Not Found**:
```json
{
  "success": false,
  "message": "Sản phẩm không tồn tại",
  "data": 0,
  "statusCode": 404
}
```

- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi lấy số lượng tồn kho: {ex.Message}",
  "data": 0,
  "statusCode": 400
}
```

---

## Test Cases

### 1. GetInventoryData - `POST /api/Inventory/GetInventoryData` (3 tests)

**Mục đích**: Lấy danh sách sản phẩm và số lượng tồn kho (có phân trang)

#### 1.1. GetInventoryData_WithValidSearch_ReturnsOkWithPagedList ✅
**Mô tả**: Lấy danh sách inventory với search hợp lệ (có WarehouseId)

**Input**:
```csharp
InventorySearch {
    PageNumber = 1,
    PageSize = 10,
    WarehouseId = 1,
    ProductName = "Product 1"
}
```

**Mock Setup**:
- `_mockInventoryService.GetProductInventoryListAsync()` returns PagedList with 1 item

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data` contains PagedList with 1 ProductInventoryDto
- ProductName = "Product 1"
- TotalQuantity = 100
- WarehouseId = 1

---

#### 1.2. GetInventoryData_WithoutWarehouseId_ReturnsOkWithTotalInventory ✅
**Mô tả**: Lấy danh sách inventory không có WarehouseId (hiển thị tổng tất cả kho)

**Input**:
```csharp
InventorySearch {
    PageNumber = 1,
    PageSize = 10,
    WarehouseId = null
}
```

**Mock Setup**:
- `_mockInventoryService.GetProductInventoryListAsync()` returns PagedList with total inventory

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- TotalQuantity = 300 (tổng từ tất cả kho)
- WarehouseId = null
- WarehouseName = null

---

#### 1.3. GetInventoryData_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Service throw exception khi lấy dữ liệu

**Input**:
```csharp
InventorySearch {
    PageNumber = 1,
    PageSize = 10
}
```

**Mock Setup**:
- `_mockInventoryService.GetProductInventoryListAsync()` throws Exception("Database error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message` = "Có lỗi xảy ra khi lấy dữ liệu tồn kho: Database error"

---

### 2. GetInventoryQuantity - `POST /api/Inventory/quantityProduct` (6 tests)

**Mục đích**: Lấy số lượng sản phẩm tồn kho của sản phẩm trong kho cụ thể

#### 2.1. GetInventoryQuantity_WithValidData_ReturnsOkWithQuantity ✅
**Mô tả**: Lấy số lượng tồn kho với dữ liệu hợp lệ

**Input**:
```csharp
ProductInventorySearch {
    warehouseId = 1,
    productId = 1
}
```

**Mock Setup**:
- `_mockWarehouseService.GetByIdAsync(1)` returns Warehouse
- `_mockProductService.GetByIdAsync(1)` returns Product
- `_mockInventoryService.GetInventoryQuantity(1, 1)` returns 150

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data = 150`

---

#### 2.2. GetInventoryQuantity_WithNonExistentWarehouse_ReturnsNotFound ❌
**Mô tả**: Kho không tồn tại

**Input**:
```csharp
ProductInventorySearch {
    warehouseId = 999,
    productId = 1
}
```

**Mock Setup**:
- `_mockWarehouseService.GetByIdAsync(999)` returns null

**Expected Output**:
- HTTP 404 Not Found
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Kho không tồn tại"`

**Error Response**:
```json
{
  "success": false,
  "message": "Kho không tồn tại",
  "data": 0,
  "statusCode": 404
}
```

---

#### 2.3. GetInventoryQuantity_WithNonExistentProduct_ReturnsNotFound ❌
**Mô tả**: Sản phẩm không tồn tại

**Input**:
```csharp
ProductInventorySearch {
    warehouseId = 1,
    productId = 999
}
```

**Mock Setup**:
- `_mockWarehouseService.GetByIdAsync(1)` returns Warehouse
- `_mockProductService.GetByIdAsync(999)` returns null

**Expected Output**:
- HTTP 404 Not Found
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Sản phẩm không tồn tại"`

**Error Response**:
```json
{
  "success": false,
  "message": "Sản phẩm không tồn tại",
  "data": 0,
  "statusCode": 404
}
```

---

#### 2.4. GetInventoryQuantity_WithZeroQuantity_ReturnsOkWithZero ✅
**Mô tả**: Sản phẩm có số lượng = 0 trong kho

**Input**:
```csharp
ProductInventorySearch {
    warehouseId = 1,
    productId = 1
}
```

**Mock Setup**:
- `_mockWarehouseService.GetByIdAsync(1)` returns Warehouse
- `_mockProductService.GetByIdAsync(1)` returns Product
- `_mockInventoryService.GetInventoryQuantity(1, 1)` returns 0

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data = 0`

---

#### 2.5. GetInventoryQuantity_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Service throw exception khi lấy số lượng

**Input**:
```csharp
ProductInventorySearch {
    warehouseId = 1,
    productId = 1
}
```

**Mock Setup**:
- `_mockWarehouseService.GetByIdAsync(1)` returns Warehouse
- `_mockProductService.GetByIdAsync(1)` returns Product
- `_mockInventoryService.GetInventoryQuantity(1, 1)` throws Exception("Database error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message` = "Có lỗi xảy ra khi lấy số lượng tồn kho: Database error"

**Error Response**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi lấy số lượng tồn kho: Database error",
  "data": 0,
  "statusCode": 400
}
```

---

## Tổng kết Test Coverage

| Method | Tổng số tests | Success cases | Error cases |
|--------|---------------|---------------|-------------|
| GetInventoryData | 3 | 2 | 1 |
| GetInventoryQuantity | 6 | 2 | 4 |
| **Tổng** | **9** | **4** | **5** |

## Các điểm chú ý

1. **GetInventoryData**:
   - Có 2 chế độ: với WarehouseId (hiển thị số lượng trong kho cụ thể) và không có WarehouseId (hiển thị tổng tất cả kho)
   - Sử dụng PagedList để phân trang

2. **GetInventoryQuantity**:
   - Kiểm tra warehouse tồn tại trước
   - Kiểm tra product tồn tại sau
   - Có thể trả về 0 nếu sản phẩm không có trong kho

3. **Error Handling**:
   - Service exception được catch và trả về BadRequest với message rõ ràng
   - Not Found cases trả về 404 với message cụ thể

4. **Mock Pattern**:
   - Mock 3 services: IInventoryService, IWarehouseService, IProductService
   - Setup mock theo thứ tự validation logic trong controller
