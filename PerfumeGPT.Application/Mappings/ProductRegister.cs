using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ProductRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CreateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.Description, src => src.Description);

			// UpdateProductRequest -> Product
			config.NewConfig<UpdateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.Description, src => src.Description);

			// Product -> ProductResponse
			config.NewConfig<Product, ProductResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.Media, src => src.Media)
				.Map(dest => dest.Variants, src => src.Variants)
				.Map(dest => dest.Attributes, src => src.ProductAttributes);

			// Product -> ProductListItem
			config.NewConfig<Product, ProductListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary))
				.Map(dest => dest.Attributes, src => src.ProductAttributes);

			// Product -> ProductLookupItem
			config.NewConfig<Product, ProductLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary));
		}
	}
}
