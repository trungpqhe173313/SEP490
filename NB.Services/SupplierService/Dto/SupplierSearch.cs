using NB.Service.Dto;

namespace NB.Service.SupplierService.Dto
{
    public class SupplierSearch : SearchBase
    {
        public string? SupplierName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
    }
}
