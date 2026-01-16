using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.PriceListDetailService;
using NB.Service.PriceListDetailService.Dto;
using NB.Service.PriceListDetailService.ViewModels;
using NB.Service.PriceListService;
using NB.Service.PriceListService.Dto;
using NB.Service.PriceListService.ViewModels;
using NB.Service.ProductService;
using NB.Service.UserService;

namespace NB.Tests.Controllers
{
    public class PriceListControllerTests
    {
        private readonly Mock<IPriceListService> _mockPriceListService;
        private readonly Mock<IPriceListDetailService> _mockPriceListDetailService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<PriceListController>> _mockLogger;
        private readonly PriceListController _controller;

        public PriceListControllerTests()
        {
            _mockPriceListService = new Mock<IPriceListService>();
            _mockPriceListDetailService = new Mock<IPriceListDetailService>();
            _mockUserService = new Mock<IUserService>();
            _mockProductService = new Mock<IProductService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<PriceListController>>();
            _controller = new PriceListController(
                _mockPriceListService.Object,
                _mockPriceListDetailService.Object,
                _mockUserService.Object,
                _mockProductService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        #region GetData Tests

        /// <summary>
        /// TCID01: Lay danh sach bang gia co phan trang voi tham so tim kiem hop le
        /// Input: PriceListSearch voi PageIndex 1 va PageSize 10
        /// Expected: Tra ve OkObjectResult voi danh sach bang gia co phan trang va Success true
        /// </summary>
        [Fact]
        public async Task GetData_WithValidSearch_ReturnsOkWithPagedList()
        {
            // Arrange
            var search = new PriceListSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            var priceLists = new List<PriceListDto>
            {
                new PriceListDto
                {
                    PriceListId = 1,
                    PriceListName = "Price List 1",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(10),
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-15)
                },
                new PriceListDto
                {
                    PriceListId = 2,
                    PriceListName = "Price List 2",
                    StartDate = DateTime.Now.AddDays(-5),
                    EndDate = DateTime.Now.AddDays(15),
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-10)
                }
            };

            _mockPriceListService
                .Setup(x => x.GetAllData(It.IsAny<PriceListSearch>()))
                .ReturnsAsync(priceLists);

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<PriceListOutputVM>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(2);
            response.Data.Items.First().PriceListName.Should().Be("Price List 1");
        }

        /// <summary>
        /// TCID02: Xu ly loi service khi thuc hien GetData
        /// Input: PriceListSearch hop le nhung service nem ngoai le
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task GetData_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new PriceListSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockPriceListService
                .Setup(x => x.GetAllData(It.IsAny<PriceListSearch>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetData(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<List<PriceListDto>>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Có lỗi xảy ra khi lấy danh sách bảng giá.");
        }

        #endregion

        #region GetDataForExport Tests

        /// <summary>
        /// TCID03: Lay danh sach bang gia cho xuat du lieu chi hien thi bang gia tuong lai hoac dang hoat dong
        /// Input: PriceListSearch voi PageIndex 1 va PageSize 10
        /// Expected: Tra ve OkObjectResult chi chua bang gia co EndDate lon hon ngay hien tai
        /// </summary>
        [Fact]
        public async Task GetDataForExport_WithValidSearch_ReturnsOnlyFuturePriceLists()
        {
            // Arrange
            var search = new PriceListSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            var priceLists = new List<PriceListDto>
            {
                new PriceListDto
                {
                    PriceListId = 1,
                    PriceListName = "Future Price List",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(10), // Future
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-15)
                },
                new PriceListDto
                {
                    PriceListId = 2,
                    PriceListName = "Expired Price List",
                    StartDate = DateTime.Now.AddDays(-20),
                    EndDate = DateTime.Now.AddDays(-5), // Past
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-25)
                }
            };

            _mockPriceListService
                .Setup(x => x.GetAllData(It.IsAny<PriceListSearch>()))
                .ReturnsAsync(priceLists);

            // Act
            var result = await _controller.GetDataForExport(search);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PagedList<PriceListOutputVM>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Items.Should().HaveCount(1);
            response.Data.Items.First().PriceListName.Should().Be("Future Price List");
        }

