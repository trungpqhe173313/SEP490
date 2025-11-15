using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService.ViewModels
{
    public class CategoryUpdateVM
    {

        public string CategoryName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public bool? IsActive { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
