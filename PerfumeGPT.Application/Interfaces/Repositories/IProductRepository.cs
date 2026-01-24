using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IProductRepository : IGenericRepository<Product>
	{
		/// <summary>
		/// Gets a product with all related data (Brand, Category, Family, Variants with Concentrations).
		/// </summary>
		Task<Product?> GetProductWithDetailsAsync(Guid productId);

		/// <summary>
		/// Gets paged products with related data (Brand, Category, Family).
		/// </summary>
		Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithDetailsAsync(GetPagedProductRequest request);
	}
}

