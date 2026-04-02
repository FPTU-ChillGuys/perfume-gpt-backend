using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class BatchRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Batch, BatchDetailResponse>()
				.Inherits<Batch, BatchResponse>()
				.Map(dest => dest.VariantId, src => src.VariantId)
				.Map(dest => dest.VariantSku, src => src.ProductVariant.Sku)
				.Map(dest => dest.ProductName, src => src.ProductVariant.Product.Name)
				.Map(dest => dest.VolumeMl, src => src.ProductVariant.VolumeMl)
				.Map(dest => dest.ConcentrationName, src => src.ProductVariant.Concentration.Name);

			config.NewConfig<Batch, BatchLookupResponse>()
				.Map(dest => dest.Sku, src => src.ProductVariant.Sku ?? "");
		}
	}
}
