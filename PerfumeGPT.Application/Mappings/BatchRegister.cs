using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class BatchRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// for Inventory queries
			config.NewConfig<Batch, BatchDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VariantId, src => src.VariantId)
				.Map(dest => dest.VariantSku, src => src.ProductVariant.Sku ?? "")
				.Map(dest => dest.ProductName, src => src.ProductVariant.Product.Name ?? "")
				.Map(dest => dest.VolumeMl, src => src.ProductVariant.VolumeMl)
				.Map(dest => dest.ConcentrationName, src => src.ProductVariant.Concentration.Name ?? "")
				.Map(dest => dest.BatchCode, src => src.BatchCode)
				.Map(dest => dest.ManufactureDate, src => src.ManufactureDate)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.ImportQuantity, src => src.ImportQuantity)
				.Map(dest => dest.RemainingQuantity, src => src.RemainingQuantity)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);

			// for Import ticket responses
			config.NewConfig<Batch, BatchResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.BatchCode, src => src.BatchCode)
				.Map(dest => dest.ManufactureDate, src => src.ManufactureDate)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.ImportQuantity, src => src.ImportQuantity)
				.Map(dest => dest.RemainingQuantity, src => src.RemainingQuantity)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);

			config.NewConfig<CreateBatchRequest, Batch>()
				.Map(dest => dest.BatchCode, src => src.BatchCode)
				.Map(dest => dest.ManufactureDate, src => src.ManufactureDate)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.ImportQuantity, src => src.Quantity)
				.Map(dest => dest.RemainingQuantity, src => src.Quantity);
		}
	}
}
