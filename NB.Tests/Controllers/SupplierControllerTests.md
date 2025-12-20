# SupplierControllerTests - Unit Test Documentation

## Tổng quan
- **Controller**: `SupplierController`
- **Service Dependencies**:
  - `ISupplierService`
  - `IMapper`
  - `ILogger<Supplier>`
- **Tổng số test cases**: 21 tests
- **Framework**: xUnit + Moq + FluentAssertions

## Error Message Pattern

**SupplierController sử dụng fixed error messages:**

### GetData
- **Success**: Returns `PagedList<SupplierDto>`
- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi lấy dữ liệu",
  "data": null,
  "statusCode": 400
}
```

### GetBySupplierId
- **Not Found**:
```json
{
  "success": false,
  "message": "Không tìm thấy nhà cung cấp",
  "data": null,
  "statusCode": 404
}
```

- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra",
  "data": null,
  "statusCode": 400
}
```

### CreateSupplier
- **Invalid ModelState**:
```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "statusCode": 400
}
```

- **Email Exists**:
```json
{
  "success": false,
  "message": "Email nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

- **Phone Exists**:
```json
{
  "success": false,
  "message": "Số điện thoại nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi tạo nhà cung cấp",
  "data": null,
  "statusCode": 400
}
```

### UpdateSupplier
- **Invalid ModelState**:
```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "statusCode": 400
}
```

- **Supplier Not Found**:
```json
{
  "success": false,
  "message": "Không tìm thấy nhà cung cấp",
  "data": null,
  "statusCode": 404
}
```

- **Email Exists**:
```json
{
  "success": false,
  "message": "Email nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

- **Phone Exists**:
```json
{
  "success": false,
  "message": "Số điện thoại nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi cập nhật nhà cung cấp",
  "data": null,
  "statusCode": 400
}
```

### DeleteSupplier
- **Not Found**:
```json
{
  "success": false,
  "message": "Không tìm thấy nhà cung cấp",
  "data": null,
  "statusCode": 404
}
```

- **Service Exception**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi xóa nhà cung cấp",
  "data": null,
  "statusCode": 400
}
```

---

## Test Cases

### 1. GetData - `POST /api/suppliers/GetData` (3 tests)

**Mục đích**: Lấy danh sách nhà cung cấp (có phân trang)

#### 1.1. GetData_WithValidSearch_ReturnsOkWithPagedList ✅
**Mô tả**: Lấy danh sách supplier với search hợp lệ

**Input**:
```csharp
SupplierSearch {
    PageNumber = 1,
    PageSize = 10,
    SupplierName = "Supplier 1",
    IsActive = true
}
```

**Mock Setup**:
- `_mockSupplierService.GetData()` returns PagedList with 1 item

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data` contains PagedList with 1 SupplierDto
- SupplierName = "Supplier 1"
- Email = "supplier1@example.com"
- Phone = "0123456789"

---

#### 1.2. GetData_WithEmptyResult_ReturnsOkWithEmptyList ✅
**Mô tả**: Tìm kiếm không có kết quả

**Input**:
```csharp
SupplierSearch {
    PageNumber = 1,
    PageSize = 10,
    SupplierName = "NonExistent"
}
```

**Mock Setup**:
- `_mockSupplierService.GetData()` returns empty PagedList

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data.Items` is empty

---

#### 1.3. GetData_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Service throw exception khi lấy dữ liệu

**Input**:
```csharp
SupplierSearch {
    PageNumber = 1,
    PageSize = 10
}
```

**Mock Setup**:
- `_mockSupplierService.GetData()` throws Exception("Database error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Có lỗi xảy ra khi lấy dữ liệu"`

**Error Response**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi lấy dữ liệu",
  "data": null,
  "statusCode": 400
}
```

---

### 2. GetBySupplierId - `GET /api/suppliers/GetBySupplierId/{id}` (3 tests)

**Mục đích**: Lấy thông tin chi tiết nhà cung cấp theo ID

#### 2.1. GetBySupplierId_WithValidId_ReturnsOkWithSupplier ✅
**Mô tả**: Lấy supplier với ID hợp lệ

**Input**:
```csharp
supplierId = 1
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` returns SupplierDto

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data` contains SupplierDto
- SupplierName = "Supplier 1"