        /// <summary>
        /// TCID04: Xu ly loi service khi thuc hien GetDataForExport
        /// Input: PriceListSearch hop le nhung service nem ngoai le
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task GetDataForExport_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var search = new PriceListSearch
            {
                PageIndex = 1,
                PageSize = 10
            };

            _mockPriceListService
                .Setup(x => x.GetAllData(It.IsAny<PriceListSearch>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDataForExport(search);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<List<PriceListDto>>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region GetDetail Tests

        /// <summary>
        /// TCID05: Lay chi tiet bang gia theo ID hop le
        /// Input: PriceListId hop le co ton tai trong he thong
        /// Expected: Tra ve OkObjectResult voi thong tin bang gia va danh sach chi tiet san pham
        /// </summary>
        [Fact]
        public async Task GetDetail_WithValidId_ReturnsOkWithDetails()
        {
            // Arrange
            var priceListId = 1;
            var priceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List 1",
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(10),
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-15)
            };

            var details = new List<PriceListDetailOutputVM>
            {
                new PriceListDetailOutputVM
                {
                    PriceListDetailId = 1,
                    ProductId = 1,
                    ProductName = "Product 1",
                    ProductCode = "P001",
                    Price = 100000,
                    Note = "Test note"
                }
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(priceList);

            _mockPriceListDetailService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(details);

            // Act
            var result = await _controller.GetDetail(priceListId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<PriceListOutputVM>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.PriceListId.Should().Be(priceListId);
            response.Data.PriceListDetails.Should().HaveCount(1);
        }

        /// <summary>
        /// TCID06: Thu lay chi tiet bang gia khong ton tai
        /// Input: PriceListId khong ton tai trong he thong
        /// Expected: Tra ve NotFoundObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task GetDetail_WithNonExistentPriceList_ReturnsNotFound()
        {
            // Arrange
            var priceListId = 999;

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync((PriceListDto?)null);

            // Act
            var result = await _controller.GetDetail(priceListId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy bảng giá.");
        }

        /// <summary>
        /// TCID07: Lay bang gia khong co chi tiet san pham
        /// Input: PriceListId hop le nhung khong co chi tiet san pham nao
        /// Expected: Tra ve NotFoundObjectResult voi thong bao loi khong tim thay chi tiet bang gia
        /// </summary>
        [Fact]
        public async Task GetDetail_WithNonExistentDetails_ReturnsNotFound()
        {
            // Arrange
            var priceListId = 1;
            var priceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List 1"
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(priceList);

            _mockPriceListDetailService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync((List<PriceListDetailOutputVM>?)null);

            // Act
            var result = await _controller.GetDetail(priceListId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy chi tiết bảng giá.");
        }

        /// <summary>
        /// TCID08: Xu ly loi service khi thuc hien GetDetail
        /// Input: PriceListId hop le nhung service nem ngoai le
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task GetDetail_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var priceListId = 1;

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDetail(priceListId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<PriceListDto>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region Create Tests

        /// <summary>
        /// TCID09: Tao moi bang gia voi du lieu hop le bao gom chi tiet san pham
        /// Input: PriceListCreateVM hop le voi ten thoi gian va danh sach chi tiet san pham
        /// Expected: Tra ve OkObjectResult voi thong bao tao thanh cong va Success true
        /// </summary>
        [Fact]
        public async Task Create_WithValidData_ReturnsOk()
        {
            // Arrange
            var model = new PriceListCreateVM
            {
                PriceListName = "New Price List",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                PriceListDetails = new List<PriceListDetailInputVM>
                {
                    new PriceListDetailInputVM
                    {
                        ProductId = 1,
                        UnitPrice = 100000,
                        Note = "Test"
                    }
                }
            };

            var product = new Product
            {
                ProductId = 1,
                ProductName = "Product 1"
            };

            _mockProductService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product);

            _mockPriceListService
                .Setup(x => x.CreateAsync(It.IsAny<PriceList>()))
                .Callback<PriceList>(dto => dto.PriceListId = 1)
                .Returns(Task.CompletedTask);

            _mockPriceListDetailService
                .Setup(x => x.CreateAsync(It.IsAny<PriceListDetailDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("Tạo bảng giá thành công.");
        }

        /// <summary>
        /// TCID10: Kiem tra rang buoc ngay thang khi tao bang gia voi ngay ket thuc nho hon ngay bat dau
        /// Input: PriceListCreateVM voi EndDate nho hon StartDate
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi ngay thang khong hop le
        /// </summary>
        [Fact]
        public async Task Create_WithInvalidDates_ReturnsBadRequest()
        {
            // Arrange
            var model = new PriceListCreateVM
            {
                PriceListName = "New Price List",
                StartDate = DateTime.Now.AddDays(30),
                EndDate = DateTime.Now, // End date before start date
                PriceListDetails = new List<PriceListDetailInputVM>()
            };

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
        }

        /// <summary>
        /// TCID11: Kiem tra san pham ton tai khi tao bang gia voi san pham khong ton tai
        /// Input: PriceListCreateVM voi ProductId khong ton tai trong he thong
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi san pham khong ton tai
        /// </summary>
        [Fact]
        public async Task Create_WithNonExistentProduct_ReturnsBadRequest()
        {
            // Arrange
            var model = new PriceListCreateVM
            {
                PriceListName = "New Price List",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                PriceListDetails = new List<PriceListDetailInputVM>
                {
                    new PriceListDetailInputVM
                    {
                        ProductId = 999,
                        UnitPrice = 100000
                    }
                }
            };

            _mockPriceListService
                .Setup(x => x.CreateAsync(It.IsAny<PriceList>()))
                .Callback<PriceList>(dto => dto.PriceListId = 1)
                .Returns(Task.CompletedTask);

            _mockProductService
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Sản phẩm với ID 999 không tồn tại");
        }

        /// <summary>
        /// TCID12: Xu ly loi service khi thuc hien tao bang gia
        /// Input: PriceListCreateVM hop le nhung service nem ngoai le
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task Create_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var model = new PriceListCreateVM
            {
                PriceListName = "New Price List",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30)
            };

            _mockPriceListService
                .Setup(x => x.CreateAsync(It.IsAny<PriceList>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region Update Tests

        /// <summary>
        /// TCID13: Cap nhat bang gia ton tai voi du lieu hop le
        /// Input: PriceListId hop le va PriceListUpdateVM voi du lieu moi
        /// Expected: Tra ve OkObjectResult voi thong bao cap nhat thanh cong va Success true
        /// </summary>
        [Fact]
        public async Task Update_WithValidData_ReturnsOk()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListUpdateVM
            {
                PriceListName = "Updated Price List",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = true
            };

            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Old Price List",
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(10),
                IsActive = true
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListService
                .Setup(x => x.UpdateAsync(It.IsAny<PriceListDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(priceListId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("Cập nhật bảng giá thành công.");
        }

        /// <summary>
        /// TCID14: Thu cap nhat bang gia khong ton tai
        /// Input: PriceListId khong ton tai va PriceListUpdateVM hop le
        /// Expected: Tra ve NotFoundObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task Update_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var priceListId = 999;
            var model = new PriceListUpdateVM
            {
                PriceListName = "Updated Price List",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = true
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync((PriceListDto?)null);

            // Act
            var result = await _controller.Update(priceListId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy bảng giá.");
        }

        /// <summary>
        /// TCID15: Kiem tra rang buoc ngay thang khi cap nhat bang gia voi ngay ket thuc nho hon ngay bat dau
        /// Input: PriceListId hop le va PriceListUpdateVM voi EndDate nho hon StartDate
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi ngay thang khong hop le
        /// </summary>
        [Fact]
        public async Task Update_WithInvalidDates_ReturnsBadRequest()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListUpdateVM
            {
                PriceListName = "Updated Price List",
                StartDate = DateTime.Now.AddDays(30),
                EndDate = DateTime.Now,
                IsActive = true
            };

            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Old Price List"
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            // Act
            var result = await _controller.Update(priceListId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
        }

        /// <summary>
        /// TCID16: Xu ly loi service khi thuc hien cap nhat bang gia
        /// Input: PriceListId hop le va PriceListUpdateVM hop le nhung service nem ngoai le
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task Update_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListUpdateVM
            {
                PriceListName = "Updated Price List",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = true
            };

            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Old Price List"
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListService
                .Setup(x => x.UpdateAsync(It.IsAny<PriceListDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Update(priceListId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region UpdatePriceListDetail Tests

        /// <summary>
        /// TCID17: Cap nhat chi tiet bang gia voi du lieu hop le
        /// Input: PriceListId hop le va PriceListDetailUpdateVM voi danh sach chi tiet san pham moi
        /// Expected: Tra ve OkObjectResult voi thong bao cap nhat thanh cong va Success true
        /// </summary>
        [Fact]
        public async Task UpdatePriceListDetail_WithValidData_ReturnsOk()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListDetailUpdateVM
            {
                PriceListDetails = new List<PriceListDetailInputVM>
                {
                    new PriceListDetailInputVM
                    {
                        ProductId = 1,
                        UnitPrice = 120000,
                        Note = "Updated"
                    }
                }
            };

            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List 1"
            };

            var existingDetails = new List<PriceListDetailDto>
            {
                new PriceListDetailDto
                {
                    PriceListDetailId = 1,
                    PriceListId = priceListId,
                    ProductId = 1,
                    Price = 100000
                }
            };

            var product = new Product
            {
                ProductId = 1,
                ProductName = "Product 1"
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListDetailService
                .Setup(x => x.GetById(priceListId))
                .ReturnsAsync(existingDetails);

            _mockProductService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product);

            _mockPriceListDetailService
                .Setup(x => x.UpdateAsync(It.IsAny<PriceListDetailDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdatePriceListDetail(priceListId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("Cập nhật chi tiết bảng giá thành công.");
        }

        /// <summary>
        /// TCID18: Thu cap nhat chi tiet cho bang gia khong ton tai
        /// Input: PriceListId khong ton tai va PriceListDetailUpdateVM hop le
        /// Expected: Tra ve NotFoundObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task UpdatePriceListDetail_WithNonExistentPriceList_ReturnsNotFound()
        {
            // Arrange
            var priceListId = 999;
            var model = new PriceListDetailUpdateVM
            {
                PriceListDetails = new List<PriceListDetailInputVM>()
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync((PriceListDto?)null);

            // Act
            var result = await _controller.UpdatePriceListDetail(priceListId, model);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy bảng giá.");
        }

        /// <summary>
        /// TCID19: Kiem tra san pham ton tai khi cap nhat chi tiet bang gia voi san pham khong ton tai
        /// Input: PriceListId hop le va PriceListDetailUpdateVM voi ProductId khong ton tai
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi san pham khong ton tai
        /// </summary>
        [Fact]
        public async Task UpdatePriceListDetail_WithNonExistentProduct_ReturnsBadRequest()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListDetailUpdateVM
            {
                PriceListDetails = new List<PriceListDetailInputVM>
                {
                    new PriceListDetailInputVM
                    {
                        ProductId = 999,
                        UnitPrice = 120000
                    }
                }
            };

            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List 1"
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListDetailService
                .Setup(x => x.GetById(priceListId))
                .ReturnsAsync(new List<PriceListDetailDto>());

            _mockProductService
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _controller.UpdatePriceListDetail(priceListId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Contain("Sản phẩm với ID 999 không tồn tại");
        }

        /// <summary>
        /// TCID20: Cap nhat chi tiet bang gia va tu dong xoa san pham bi loai bo khoi danh sach
        /// Input: PriceListId hop le va PriceListDetailUpdateVM voi danh sach san pham giam di
        /// Expected: Tra ve OkObjectResult va san pham bi loai bo duoc xoa tu database
        /// </summary>
        [Fact]
        public async Task UpdatePriceListDetail_DeletesRemovedProducts_ReturnsOk()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListDetailUpdateVM
            {
                PriceListDetails = new List<PriceListDetailInputVM>
                {
                    new PriceListDetailInputVM
                    {
                        ProductId = 1,
                        UnitPrice = 120000
                    }
                    // Product 2 is removed
                }
            };

            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List 1"
            };

            var existingDetails = new List<PriceListDetailDto>
            {
                new PriceListDetailDto
                {
                    PriceListDetailId = 1,
                    PriceListId = priceListId,
                    ProductId = 1,
                    Price = 100000
                },
                new PriceListDetailDto
                {
                    PriceListDetailId = 2,
                    PriceListId = priceListId,
                    ProductId = 2,
                    Price = 200000
                }
            };

            var product1 = new Product { ProductId = 1, ProductName = "Product 1" };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListDetailService
                .Setup(x => x.GetById(priceListId))
                .ReturnsAsync(existingDetails);

            _mockProductService
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product1);

            _mockPriceListDetailService
                .Setup(x => x.DeleteAsync(It.IsAny<PriceListDetailDto>()))
                .Returns(Task.CompletedTask);

            _mockPriceListDetailService
                .Setup(x => x.UpdateAsync(It.IsAny<PriceListDetailDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdatePriceListDetail(priceListId, model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();

            _mockPriceListDetailService.Verify(
                x => x.DeleteAsync(It.Is<PriceListDetailDto>(d => d.ProductId == 2)),
                Times.Once);
        }

        /// <summary>
        /// TCID21: Xu ly loi service khi thuc hien cap nhat chi tiet bang gia
        /// Input: PriceListId hop le va PriceListDetailUpdateVM hop le nhung service nem ngoai le
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task UpdatePriceListDetail_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var priceListId = 1;
            var model = new PriceListDetailUpdateVM
            {
                PriceListDetails = new List<PriceListDetailInputVM>()
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdatePriceListDetail(priceListId, model);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion

        #region Delete Tests

        /// <summary>
        /// TCID22: Xoa mem bang gia bang cach dat IsActive thanh false voi ID hop le
        /// Input: PriceListId hop le co ton tai trong he thong
        /// Expected: Tra ve OkObjectResult voi thong bao vo hieu hoa thanh cong va IsActive duoc dat thanh false
        /// </summary>
        [Fact]
        public async Task Delete_WithValidId_SoftDeletesAndReturnsOk()
        {
            // Arrange
            var priceListId = 1;
            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List to Delete",
                IsActive = true
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListService
                .Setup(x => x.UpdateAsync(It.IsAny<PriceListDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(priceListId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("Vô hiệu hóa bảng giá thành công.");

            _mockPriceListService.Verify(
                x => x.UpdateAsync(It.Is<PriceListDto>(p => p.IsActive == false)),
                Times.Once);
        }

        /// <summary>
        /// TCID23: Thu xoa bang gia khong ton tai
        /// Input: PriceListId khong ton tai trong he thong
        /// Expected: Tra ve NotFoundObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task Delete_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var priceListId = 999;

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync((PriceListDto?)null);

            // Act
            var result = await _controller.Delete(priceListId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Error!.Message.Should().Be("Không tìm thấy bảng giá.");
        }

        /// <summary>
        /// TCID24: Xu ly loi service khi thuc hien xoa bang gia
        /// Input: PriceListId hop le nhung service nem ngoai le khi cap nhat
        /// Expected: Tra ve BadRequestObjectResult voi thong bao loi va Success false
        /// </summary>
        [Fact]
        public async Task Delete_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var priceListId = 1;
            var existingPriceList = new PriceListDto
            {
                PriceListId = priceListId,
                PriceListName = "Price List to Delete",
                IsActive = true
            };

            _mockPriceListService
                .Setup(x => x.GetByPriceListId(priceListId))
                .ReturnsAsync(existingPriceList);

            _mockPriceListService
                .Setup(x => x.UpdateAsync(It.IsAny<PriceListDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Delete(priceListId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
        }

        #endregion
    }
}
