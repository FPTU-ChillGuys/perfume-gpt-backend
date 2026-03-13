using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.DTOs.Responses.Variants;
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

			config.NewConfig<UpdateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.Description, src => src.Description);

			config.NewConfig<Product, ProductInforResponse>()
				.Map(dest => dest.ProductCode, src => src.Id.ToString())
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.Origin, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "ORIGIN")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.ReleaseYear, src => src.CreatedAt.Year)
				.Map(dest => dest.ScentGroup, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "FAMILY")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.Style, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "STYLE")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.TopNotes, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "TOP_NOTES")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.MiddleNotes, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "MIDDLE_NOTES")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.BaseNotes, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "BASE_NOTES")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.Description, src => src.Description);

			config.NewConfig<Product, ProductResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.Media, src => src.Media.Where(m => !m.IsDeleted))
				.Map(dest => dest.Variants, src => src.Variants)
				.Map(dest => dest.Attributes, src => src.ProductAttributes);

			config.NewConfig<ProductVariant, VariantSummaryItem>()
				.Map(dest => dest.DisplayName, src => $"{src.Concentration.Name} - {src.VolumeMl}ml")
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name);

			config.NewConfig<Product, ProductListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted))
				.Map(dest => dest.Attributes, src => src.ProductAttributes);

			config.NewConfig<Product, ProductListItemWithVariants>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted))
				.Map(dest => dest.Attributes, src => src.ProductAttributes)
				.Map(dest => dest.Variants, src => src.Variants.Where(v => !v.IsDeleted));

			config.NewConfig<Product, ProductLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted));
		}
	}
}
