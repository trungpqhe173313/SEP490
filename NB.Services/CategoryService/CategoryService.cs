using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.CategoryService.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.CategoryService
{
    public class CategoryService : Service<Category>, ICategoryService
    {
        public CategoryService(IRepository<Category> serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<CategoryDto>> GetData()
        {
            var query = from category in GetQueryable()
                        select new CategoryDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt
                        };
            return await query.ToListAsync();
        }

        public async Task<CategoryDto?> GetById(int id)
        {
            var query = from category in GetQueryable()
                        where category.CategoryId == id
                        select new CategoryDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt
                        };
            return await query.FirstOrDefaultAsync();
        }

        public async Task<CategoryDto?> GetByName(string name)
        {
            var query = from category in GetQueryable()
                        where category.CategoryName == name
                        select new CategoryDto
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Description = category.Description,
                            IsActive = category.IsActive,
                            CreatedAt = category.CreatedAt,
                            UpdateAt = category.UpdateAt
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
