using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService.Dto
{
    public class CategorySearch : SearchBase
    {
        public string? CategoryName { get; set; }
    }
}