---

#### 2.2. GetBySupplierId_WithNonExistentId_ReturnsNotFound ❌
**Mô tả**: Supplier không tồn tại

**Input**:
```csharp
supplierId = 999
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(999)` returns null

**Expected Output**:
- HTTP 404 Not Found
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Không tìm thấy nhà cung cấp"`
- `ApiResponse.StatusCode = 404`

**Error Response**:
```json
{
  "success": false,
  "message": "Không tìm thấy nhà cung cấp",
  "data": null,
  "statusCode": 404
}
```

---

#### 2.3. GetBySupplierId_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Service throw exception

**Input**:
```csharp
supplierId = 1
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` throws Exception("Database error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Có lỗi xảy ra"`

**Error Response**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra",
  "data": null,
  "statusCode": 400
}
```

---

### 3. CreateSupplier - `POST /api/suppliers/CreateSupplier` (6 tests)

**Mục đích**: Tạo nhà cung cấp mới

#### 3.1. CreateSupplier_WithValidData_ReturnsOkWithCreatedSupplier ✅
**Mô tả**: Tạo supplier thành công với dữ liệu hợp lệ

**Input**:
```csharp
SupplierCreateVM {
    SupplierName = "Supplier 1",
    Email = "supplier1@example.com",
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetByEmail()` returns null
- `_mockSupplierService.GetByPhone()` returns null
- `_mockMapper.Map()` returns Supplier
- `_mockSupplierService.CreateAsync()` succeeds

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data.SupplierName = "Supplier 1"`
- `ApiResponse.Data.IsActive = true`
- CreatedAt is set

---

#### 3.2. CreateSupplier_WithInvalidModelState_ReturnsBadRequest ❌
**Mô tả**: ModelState không hợp lệ (email sai format)

**Input**:
```csharp
SupplierCreateVM {
    SupplierName = "Supplier 1",
    Email = "invalid-email",
    Phone = "0123456789"
}
ModelState.AddModelError("Email", "Email nhà cung cấp không hợp lệ")
```

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Dữ liệu không hợp lệ"`

**Error Response**:
```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "statusCode": 400
}
```

---

#### 3.3. CreateSupplier_WithExistingEmail_ReturnsBadRequest ❌
**Mô tả**: Email đã tồn tại trong hệ thống

**Input**:
```csharp
SupplierCreateVM {
    SupplierName = "Supplier 1",
    Email = "existing@example.com",
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetByEmail("existing@example.com")` returns existing Supplier

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Email nhà cung cấp đã tồn tại"`

**Error Response**:
```json
{
  "success": false,
  "message": "Email nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

---

#### 3.4. CreateSupplier_WithExistingPhone_ReturnsBadRequest ❌
**Mô tả**: Số điện thoại đã tồn tại trong hệ thống

**Input**:
```csharp
SupplierCreateVM {
    SupplierName = "Supplier 1",
    Email = "supplier1@example.com",
    Phone = "0987654321"
}
```

**Mock Setup**:
- `_mockSupplierService.GetByEmail()` returns null
- `_mockSupplierService.GetByPhone("0987654321")` returns existing Supplier

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Số điện thoại nhà cung cấp đã tồn tại"`

**Error Response**:
```json
{
  "success": false,
  "message": "Số điện thoại nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

---

#### 3.5. CreateSupplier_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Exception xảy ra trong quá trình tạo

**Input**:
```csharp
SupplierCreateVM {
    SupplierName = "Supplier 1",
    Email = "supplier1@example.com",
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetByEmail()` returns null
- `_mockSupplierService.GetByPhone()` returns null
- `_mockMapper.Map()` throws Exception("Mapping error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Có lỗi xảy ra khi tạo nhà cung cấp"`

**Error Response**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi tạo nhà cung cấp",
  "data": null,
  "statusCode": 400
}
```

---

### 4. UpdateSupplier - `PUT /api/suppliers/UpdateSupplier/{id}` (8 tests)

**Mục đích**: Cập nhật thông tin nhà cung cấp

#### 4.1. UpdateSupplier_WithValidData_ReturnsOkWithUpdatedSupplier ✅
**Mô tả**: Cập nhật supplier thành công

**Input**:
```csharp
supplierId = 1
SupplierEditVM {
    SupplierName = "Supplier 1 Updated",
    Email = "supplier1@example.com",
    Phone = "0123456789",
    IsActive = true
}
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` returns existing SupplierDto
- `_mockMapper.Map()` returns updated Supplier
- `_mockSupplierService.UpdateAsync()` succeeds

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data.SupplierName = "Supplier 1 Updated"`

---

#### 4.2. UpdateSupplier_WithInvalidModelState_ReturnsBadRequest ❌
**Mô tả**: ModelState không hợp lệ

**Input**:
```csharp
supplierId = 1
SupplierEditVM {
    SupplierName = "Supplier 1",
    Email = "invalid-email",
    Phone = "0123456789"
}
ModelState.AddModelError("Email", "Email nhà cung cấp không hợp lệ")
```

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Dữ liệu không hợp lệ"`

**Error Response**:
```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "statusCode": 400
}
```

---

#### 4.3. UpdateSupplier_WithNonExistentSupplier_ReturnsNotFound ❌
**Mô tả**: Supplier không tồn tại

**Input**:
```csharp
supplierId = 999
SupplierEditVM {
    SupplierName = "Supplier 1",
    Email = "supplier1@example.com",
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(999)` returns null

**Expected Output**:
- HTTP 404 Not Found
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Không tìm thấy nhà cung cấp"`
- `ApiResponse.StatusCode = 404`

**Error Response**:
```json
{
  "success": false,
  "message": "Không tìm thấy nhà cung cấp",
  "data": null,
  "statusCode": 404
}
```

---

#### 4.4. UpdateSupplier_WithNewExistingEmail_ReturnsBadRequest ❌
**Mô tả**: Email mới đã được sử dụng bởi supplier khác

**Input**:
```csharp
supplierId = 1
SupplierEditVM {
    SupplierName = "Supplier 1",
    Email = "existing@example.com",
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` returns Supplier with email "supplier1@example.com"
- `_mockSupplierService.GetByEmail("existing@example.com")` returns another Supplier (SupplierId = 2)

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Email nhà cung cấp đã tồn tại"`

**Error Response**:
```json
{
  "success": false,
  "message": "Email nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

---

#### 4.5. UpdateSupplier_WithNewExistingPhone_ReturnsBadRequest ❌
**Mô tả**: Số điện thoại mới đã được sử dụng bởi supplier khác

**Input**:
```csharp
supplierId = 1
SupplierEditVM {
    SupplierName = "Supplier 1",
    Email = "supplier1@example.com",
    Phone = "0987654321"
}
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` returns Supplier with phone "0123456789"
- `_mockSupplierService.GetByPhone("0987654321")` returns another Supplier (SupplierId = 2)

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Số điện thoại nhà cung cấp đã tồn tại"`

**Error Response**:
```json
{
  "success": false,
  "message": "Số điện thoại nhà cung cấp đã tồn tại",
  "data": null,
  "statusCode": 400
}
```

---

#### 4.6. UpdateSupplier_WithSameEmail_ReturnsOk ✅
**Mô tả**: Giữ nguyên email cũ (không check trùng)

**Input**:
```csharp
supplierId = 1
SupplierEditVM {
    SupplierName = "Supplier 1 Updated",
    Email = "supplier1@example.com", // Same email
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` returns Supplier with email "supplier1@example.com"
- Email check is skipped (same email)
- `_mockSupplierService.UpdateAsync()` succeeds

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- Update succeeds without email validation

---

#### 4.7. UpdateSupplier_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Exception xảy ra trong quá trình cập nhật

**Input**:
```csharp
supplierId = 1
SupplierEditVM {
    SupplierName = "Supplier 1",
    Email = "supplier1@example.com",
    Phone = "0123456789"
}
```

**Mock Setup**:
- `_mockSupplierService.GetBySupplierId(1)` throws Exception("Database error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Có lỗi xảy ra khi cập nhật nhà cung cấp"`

**Error Response**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi cập nhật nhà cung cấp",
  "data": null,
  "statusCode": 400
}
```

---

### 5. DeleteSupplier - `DELETE /api/suppliers/DeleteSupplier/{id}` (3 tests)

**Mục đích**: Xóa nhà cung cấp (soft delete - set IsActive = false)

#### 5.1. DeleteSupplier_WithValidId_ReturnsOkWithTrue ✅
**Mô tả**: Xóa supplier thành công

**Input**:
```csharp
supplierId = 1
```

**Mock Setup**:
- `_mockSupplierService.GetByIdAsync(1)` returns Supplier with IsActive = true
- `_mockSupplierService.UpdateAsync()` succeeds

**Expected Output**:
- HTTP 200 OK
- `ApiResponse.Success = true`
- `ApiResponse.Data = true`
- Supplier.IsActive is set to false

**Verification**:
- Verify `UpdateAsync` was called with Supplier where IsActive = false

---

#### 5.2. DeleteSupplier_WithNonExistentId_ReturnsNotFound ❌
**Mô tả**: Supplier không tồn tại

**Input**:
```csharp
supplierId = 999
```

**Mock Setup**:
- `_mockSupplierService.GetByIdAsync(999)` returns null

**Expected Output**:
- HTTP 404 Not Found
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Không tìm thấy nhà cung cấp"`

**Error Response**:
```json
{
  "success": false,
  "message": "Không tìm thấy nhà cung cấp",
  "data": null,
  "statusCode": 404
}
```

---

#### 5.3. DeleteSupplier_WithServiceException_ReturnsBadRequest ❌
**Mô tả**: Exception xảy ra trong quá trình xóa

**Input**:
```csharp
supplierId = 1
```

**Mock Setup**:
- `_mockSupplierService.GetByIdAsync(1)` throws Exception("Database error")

**Expected Output**:
- HTTP 400 Bad Request
- `ApiResponse.Success = false`
- `ApiResponse.Message = "Có lỗi xảy ra khi xóa nhà cung cấp"`

**Error Response**:
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi xóa nhà cung cấp",
  "data": null,
  "statusCode": 400
}
```

---

## Tổng kết Test Coverage

| Method | Tổng số tests | Success cases | Error cases |
|--------|---------------|---------------|-------------|
| GetData | 3 | 2 | 1 |
| GetBySupplierId | 3 | 1 | 2 |
| CreateSupplier | 6 | 1 | 5 |
| UpdateSupplier | 8 | 2 | 6 |
| DeleteSupplier | 3 | 1 | 2 |
| **Tổng** | **23** | **7** | **16** |

## Các điểm chú ý

1. **Validation Logic**:
   - Email và Phone phải unique trong hệ thống
   - Khi update, nếu email/phone không thay đổi thì không check trùng
   - ModelState validation cho email format

2. **Soft Delete**:
   - DeleteSupplier không xóa hẳn record, chỉ set IsActive = false
   - Vẫn giữ lại dữ liệu trong database

3. **Error Handling**:
   - Service exception được catch và trả về BadRequest
   - Not Found cases trả về 404
   - Validation errors trả về 400 với message rõ ràng

4. **Update Logic**:
   - Kiểm tra supplier tồn tại trước
   - Kiểm tra email trùng (nếu thay đổi email)
   - Kiểm tra phone trùng (nếu thay đổi phone)
   - Sử dụng mapper để map từ ViewModel sang Entity

5. **Create Logic**:
   - Kiểm tra email trùng
   - Kiểm tra phone trùng
   - Set IsActive = true và CreatedAt = DateTime.Now tự động
   - Sử dụng mapper để map từ ViewModel sang Entity

6. **Mock Pattern**:
   - Mock ISupplierService cho business logic
   - Mock IMapper cho object mapping
   - Mock ILogger<Supplier> cho logging
