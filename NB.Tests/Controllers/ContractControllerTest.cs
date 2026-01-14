using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.ContractService;
using NB.Service.ContractService.ViewModels;
using NB.Service.Dto;
using NB.Service.ContractService.Dto;
using NB.Service.RoleService;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.UserRoleService;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using Xunit;

namespace NB.Test.Controllers
{
    public class ContractControllerTest
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IUserRoleService> _userRoleServiceMock;
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly Mock<ISupplierService> _supplierServiceMock;
        private readonly Mock<IContractService> _contractServiceMock;
        private readonly Mock<ILogger<ContractController>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
        private readonly ContractController _controller;

        public ContractControllerTest()
        {
            _userServiceMock = new Mock<IUserService>();
            _userRoleServiceMock = new Mock<IUserRoleService>();
            _roleServiceMock = new Mock<IRoleService>();
            _supplierServiceMock = new Mock<ISupplierService>();
            _contractServiceMock = new Mock<IContractService>();
            _loggerMock = new Mock<ILogger<ContractController>>();
            _mapperMock = new Mock<IMapper>();
            _cloudinaryServiceMock = new Mock<ICloudinaryService>();

            _controller = new ContractController(
                _userServiceMock.Object,
                _userRoleServiceMock.Object,
                _roleServiceMock.Object,
                _supplierServiceMock.Object,
                _contractServiceMock.Object,
                _loggerMock.Object,
                _mapperMock.Object,
                _cloudinaryServiceMock.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID23: GetData khi ModelState invalid
        ///
        /// PRECONDITION:
        /// - Controller.ModelState invalid
        ///
        /// INPUT:
        /// - Search hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Dữ liệu không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID23_GetData_InvalidModelState_ReturnsBadRequest()
        {
            var search = BuildSearch();
            _controller.ModelState.AddModelError("Test", "invalid");

            var result = await _controller.GetData(search);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID24: GetData với CustomerId không hợp lệ
        ///
        /// PRECONDITION:
        /// - CustomerId <= 0
        ///
        /// INPUT:
        /// - CustomerId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Mã khách hàng không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID24_GetData_InvalidCustomerId_ReturnsBadRequest()
        {
            var search = BuildSearch(customerId: 0);

            var result = await _controller.GetData(search);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Mã khách hàng không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID25: GetData với SupplierId không hợp lệ
        ///
        /// PRECONDITION:
        /// - SupplierId <= 0
        ///
        /// INPUT:
        /// - SupplierId = 0
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Mã nhà cung cấp không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID25_GetData_InvalidSupplierId_ReturnsBadRequest()
        {
            var search = BuildSearch(supplierId: 0);

            var result = await _controller.GetData(search);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Mã nhà cung cấp không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID26: GetData với FromDate ở tương lai
        ///
        /// PRECONDITION:
        /// - FromDate > Now
        ///
        /// INPUT:
        /// - FromDate = DateTime.Now.AddDays(1)
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Ngày tạo phải là ngày quá khứ", 400)
        /// </summary>
        [Fact]
        public async Task TCID26_GetData_FromDateInFuture_ReturnsBadRequest()
        {
            var search = BuildSearch(fromDate: DateTime.Now.AddDays(1));

            var result = await _controller.GetData(search);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Ngày tạo phải là ngày quá khứ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID27: GetData khi service ném exception
        ///
        /// PRECONDITION:
        /// - _contractService.GetData throw
        ///
        /// INPUT:
        /// - Search hợp lệ
        ///
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Có lỗi xảy ra khi lấy hợp đồng", 400)
        /// </summary>
        [Fact]
        public async Task TCID27_GetData_ServiceThrows_ReturnsBadRequest()
        {
            var search = BuildSearch();
            _contractServiceMock
                .Setup(s => s.GetData(It.IsAny<ContractSearch>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await _controller.GetData(search);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi lấy hợp đồng", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID28: GetData thành công trả về danh sách hợp đồng
        ///
        /// PRECONDITION:
        /// - _contractService.GetData trả về dữ liệu
        /// - User/Supplier tồn tại tương ứng
        ///
        /// INPUT:
        /// - Search mặc định
        ///
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok(PagedList&lt;ContractOutputVM&gt;)
        /// - Tổng số bản ghi đúng với danh sách trả về
        /// </summary>
        [Fact]
        public async Task TCID28_GetData_Succeeds_ReturnsPagedContracts()
        {
            var search = BuildSearch();
            var now = DateTime.UtcNow;
            var contractList = new List<ContractDto?>
            {
                new ContractDto
                {
                    ContractId = 1,
                    UserId = 1,
                    Image = "img-user",
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now.AddMinutes(1)
                },
                new ContractDto
                {
                    ContractId = 2,
                    SupplierId = 2,
                    Image = "img-supplier",
                    IsActive = false,
                    CreatedAt = now.AddHours(-1),
                    UpdatedAt = now
                }
            };

            _contractServiceMock
                .Setup(s => s.GetData(It.IsAny<ContractSearch>()))
                .ReturnsAsync(contractList);

            var customer = new UserDto { UserId = 1, FullName = "Khách A" };
            _userServiceMock
                .Setup(s => s.GetByUserId(1))
                .ReturnsAsync(customer);

            var supplier = new SupplierDto { SupplierId = 2, SupplierName = "Nhà cung cấp B" };
            _supplierServiceMock
                .Setup(s => s.GetBySupplierId(2))
                .ReturnsAsync(supplier);

            var result = await _controller.GetData(search);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PagedList<ContractOutputVM>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(2, response.Data.Items.Count);
            Assert.Equal(2, response.Data.TotalCount);
            Assert.Equal("Khách A", response.Data.Items[0].CustomerName);
            Assert.Equal("Nhà cung cấp B", response.Data.Items[1].SupplierName);
            _userServiceMock.Verify(s => s.GetByUserId(1), Times.Once);
            _supplierServiceMock.Verify(s => s.GetBySupplierId(2), Times.Once);
        }

        #endregion
        #region CreateContract Tests

        /// <summary>
        /// TCID01: CreateContract khi ModelState không hợp lệ
        /// 
        /// PRECONDITION:
        /// - Controller.ModelState invalid
        /// 
        /// INPUT:
        /// - Request có UserId hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Dữ liệu không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID01_CreateContract_InvalidModelState_ReturnsBadRequest()
        {
            var request = BuildRequest(userId: 1);
            _controller.ModelState.AddModelError("Test", "invalid");

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID02: CreateContract khi không cung cấp bên nào
        /// 
        /// PRECONDITION:
        /// - UserId và SupplierId đều null
        /// 
        /// INPUT:
        /// - Request không có UserId, SupplierId
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Hợp đồng phải có ít nhất một bên", 400)
        /// </summary>
        [Fact]
        public async Task TCID02_CreateContract_NoParties_ReturnsBadRequest()
        {
            var request = BuildRequest();

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Hợp đồng phải có ít nhất một bên", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID03: CreateContract khi có cả khách hàng và nhà cung cấp
        /// 
        /// PRECONDITION:
        /// - UserId và SupplierId đều được gán
        /// 
        /// INPUT:
        /// - Request chứa cả hai Id
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Hợp đồng chỉ có thể có một bên ...", 400)
        /// </summary>
        [Fact]
        public async Task TCID03_CreateContract_BothParties_ReturnsBadRequest()
        {
            var request = BuildRequest(userId: 1, supplierId: 2);

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal(
                "Hợp đồng chỉ có thể có một bên (Khách hàng HOẶC Nhà cung cấp), không thể có cả hai",
                response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID04: CreateContract khi file ảnh có đuôi không hợp lệ
        /// 
        /// PRECONDITION:
        /// - Image có định dạng .gif (không nằm trong png/jpg/jpeg)
        /// - SupplierId hợp lệ để đi qua các kiểm tra trước đó
        /// 
        /// INPUT:
        /// - Request chỉ có SupplierId và Image .gif
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: .gif", 400)
        /// </summary>
        [Fact]
        public async Task TCID04_CreateContract_InvalidImageExtension_ReturnsBadRequest()
        {
            var fileName = "contract.gif";
            var request = BuildRequest(supplierId: 2, image: CreateFormFile(fileName));

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            Assert.False(response.Success);
            Assert.Equal($"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {extension}", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID05: CreateContract khi khách hàng không tồn tại
        /// 
        /// PRECONDITION:
        /// - UserId > 0
        /// - _userService trả về null
        /// 
        /// INPUT:
        /// - Request chỉ có UserId
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Khách hàng không tồn tại", 400)
        /// </summary>
        [Fact]
        public async Task TCID05_CreateContract_UserDoesNotExist_ReturnsBadRequest()
        {
            var request = BuildRequest(userId: 1);
            _userServiceMock
                .Setup(s => s.GetByUserId(request.UserId!.Value))
                .ReturnsAsync((UserDto?)null);

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Khách hàng không tồn tại", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID06: CreateContract khi người dùng không nằm trong vai trò khách hàng
        /// 
        /// PRECONDITION:
        /// - UserId hợp lệ
        /// - _userService trả về thực thể
        /// - _userRoleService trả về ID khác
        /// 
        /// INPUT:
        /// - Request chỉ có UserId
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Người dùng không phải khách hàng", 400)
        /// </summary>
        [Fact]
        public async Task TCID06_CreateContract_UserNotCustomer_ReturnsBadRequest()
        {
            var request = BuildRequest(userId: 5);
            var customer = new UserDto { UserId = request.UserId!.Value };
            _userServiceMock
                .Setup(s => s.GetByUserId(request.UserId!.Value))
                .ReturnsAsync(customer);

            _userRoleServiceMock
                .Setup(s => s.GetByRoleId(It.IsAny<int>()))
                .ReturnsAsync(new List<UserRole> { new() { UserId = 99, RoleId = 4 } });

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Người dùng không phải khách hàng", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID07: CreateContract khi nhà cung cấp không tồn tại
        /// 
        /// PRECONDITION:
        /// - SupplierId > 0
        /// - _supplierService trả về null
        /// 
        /// INPUT:
        /// - Request chỉ có SupplierId
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Nhà cung cấp không tồn tại", 400)
        /// </summary>
        [Fact]
        public async Task TCID07_CreateContract_SupplierDoesNotExist_ReturnsBadRequest()
        {
            var request = BuildRequest(supplierId: 2);
            _supplierServiceMock
                .Setup(s => s.GetBySupplierId(request.SupplierId!.Value))
                .ReturnsAsync((SupplierDto?)null);

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Nhà cung cấp không tồn tại", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID08: CreateContract khi upload ảnh thất bại
        /// 
        /// PRECONDITION:
        /// - SupplierId tồn tại
        /// - Image có định dạng hợp lệ
        /// - UploadImageAsync trả về null
        /// 
        /// INPUT:
        /// - Request chứa SupplierId và Image .jpg
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Không thể upload ảnh", 400)
        /// </summary>
        [Fact]
        public async Task TCID08_CreateContract_ImageUploadFails_ReturnsBadRequest()
        {
            var request = BuildRequest(supplierId: 3, image: CreateFormFile("contract.jpg"));
            var supplier = new SupplierDto { SupplierId = request.SupplierId!.Value };
            _supplierServiceMock
                .Setup(s => s.GetBySupplierId(request.SupplierId.Value))
                .ReturnsAsync(supplier);

            _cloudinaryServiceMock
                .Setup(c => c.UploadImageAsync(request.Image!, It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Không thể upload ảnh", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID09: CreateContract khi service throw exception
        /// 
        /// PRECONDITION:
        /// - SupplierId tồn tại
        /// - _contractService ném lỗi
        /// 
        /// INPUT:
        /// - Request chứa SupplierId
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Có lỗi xảy ra khi tạo hợp đồng", 400)
        /// </summary>
        [Fact]
        public async Task TCID09_CreateContract_ServiceThrows_ReturnsBadRequest()
        {
            var request = BuildRequest(supplierId: 4);
            var supplier = new SupplierDto { SupplierId = request.SupplierId!.Value };
            _supplierServiceMock
                .Setup(s => s.GetBySupplierId(request.SupplierId.Value))
                .ReturnsAsync(supplier);

            _contractServiceMock
                .Setup(c => c.CreateAsync(It.IsAny<Contract>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await _controller.CreateContract(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi tạo hợp đồng", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID10: CreateContract thành công với nhà cung cấp
        /// 
        /// PRECONDITION:
        /// - SupplierId tồn tại
        /// - UploadImageAsync không được gọi (Image null)
        /// 
        /// INPUT:
        /// - Request chỉ có SupplierId
        /// 
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse<ContractOutputVM> chứa SupplierName
        /// </summary>
        [Fact]
        public async Task TCID10_CreateContract_Succeeds_ReturnsOk()
        {
            var request = BuildRequest(supplierId: 5);
            var supplier = new SupplierDto { SupplierId = request.SupplierId!.Value, SupplierName = "Supplier Tên" };
            _supplierServiceMock
                .Setup(s => s.GetBySupplierId(request.SupplierId.Value))
                .ReturnsAsync(supplier);

            _contractServiceMock
                .Setup(c => c.CreateAsync(It.IsAny<Contract>()))
                .Callback<Contract>(c => c.ContractId = 99)
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateContract(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ContractOutputVM>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Supplier Tên", response.Data?.SupplierName);
            Assert.Equal(99, response.Data?.ContractId);
            _contractServiceMock.Verify(c => c.CreateAsync(It.IsAny<Contract>()), Times.Once);
        }

        #endregion

        #region UpdateContract Tests

        /// <summary>
        /// TCID11: UpdateContract với ModelState invalid
        /// 
        /// PRECONDITION:
        /// - ModelState invalid
        /// 
        /// INPUT:
        /// - contractId = 1
        /// - Request có IsActive false
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Dữ liệu không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID11_UpdateContract_InvalidModelState_ReturnsBadRequest()
        {
            var request = BuildUpdateRequest(isActive: false);
            _controller.ModelState.AddModelError("Test", "invalid");

            var result = await _controller.UpdateContract(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Dữ liệu không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID12: UpdateContract với contractId không hợp lệ
        /// 
        /// PRECONDITION:
        /// - contractId <= 0
        /// 
        /// INPUT:
        /// - contractId = 0
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Mã hợp đồng không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID12_UpdateContract_InvalidContractId_ReturnsBadRequest()
        {
            var request = BuildUpdateRequest(isActive: true);

            var result = await _controller.UpdateContract(0, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Mã hợp đồng không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID13: UpdateContract khi file ảnh không đúng định dạng
        /// 
        /// PRECONDITION:
        /// - contractId > 0
        /// - Image có extension .gif
        /// 
        /// INPUT:
        /// - Request có Image .gif
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: .gif", 400)
        /// </summary>
        [Fact]
        public async Task TCID13_UpdateContract_InvalidImageExtension_ReturnsBadRequest()
        {
            var request = BuildUpdateRequest(image: CreateFormFile("update.gif"));

            var result = await _controller.UpdateContract(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            var extension = Path.GetExtension("update.gif").ToLowerInvariant();
            Assert.False(response.Success);
            Assert.Equal($"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {extension}", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID14: UpdateContract khi không tìm thấy hợp đồng
        /// 
        /// PRECONDITION:
        /// - contractId > 0
        /// - _contractService trả về null
        /// 
        /// INPUT:
        /// - contractId = 10
        /// 
        /// EXPECTED OUTPUT:
        /// - NotFound ApiResponse.Fail("Hợp đồng không tồn tại", 404)
        /// </summary>
        [Fact]
        public async Task TCID14_UpdateContract_NotFound_ReturnsNotFound()
        {
            var contractId = 10;
            var request = BuildUpdateRequest(isActive: true);
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync((ContractDto?)null);

            var result = await _controller.UpdateContract(contractId, request);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Hợp đồng không tồn tại", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID15: UpdateContract khi update image thất bại
        /// 
        /// PRECONDITION:
        /// - contract tồn tại
        /// - UpdateImageAsync trả về null
        /// 
        /// INPUT:
        /// - Request có Image .jpg
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Không thể upload ảnh", 400)
        /// </summary>
        [Fact]
        public async Task TCID15_UpdateContract_ImageUpdateFails_ReturnsBadRequest()
        {
            var contractId = 20;
            var image = CreateFormFile("cover.jpg");
            var request = BuildUpdateRequest(image: image);
            var contract = new ContractDto { ContractId = contractId, Image = "old.jpg" };
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync(contract);

            _cloudinaryServiceMock
                .Setup(c => c.UpdateImageAsync(image, contract.Image, It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var result = await _controller.UpdateContract(contractId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Không thể upload ảnh", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID16: UpdateContract khi service ném exception
        /// 
        /// PRECONDITION:
        /// - contract tồn tại
        /// - _contractService.UpdateAsync throw
        /// 
        /// INPUT:
        /// - Request không có ảnh
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Có lỗi xảy ra khi cập nhật hợp đồng", 400)
        /// </summary>
        [Fact]
        public async Task TCID16_UpdateContract_ServiceThrows_ReturnsBadRequest()
        {
            var contractId = 30;
            var request = BuildUpdateRequest(isActive: false);
            var contract = new ContractDto { ContractId = contractId };
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync(contract);

            _contractServiceMock
                .Setup(s => s.UpdateAsync(contract))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await _controller.UpdateContract(contractId, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi cập nhật hợp đồng", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID17: UpdateContract thành công khi chỉ thay đổi trạng thái
        /// 
        /// PRECONDITION:
        /// - contract tồn tại
        /// - Request không có Image
        /// 
        /// INPUT:
        /// - Request IsActive = false
        /// 
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok("Cập nhật hợp đồng thành công")
        /// - Contract.IsActive cập nhật
        /// </summary>
        [Fact]
        public async Task TCID17_UpdateContract_TogglesIsActive_ReturnsOk()
        {
            var contractId = 40;
            var request = BuildUpdateRequest(isActive: false);
            var contract = new ContractDto { ContractId = contractId, IsActive = true };
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync(contract);

            _contractServiceMock
                .Setup(s => s.UpdateAsync(contract))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateContract(contractId, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Cập nhật hợp đồng thành công", response.Data);
            Assert.False(contract.IsActive);
        }

        /// <summary>
        /// TCID18: UpdateContract thành công khi upload ảnh mới
        /// 
        /// PRECONDITION:
        /// - contract tồn tại có ảnh cũ
        /// - UpdateImageAsync trả về url mới
        /// 
        /// INPUT:
        /// - Request có Image hợp lệ
        /// 
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok("Cập nhật hợp đồng thành công")
        /// - Contract.Image cập nhật
        /// </summary>
        [Fact]
        public async Task TCID18_UpdateContract_WithImage_ReturnsOk()
        {
            var contractId = 50;
            var image = CreateFormFile("update.jpg");
            var request = BuildUpdateRequest(image: image);
            var contract = new ContractDto { ContractId = contractId, Image = "old.jpg" };
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync(contract);

            _cloudinaryServiceMock
                .Setup(c => c.UpdateImageAsync(image, contract.Image, It.IsAny<string>()))
                .ReturnsAsync("new.jpg");

            _contractServiceMock
                .Setup(s => s.UpdateAsync(contract))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateContract(contractId, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Cập nhật hợp đồng thành công", response.Data);
            Assert.Equal("new.jpg", contract.Image);
            _cloudinaryServiceMock.Verify(c => c.UpdateImageAsync(image, "old.jpg", It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region DeleteContract Tests

        /// <summary>
        /// TCID19: DeleteContract với contractId <= 0
        /// 
        /// PRECONDITION:
        /// - contractId invalid
        /// 
        /// INPUT:
        /// - contractId = 0
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Mã hợp đồng không hợp lệ", 400)
        /// </summary>
        [Fact]
        public async Task TCID19_DeleteContract_InvalidId_ReturnsBadRequest()
        {
            var result = await _controller.DeleteContract(0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Mã hợp đồng không hợp lệ", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID20: DeleteContract khi hợp đồng không tồn tại
        /// 
        /// PRECONDITION:
        /// - contractId > 0
        /// - _contractService.GetByContractId trả về null
        /// 
        /// INPUT:
        /// - contractId = 11
        /// 
        /// EXPECTED OUTPUT:
        /// - NotFound ApiResponse.Fail("Hợp đồng không tồn tại", 404)
        /// </summary>
        [Fact]
        public async Task TCID20_DeleteContract_NotFound_ReturnsNotFound()
        {
            var contractId = 11;
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync((ContractDto?)null);

            var result = await _controller.DeleteContract(contractId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFound.Value);

            Assert.False(response.Success);
            Assert.Equal("Hợp đồng không tồn tại", response.Error?.Message);
            Assert.Equal(404, response.StatusCode);
        }

        /// <summary>
        /// TCID21: DeleteContract khi service cập nhật tung exception
        /// 
        /// PRECONDITION:
        /// - contract tồn tại
        /// - _contractService.UpdateAsync throw
        /// 
        /// INPUT:
        /// - contractId = 21
        /// 
        /// EXPECTED OUTPUT:
        /// - BadRequest ApiResponse.Fail("Có lỗi xảy ra khi xóa hợp đồng", 400)
        /// </summary>
        [Fact]
        public async Task TCID21_DeleteContract_ServiceThrows_ReturnsBadRequest()
        {
            var contractId = 21;
            var contract = new ContractDto { ContractId = contractId, IsActive = true };
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync(contract);

            _contractServiceMock
                .Setup(s => s.UpdateAsync(contract))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await _controller.DeleteContract(contractId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal("Có lỗi xảy ra khi xóa hợp đồng", response.Error?.Message);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// TCID22: DeleteContract thành công
        /// 
        /// PRECONDITION:
        /// - contract tồn tại
        /// 
        /// INPUT:
        /// - contractId = 22
        /// 
        /// EXPECTED OUTPUT:
        /// - Ok ApiResponse.Ok("Xóa hợp đồng thành công")
        /// - Contract.IsActive = false và UpdateAsync được gọi
        /// </summary>
        [Fact]
        public async Task TCID22_DeleteContract_Succeeds_ReturnsOk()
        {
            var contractId = 22;
            var contract = new ContractDto { ContractId = contractId, IsActive = true };
            _contractServiceMock
                .Setup(s => s.GetByContractId(contractId))
                .ReturnsAsync(contract);

            _contractServiceMock
                .Setup(s => s.UpdateAsync(contract))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteContract(contractId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("Xóa hợp đồng thành công", response.Data);
            Assert.False(contract.IsActive);
            _contractServiceMock.Verify(s => s.UpdateAsync(contract), Times.Once);
        }

        #endregion

        private static CreateContractVM BuildRequest(int? userId = null, int? supplierId = null, IFormFile? image = null) =>
            new()
            {
                UserId = userId,
                SupplierId = supplierId,
                Image = image
            };

        private static UpdateContractVM BuildUpdateRequest(IFormFile? image = null, bool? isActive = null) =>
            new()
            {
                Image = image,
                IsActive = isActive
            };

        private static ContractSearch BuildSearch(
            int? customerId = null,
            int? supplierId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 20) =>
            new()
            {
                CustomerId = customerId,
                SupplierId = supplierId,
                FromDate = fromDate,
                ToDate = toDate,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

        private static IFormFile CreateFormFile(string fileName)
        {
            var bytes = Encoding.UTF8.GetBytes("dummy");
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, stream.Length, "image", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
        }
    }
}
