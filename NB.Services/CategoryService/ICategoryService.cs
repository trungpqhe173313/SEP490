using NB.Model.Entities;
using NB.Service.CategoryService.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService
{
    public interface ICategoryService : IService<Category>
    {
        Task<List<CategoryDto?>> GetData();

        Task<List<CategoryDetailDto>> GetDataWithProducts();

        Task<CategoryDto?> GetById(int id);

        Task<CategoryDetailDto?> GetByIdWithProducts(int id);

        Task<CategoryDto?> GetByName(string name);
    }
}
