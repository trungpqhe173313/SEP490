using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService.Dto
{
    public class CategoryUpdateVM
    {

        public string CategoryName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public DateTime? UpdatedAt { get; set; }
    }
}
