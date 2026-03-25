using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class VariantRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<ProductVariant, VariantPagedItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.RetailPrice, src => src.RetailPrice)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.StockQuantity, src => src.Stock.TotalQuantity - src.Stock.ReservedQuantity)
				.Map(dest => dest.Attributes, src => src.ProductAttributes)
				.Map(dest => dest.PrimaryImageUrl, src => src.Media.Where(m => m.IsPrimary && !m.IsDeleted).Select(m => m.Url).FirstOrDefault());

			config.NewConfig<ProductVariant, ProductVariantResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductId, src => src.ProductId)
				.Map(dest => dest.ProductName, src => src.Product.Name)
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationId, src => src.ConcentrationId)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.RetailPrice, src => src.RetailPrice)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.StockQuantity, src => src.Stock.TotalQuantity - src.Stock.ReservedQuantity)
				.Map(dest => dest.Sillage, src => src.Sillage)
				.Map(dest => dest.Longevity, src => src.Longevity)
				.Map(dest => dest.Attributes, src => src.ProductAttributes)
				.Map(dest => dest.Media, src =>
					src.Media.Where(m => !m.IsDeleted)
						.Select(m => new MediaResponse
						{
							Id = m.Id,
							Url = m.Url,
							AltText = m.AltText,
							IsPrimary = m.IsPrimary,
							DisplayOrder = m.DisplayOrder,
							MimeType = m.MimeType,
							FileSize = m.FileSize
						}));

			config.NewConfig<ProductVariant, VariantLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Sku, src => src.Sku)
				.Map(dest => dest.Barcode, src => src.Barcode)
				.Map(dest => dest.DisplayName, src =>
					$"{src.Product.Name ?? "Unknown"} - {src.VolumeMl}ml {src.Concentration.Name ?? "Unknown"}")
				.Map(dest => dest.VolumeMl, src => src.VolumeMl)
				.Map(dest => dest.ConcentrationName, src => src.Concentration.Name)
				.Map(dest => dest.BasePrice, src => src.BasePrice)
				.Map(dest => dest.PrimaryImageUrl, src => src.Media.Where(m => m.IsPrimary && !m.IsDeleted).Select(m => m.Url).FirstOrDefault());

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



