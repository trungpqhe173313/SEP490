using Moq;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using NB.Service.Common;
using System.Linq.Expressions;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace NB.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IRepository<Product>> _productRepositoryMock;
        private readonly Mock<IRepository<Supplier>> _supplierRepositoryMock;
        private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
        private readonly Mock<IRepository<Inventory>> _inventoryRepositoryMock;
        private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
        private readonly Mock<IRepository<TransactionDetail>> _transactionDetailRepositoryMock;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _productRepositoryMock = new Mock<IRepository<Product>>();
            _supplierRepositoryMock = new Mock<IRepository<Supplier>>();
            _categoryRepositoryMock = new Mock<IRepository<Category>>();
            _inventoryRepositoryMock = new Mock<IRepository<Inventory>>();
            _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
            _transactionDetailRepositoryMock = new Mock<IRepository<TransactionDetail>>();
            _productService = new ProductService(
                _productRepositoryMock.Object,
                _supplierRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _inventoryRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _transactionDetailRepositoryMock.Object);
        }
        // GetById Tests
        private IQueryable<Product> GetTestProducts()
        {
            var supplier = new Supplier { SupplierId = 1, SupplierName = "Test Supplier" };
            var category = new Category { CategoryId = 1, CategoryName = "Test Category" };

            return new List<Product>
            {
                new Product {
                    ProductId = 1,
                    ProductName = "Product 1",
                    ProductCode = "P001",
                    CategoryId = 1,
                    SupplierId = 1,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Supplier = supplier,
                    Category = category
                },
                new Product { ProductId = 2, ProductName = "Product 2", ProductCode = "P002", CategoryId = 2, SupplierId = 1, IsAvailable = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Product { ProductId = 3, ProductName = "Another Product", ProductCode = "P003", CategoryId = 1, SupplierId = 2, IsAvailable = true, CreatedAt = DateTime.UtcNow }
            }.AsQueryable();
        }

        [Fact]
        public async Task GetById_ShouldReturnProductDto_WhenProductExists()
        {
            // Arrange
            var products = GetTestProducts();
            var productId = 1;
            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(products);

            // Act
            var result = await _productService.GetById(productId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ProductDto>();
            result.ProductId.Should().Be(productId);
            result.ProductName.Should().Be("Product 1");
            result.SupplierName.Should().Be("Test Supplier");
            result.CategoryName.Should().Be("Test Category");
        }

        [Theory]
        [InlineData(100)]  // ID không tồn tại
        [InlineData(0)]    // ID không hợp lệ
        [InlineData(-1)]   // ID không hợp lệ
        public async Task GetById_ShouldReturnNull_WhenProductNotFoundOrIdIsInvalid(int testId)
        {
            // Arrange
            var products = GetTestProducts();
            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(products);

            // Act
            var result = await _productService.GetById(testId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetData_WithSearchCriteria_ShouldReturnFilteredProducts()
        {
            // Arrange
            var products = GetTestProducts();
            var search = new ProductSearch { ProductName = "Product", IsAvailable = true, PageIndex = 1, PageSize = 10 };

            var mockSet = new Mock<DbSet<Product>>();
            mockSet.As<IQueryable<Product>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Product>(products.Provider));
            mockSet.As<IQueryable<Product>>().Setup(m => m.Expression).Returns(products.Expression);
            mockSet.As<IQueryable<Product>>().Setup(m => m.ElementType).Returns(products.ElementType);
            mockSet.As<IQueryable<Product>>().Setup(m => m.GetEnumerator()).Returns(() => products.GetEnumerator());

            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // Act
            var result = await _productService.GetData(search);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.Items.Should().Contain(p => p.ProductName == "Product 1");
            result.Items.Should().Contain(p => p.ProductName == "Another Product");
        }
        // GetByIds Tests
        public static IEnumerable<object[]> GetByIdsTestData()
        {
            // { inputIds, expectedCount }
            yield return new object[] { new List<int> { 1, 2, 3 }, 3 };
            yield return new object[] { new List<int> { 1, 100, 2 }, 2 };
            yield return new object[] { new List<int> { 100, 200 }, 0 };
            yield return new object[] { new List<int>(), 0 };
            yield return new object[] { new List<int> { 1, 1, 2 }, 2 };
            yield return new object[] { new List<int> { -1, 1, 2 }, 2 };
        }

        [Theory]
        [MemberData(nameof(GetByIdsTestData))]
        public async Task GetByIds_ShouldReturnCorrectProducts_ForVariousInputs(List<int> ids, int expectedCount)
        {
            // Arrange
            var products = GetTestProducts();
            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(products);

            // Act
            var result = await _productService.GetByIds(ids);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(expectedCount);
            if (expectedCount > 0)
            {
                // Ensure no duplicate products are returned
                result.Select(p => p.ProductId).Should().OnlyHaveUniqueItems();
            }
        }

        [Fact]
        public async Task GetByIds_ShouldReturnEmptyList_WhenInputIsNull()
        {
            // Arrange
            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(GetTestProducts());

            // Act
            var result = await _productService.GetByIds(null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // GetByInventory Tests
        public static IEnumerable<object[]> GetByInventoryTestData()
        {
            // { inputProductIds, expectedCount }
            yield return new object[] { new List<int> { 1, 2 }, 2 };
            yield return new object[] { new List<int> { 1, 100 }, 1 };
            yield return new object[] { new List<int>(), 0 };
            yield return new object[] { new List<int> { 1, 1, 2 }, 2 };
            yield return new object[] { new List<int> { -1, 1, 2 }, 2 };
        }

        [Theory]
        [MemberData(nameof(GetByInventoryTestData))]
        public async Task GetByInventory_ShouldReturnCorrectProducts_ForVariousInputs(List<int> productIds, int expectedCount)
        {
            // Arrange
            var inventoryList = productIds.Select(id => new NB.Service.InventoryService.Dto.InventoryDto { ProductId = id }).ToList();
            var products = GetTestProducts();
            
            var mockSet = new Mock<DbSet<Product>>();
            mockSet.As<IQueryable<Product>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Product>(products.Provider));
            mockSet.As<IQueryable<Product>>().Setup(m => m.Expression).Returns(products.Expression);
            mockSet.As<IQueryable<Product>>().Setup(m => m.ElementType).Returns(products.ElementType);
            mockSet.As<IQueryable<Product>>().Setup(m => m.GetEnumerator()).Returns(() => products.GetEnumerator());

            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            // Act
            var result = await _productService.GetByInventory(inventoryList);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(expectedCount);
            if (expectedCount > 0)
            {
                result.Select(p => p.ProductId).Should().OnlyHaveUniqueItems();
            }
        }

        [Fact]
        public void GetByInventory_ShouldThrowException_WhenInputIsNull()
        {
            // Arrange
            // No arrangement needed as we are testing null input

            // Act
            Func<Task> act = async () => await _productService.GetByInventory(null);

            // Assert
            act.Should().ThrowAsync<NullReferenceException>();
        }

        // GetByCode Tests
        [Theory]
        [InlineData("P001")]
        [InlineData("  P001  ")]
        public async Task GetByCode_ShouldReturnProduct_WhenCodeExistsAndIsValid(string code)
        {
            // Arrange
            var products = GetTestProducts();
            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(products);

            // Act
            var result = await _productService.GetByCode(code);

            // Assert
            result.Should().NotBeNull();
            result.ProductCode.Should().Be("P001");
        }

        [Theory]
        [InlineData("P005")]     // Code does not exist
        [InlineData("")]         // Empty string
        [InlineData("   ")]      // Whitespace
        [InlineData("p001")]     // Case-sensitive mismatch
        [InlineData("P-001")]    // Different format
        public async Task GetByCode_ShouldReturnNull_WhenCodeDoesNotExistOrIsInvalid(string code)
        {
            // Arrange
            var products = GetTestProducts();
            _productRepositoryMock.Setup(r => r.GetQueryable()).Returns(products);

            // Act
            var result = await _productService.GetByCode(code);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetByCode_ShouldThrowException_WhenInputIsNull()
        {
            // Arrange
            // No arrangement needed

            // Act
            Func<Task> act = async () => await _productService.GetByCode(null);

            // Assert
            act.Should().ThrowAsync<NullReferenceException>();
        }
    }
    

    // Helper class for mocking async queryable
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(
                                      name: nameof(IQueryProvider.Execute),
                                      genericParameterCount: 1,
                                      types: new[] { typeof(Expression) })
                                 .MakeGenericMethod(expectedResultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                                        .MakeGenericMethod(expectedResultType)
                                        .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }
}
