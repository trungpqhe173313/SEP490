using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.InventoryService.Dto
{
    public class InventorySearch : SearchBase
    {
        [AllowNull]
        public int? ProductId { get; set; }

        [AllowNull]
        public string? ProductName { get; set; }
    }
}
