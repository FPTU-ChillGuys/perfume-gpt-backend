using Mapster;
using PerfumeGPT.Application.DTOs.Requests.ImportDetails;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ImportDetailRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<ImportDetail, ImportDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VariantId, src => src.ProductVariantId)
				.Map(dest => dest.VariantName, src => $"{src.ProductVariant.Product.Name ?? "Unknown"} - {src.ProductVariant.VolumeMl}")
				.Map(dest => dest.VariantSku, src => src.ProductVariant != null ? src.ProductVariant.Sku : "Unknown")
				.Map(dest => dest.ExpectedQuantity, src => src.ExpectedQuantity)
				.Map(dest => dest.UnitPrice, src => src.UnitPrice)
				.Map(dest => dest.TotalPrice, src => src.ExpectedQuantity * src.UnitPrice)
				.Map(dest => dest.RejectedQuantity, src => src.RejectedQuantity)
				.Map(dest => dest.Note, src => src.Note)
				.Map(dest => dest.Batches, src => src.Batches);

			config.NewConfig<CreateImportDetailRequest, ImportDetail>()
				.Map(dest => dest.ProductVariantId, src => src.VariantId)
				.Map(dest => dest.ExpectedQuantity, src => src.ExpectedQuantity)
				.Map(dest => dest.UnitPrice, src => src.UnitPrice);
		}
	}
}
