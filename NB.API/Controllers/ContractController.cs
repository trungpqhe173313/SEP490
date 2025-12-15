using NB.Service.Core.Mapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NB.API.Utils;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ContractService;
using NB.Service.ContractService.Dto;
using NB.Service.ContractService.ViewModels;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.SupplierService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.UserRoleService;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using static System.DateTime;

namespace NB.API.Controllers
{
    [Route("api/contract")]
    public class ContractController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private readonly ISupplierService _supplierService;
        private readonly IContractService _contractService;
        private readonly ILogger<ContractController> _logger;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly int role = 4;

        public ContractController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            ISupplierService supplierService,
            IContractService contractService,
            ILogger<ContractController> logger,
            IMapper mapper,
            ICloudinaryService cloudinaryService)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _supplierService = supplierService;
            _contractService = contractService;
            _logger = logger;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] ContractSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (search.CustomerId <= 0)
                return BadRequest(ApiResponse<object>.Fail("Mã khách hàng không hợp lệ", 400));
            if (search.SupplierId <= 0)
                return BadRequest(ApiResponse<object>.Fail("Mã nhà cung cấp không hợp lệ", 400));
            if (search.FromDate > Now)
                return BadRequest(ApiResponse<object>.Fail("Ngày tạo phải là ngày quá khứ", 400));
            try
            {

                var contracts = await _contractService.GetData(search);
                List<ContractOutputVM> outputs = new List<ContractOutputVM>();
                foreach (var contract in contracts)
                {
                    var customer = contract.UserId.HasValue
                               ? await _userService.GetByUserId(contract.UserId.Value)
                               : null;

                    var supplier = contract.SupplierId.HasValue
                                   ? await _supplierService.GetBySupplierId(contract.SupplierId.Value)
                                   : null;
                    var output = new ContractOutputVM
                    {
                        ContractId = contract.ContractId,
                        CustomerName = customer?.FullName,
                        SupplierName = supplier?.SupplierName,
                        Image = contract.Image,
                        IsActive = contract.IsActive,
                        CreatedAt = contract.CreatedAt,
                        UpdatedAt = contract.UpdatedAt
                    };
                    outputs.Add(output);
                }

                var pagedResult = new PagedList<ContractOutputVM>(
                    items: outputs,
                    pageIndex: search.PageIndex,
                    pageSize: search.PageSize,
                    totalCount: outputs.Count
                );
                return Ok(ApiResponse<PagedList<ContractOutputVM>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu hợp đồng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy hợp đồng", 400));
            }
        }

        [HttpGet("GetById/{ContractId}")]
        public async Task<IActionResult> GetById(int ContractId)
        {
            if (ContractId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã hợp đồng không hợp lệ", 400));
            }
            try
            {
                var contract = await _contractService.GetByContractId(ContractId);
                if (contract == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Hợp đồng không tồn tại", 404));
                }
                var customer = contract.UserId.HasValue
                               ? await _userService.GetByUserId(contract.UserId.Value)
                               : null;

                var supplier = contract.SupplierId.HasValue
                               ? await _supplierService.GetBySupplierId(contract.SupplierId.Value)
                               : null;
                var contractDetail = new ContractDetailVM
                {
                    ContractId = contract.ContractId,
                    Image = contract.Image,
                    IsActive = contract.IsActive,
                    CreatedAt = contract.CreatedAt,
                    UpdatedAt = contract.UpdatedAt,
                    Customer = new CustomerOutputVM
                    {
                        UserId = customer?.UserId,
                        Email = customer?.Email,
                        FullName = customer?.FullName,
                        Phone = customer?.Phone,
                        Image = customer?.Image
                    },
                    Supplier = new SupplierOutputVM
                    {
                        SupplierId = supplier?.SupplierId,
                        SupplierName = supplier?.SupplierName,
                        Email = supplier?.Email,
                        Phone = supplier?.Phone,
                        Status = supplier?.IsActive switch
                        {
                            false => "Ngừng hoạt động",
                            true => "Đang hoạt động",
                            _ => null,
                        } 
                    }
                };
                return Ok(ApiResponse<ContractDetailVM>.Ok(contractDetail));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu hợp đồng theo ID");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy hợp đồng", 400));
            }
        }

        [HttpPost("CreateContract")]
        public async Task<IActionResult> CreateContract([FromForm] CreateContractVM request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (!request.UserId.HasValue && !request.SupplierId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("Hợp đồng phải có ít nhất một bên", 400));
            }
            if (request.UserId.HasValue && request.SupplierId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Hợp đồng chỉ có thể có một bên (Khách hàng HOẶC Nhà cung cấp), không thể có cả hai",
                    400));
            }
            if (request.Image != null)
            {
                var imageExtension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" }; 

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }
            }

            try
            {
                //Validate User và Supplier 
                UserDto? customer = null;
                SupplierDto? supplier = null;

                if (request.UserId.HasValue)
                {
                    customer = await _userService.GetByUserId(request.UserId.Value);
                    if (customer == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Khách hàng không tồn tại", 400));
                    }

                    var customerRole = await _userRoleService.GetByRoleId(role);
                    bool isInRole = customerRole.Any(cus => cus.UserId == customer.UserId);
                    if (!isInRole)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Người dùng không phải khách hàng", 400));
                    }
                }

                if (request.SupplierId.HasValue)
                {
                    supplier = await _supplierService.GetBySupplierId(request.SupplierId.Value);
                    if (supplier == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Nhà cung cấp không tồn tại", 400));
                    }
                }

                // Upload image 
                string? imageUrl = null;
                if (request.Image != null)
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(request.Image, "contracts/images");
                    if (imageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }
                }
                var contract = new Contract
                {
                    UserId = request.UserId,
                    SupplierId = request.SupplierId,
                    Image = imageUrl,
                    CreatedAt = Now,
                    UpdatedAt = Now,
                    IsActive = true
                };

                await _contractService.CreateAsync(contract);

                var result = new ContractOutputVM
                {
                    ContractId = contract.ContractId,
                    CustomerName = customer?.FullName,
                    SupplierName = supplier?.SupplierName,
                    Image = contract.Image,
                    IsActive = contract.IsActive,
                    CreatedAt = contract.CreatedAt,
                    UpdatedAt = contract.UpdatedAt,
                };

                return Ok(ApiResponse<ContractOutputVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo hợp đồng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tạo hợp đồng", 400));
            }
        }

        [HttpPut("UpdateContract/{contractId}")]
        public async Task<IActionResult> UpdateContract(int contractId, [FromForm] UpdateContractVM request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (contractId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã hợp đồng không hợp lệ", 400));
            }

            // Validate file type
            if (request.Image != null)
            {
                var imageExtension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"File ảnh phải có định dạng PNG, JPG hoặc JPEG. File hiện tại: {imageExtension}",
                        400));
                }

            }


            try
            {
                var contract = await _contractService.GetByContractId(contractId);
                if (contract == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Hợp đồng không tồn tại", 404));
                }

                // Lưu URL cũ để xóa nếu cần
                string? oldImageUrl = contract.Image;

                // Handle image update 
                if (request.Image != null)
                {
                    string? newImageUrl = await _cloudinaryService.UpdateImageAsync(request.Image, oldImageUrl, "contracts/images");

                    if (newImageUrl == null)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Không thể upload ảnh", 400));
                    }

                    contract.Image = newImageUrl;
                }

                if (request.IsActive.HasValue)
                {
                    contract.IsActive = request.IsActive;
                }

                contract.UpdatedAt = Now;
                await _contractService.UpdateAsync(contract);

                return Ok(ApiResponse<object>.Ok("Cập nhật hợp đồng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hợp đồng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật hợp đồng", 400));
            }
        }

        [HttpDelete("DeleteContract/{contractId}")]
        public async Task<IActionResult> DeleteContract(int contractId)
        {
            if (contractId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã hợp đồng không hợp lệ", 400));
            }
            try
            {
                var contract = await _contractService.GetByContractId(contractId);
                if (contract == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Hợp đồng không tồn tại", 404));
                }
                contract.IsActive = false;
                await _contractService.UpdateAsync(contract);
                return Ok(ApiResponse<object>.Ok("Xóa hợp đồng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa hợp đồng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi xóa hợp đồng", 400));
            }
        }
    }
}
