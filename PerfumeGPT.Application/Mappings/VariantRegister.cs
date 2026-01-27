using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class VariantRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// CreateVariantRequest -> ProductVariant
			config.NewConfig<CreateVariantRequest, ProductVariant>()
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.ImageUrl, src => src.ImageUrl)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status)
				.Ignore(dest => dest.Id)
				.Ignore(dest => dest.Barcode)
				.Ignore(dest => dest.IsDeleted)
				.Ignore(dest => dest.DeletedAt)
				.Ignore(dest => dest.CreatedAt)
				.Ignore(dest => dest.UpdatedAt)
				.Ignore(dest => dest.Product)
				.Ignore(dest => dest.Concentration)
				.Ignore(dest => dest.ImportDetails)
				.Ignore(dest => dest.Batches)
				.Ignore(dest => dest.Stock)
				.Ignore(dest => dest.CartItems)
				.Ignore(dest => dest.OrderDetails);

			// UpdateVariantRequest -> ProductVariant (existing instance)
			config.NewConfig<UpdateVariantRequest, ProductVariant>()
				.Map(dest => dest.ImageUrl, src => src.ImageUrl)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status)
				.Ignore(dest => dest.Id)
				.Ignore(dest => dest.ProductId)
				.Ignore(dest => dest.Barcode)
				.Ignore(dest => dest.IsDeleted)
				.Ignore(dest => dest.DeletedAt)
				.Ignore(dest => dest.CreatedAt)
				.Ignore(dest => dest.UpdatedAt)
				.Ignore(dest => dest.Product)
				.Ignore(dest => dest.Concentration)
				.Ignore(dest => dest.ImportDetails)
				.Ignore(dest => dest.Batches)
				.Ignore(dest => dest.Stock)
				.Ignore(dest => dest.CartItems)
				.Ignore(dest => dest.OrderDetails);

			// ProductVariant -> VariantPagedItem
			config.NewConfig<ProductVariant, VariantPagedItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.ImageUrl, src => src.ImageUrl)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status);

			// ProductVariant -> ProductVariantResponse
			config.NewConfig<ProductVariant, ProductVariantResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.ProductName, src => src.Product.Name)
				.Map(dest => dest.ImageUrl, src => src.ImageUrl)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status);
		}
	}
}


