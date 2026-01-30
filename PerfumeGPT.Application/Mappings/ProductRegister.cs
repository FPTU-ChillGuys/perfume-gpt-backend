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
			// CreateProductRequest -> Product
			config.NewConfig<CreateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.FamilyId, src => src.FamilyId)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.TopNotes, src => src.TopNotes)
				.Map(dest => dest.MiddleNotes, src => src.MiddleNotes)
				.Map(dest => dest.BaseNotes, src => src.BaseNotes)
				.Ignore(dest => dest.Id)
				.Ignore(dest => dest.IsDeleted)
				.Ignore(dest => dest.DeletedAt)
				.Ignore(dest => dest.CreatedAt)
				.Ignore(dest => dest.UpdatedAt)
				.Ignore(dest => dest.Brand)
				.Ignore(dest => dest.Category)
				.Ignore(dest => dest.FragranceFamily)
				.Ignore(dest => dest.Variants)
				.Ignore(dest => dest.Media);

			// UpdateProductRequest -> Product (existing instance)
			config.NewConfig<UpdateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.FamilyId, src => src.FamilyId)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.TopNotes, src => src.TopNotes)
				.Map(dest => dest.MiddleNotes, src => src.MiddleNotes)
				.Map(dest => dest.BaseNotes, src => src.BaseNotes)
				.Ignore(dest => dest.Id)
				.Ignore(dest => dest.Gender)
				.Ignore(dest => dest.IsDeleted)
				.Ignore(dest => dest.DeletedAt)
				.Ignore(dest => dest.CreatedAt)
				.Ignore(dest => dest.UpdatedAt)
				.Ignore(dest => dest.Brand)
				.Ignore(dest => dest.Category)
				.Ignore(dest => dest.FragranceFamily)
				.Ignore(dest => dest.Variants)
				.Ignore(dest => dest.Media);

			// Product -> ProductResponse
			config.NewConfig<Product, ProductResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.FamilyId, src => src.FamilyId)
				.Map(dest => dest.FamilyName, src => src.FragranceFamily.Name)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.TopNotes, src => src.TopNotes)
				.Map(dest => dest.MiddleNotes, src => src.MiddleNotes)
				.Map(dest => dest.BaseNotes, src => src.BaseNotes)
				.Map(dest => dest.Media, src => src.Media)
				.Map(dest => dest.Variants, src => src.Variants);

			// Product -> ProductListItem
			config.NewConfig<Product, ProductListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.FamilyId, src => src.FamilyId)
				.Map(dest => dest.FamilyName, src => src.FragranceFamily.Name)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.TopNotes, src => src.TopNotes)
				.Map(dest => dest.MiddleNotes, src => src.MiddleNotes)
				.Map(dest => dest.BaseNotes, src => src.BaseNotes)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary));
		}
	}
}
