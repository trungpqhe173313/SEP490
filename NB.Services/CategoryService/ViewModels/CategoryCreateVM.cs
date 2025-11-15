using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService.ViewModels
{
    public class CategoryCreateVM
    {
        public string CategoryName { get; set; } = null!;

        public string Description { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
