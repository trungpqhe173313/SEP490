using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ContractService;
using NB.Service.ContractService.Dto;
using NB.Service.ContractService.ViewModels;
using NB.Service.Dto;
using NB.Service.RoleService;
using NB.Service.SupplierService;
using NB.Service.SupplierService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.UserRoleService;
using NB.Service.UserService;
using NB.Service.UserService.ViewModels;

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
        private readonly int role = 4;

        public ContractController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            ISupplierService supplierService,
            IContractService contractService,
            ILogger<ContractController> logger,
            IMapper mapper)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _supplierService = supplierService;
            _contractService = contractService;
            _logger = logger;
            _mapper = mapper;
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
            if (search.FromDate > DateTime.UtcNow)
                return BadRequest(ApiResponse<object>.Fail("Ngày tạo phải là ngày quá khứ", 400));
            try
            {

                var contracts = await _contractService.GetData(search);
                List<ContractOutputVM> outputs = new List<ContractOutputVM>();
                foreach (var contract in contracts)
                {
                    var customer = await _userService.GetByUserId((int)contract.UserId);
                    
                        
                    var supplier = await _supplierService.GetBySupplierId((int)contract.SupplierId);
                    var output = new ContractOutputVM
                    {
                        ContractId = contract.ContractId,
                        CustomerName = customer != null ? customer.FullName : "N/A",
                        SupplierName = supplier != null ? supplier.SupplierName : "N/A",
                        Image = contract.Image,
                        Pdf = contract.Pdf,
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
                var customer = await _userService.GetByUserId((int)contract.UserId);
                var supplier = await _supplierService.GetBySupplierId((int)contract.SupplierId);
                var contractDetail = new ContractDetailVM
                {
                    ContractId = contract.ContractId,
                    Image = contract.Image,
                    Pdf = contract.Pdf,
                    IsActive = contract.IsActive,
                    CreatedAt = contract.CreatedAt,
                    UpdatedAt = contract.UpdatedAt,
                    Customer = new CustomerOutputVM
                    {
                        UserId = customer.UserId,
                        Email = customer.Email,
                        FullName = customer.FullName,
                        Phone = customer.Phone,
                        Image = customer.Image
                    },
                    Supplier = new SupplierOutputVM
                    {
                        SupplierId = supplier.SupplierId,
                        SupplierName = supplier.SupplierName,
                        Email = supplier.Email,
                        Phone = supplier.Phone,
                        Status = supplier.IsActive switch
                        {
                            false => "Ngừng hoạt động",
                            true => "Đang hoạt động"
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
        public async Task<IActionResult> CreateContract([FromBody] CreateContractVM request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            var customer = await _userService.GetByUserId(request.UserId);
            var customerRole = await _userRoleService.GetByRoleId(role);
            bool isInRole = customerRole.Any(cus => cus.UserId == customer.UserId);
            if(customer == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Khách hàng không tồn tại", 400));
            }
            if (isInRole == false)
            {
                return BadRequest(ApiResponse<object>.Fail("Người dùng không phải khách hàng", 400));
            }
            var supplier = await _supplierService.GetBySupplierId(request.SupplierId);
            if (supplier == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Nhà cung cấp không tồn tại", 400));
            }
            try
            {
                var contract = new Contract
                {
                    UserId = request.UserId,
                    SupplierId = request.SupplierId,
                    Image = request.Image,
                    Pdf = request.Pdf,
                    
                };
                contract.CreatedAt = DateTime.UtcNow;
                contract.UpdatedAt = DateTime.UtcNow;
                contract.IsActive = true;
                await _contractService.CreateAsync(contract);

                var result = new ContractOutputVM
                {
                    ContractId = contract.ContractId,
                    CustomerName = (await _userService.GetByUserId((int)contract.UserId)).FullName,
                    SupplierName = (await _supplierService.GetBySupplierId((int)contract.SupplierId)).SupplierName,
                    Image = contract.Image,
                    Pdf = contract.Pdf,
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

        [HttpPut("UpdateContract/{ContractId}")]

        public async Task<IActionResult> UpdateContract(int ContractId, [FromBody] UpdateContractVM request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if(ContractId <= 0)
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
                contract.Image = request.Image;
                contract.Pdf = request.Pdf;
                contract.IsActive = request.IsActive;
                contract.UpdatedAt = DateTime.UtcNow;
                await _contractService.UpdateAsync(contract);
                return Ok(ApiResponse<object>.Ok("Cập nhật hợp đồng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hợp đồng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật hợp đồng", 400));
            }
        }
    }
}
