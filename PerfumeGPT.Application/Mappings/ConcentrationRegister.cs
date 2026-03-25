using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Concentrations;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ConcentrationRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Concentration, ConcentrationResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name);
		}
	}
}
