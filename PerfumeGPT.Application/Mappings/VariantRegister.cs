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
			config.NewConfig<CreateVariantRequest, ProductVariant>()
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Sillage, src => src.Sillage)
				.Map(dest => dest.Longevity, src => src.Longevity)
				.Map(dest => dest.Status, src => src.Status);

			config.NewConfig<UpdateVariantRequest, ProductVariant>()
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Sillage, src => src.Sillage)
				.Map(dest => dest.Longevity, src => src.Longevity)
				.Map(dest => dest.Status, src => src.Status);

			config.NewConfig<ProductVariant, VariantPagedItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted))
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.StockQuantity, src => src.Stock.TotalQuantity - src.Stock.ReservedQuantity)
				.Map(dest => dest.Attributes, src => src.ProductAttributes);

			config.NewConfig<ProductVariant, ProductVariantResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.ProductName, src => src.Product.Name)
				.Map(dest => dest.Media, src => src.Media.Where(m => !m.IsDeleted))
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.StockQuantity, src => src.Stock.TotalQuantity - src.Stock.ReservedQuantity)
				.Map(dest => dest.Sillage, src => src.Sillage)
				.Map(dest => dest.Longevity, src => src.Longevity)
				.Map(dest => dest.Attributes, src => src.ProductAttributes);

			config.NewConfig<ProductVariant, VariantLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.DisplayName, src => $"{src.Product.Name ?? "Unknown"} - {src.VolumeMl}ml {src.Concentration.Name ?? "Unknown"}")
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted));

			config.NewConfig<ProductVariant, VariantCreateOrder>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.UnitPrice, src => src.BasePrice)
				.Map(dest => dest.Snapshot, src => $"{src.Product.Name} - {src.VolumeMl}ml - {src.Concentration.Name} - {src.Type}");

			config.NewConfig<ProductVariant, VariantSummaryItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.DisplayName, src => $"{src.Concentration.Name} - {src.VolumeMl}ml")
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted))
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.StockQuantity, src => src.Stock.TotalQuantity - src.Stock.ReservedQuantity)
				.Map(dest => dest.Attributes, src => src.ProductAttributes);
		}
	}
}



