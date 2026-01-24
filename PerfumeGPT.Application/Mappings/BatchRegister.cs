using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class BatchRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Batch, BatchResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.BatchCode, src => src.BatchCode)
				.Map(dest => dest.ManufactureDate, src => src.ManufactureDate)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.ImportQuantity, src => src.ImportQuantity)
				.Map(dest => dest.RemainingQuantity, src => src.RemainingQuantity)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);
		}
	}
}
