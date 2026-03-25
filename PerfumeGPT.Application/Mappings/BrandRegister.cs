using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Brands;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class BrandRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Brand, BrandResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name);
		}
	}
}
