using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class StockRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Stock, StockResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VariantId, src => src.VariantId)
				.Map(dest => dest.VariantSku, src => src.ProductVariant.Sku)
				.Map(dest => dest.ProductName, src => src.ProductVariant.Product.Name)
				.Map(dest => dest.VolumeMl, src => src.ProductVariant.VolumeMl)
				.Map(dest => dest.ConcentrationName, src => src.ProductVariant.Concentration.Name)
				.Map(dest => dest.TotalQuantity, src => src.TotalQuantity)
				.Map(dest => dest.AvailableQuantity, src => src.AvailableQuantity)
				.Map(dest => dest.LowStockThreshold, src => src.LowStockThreshold)
				.Map(dest => dest.IsLowStock, src => src.TotalQuantity <= src.LowStockThreshold);
		}
	}
}
