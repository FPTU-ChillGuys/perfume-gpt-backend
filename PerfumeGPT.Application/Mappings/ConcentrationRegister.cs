using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Concentrations;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;
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

			config.NewConfig<CreateConcentrationRequest, Concentration>()
				.Map(dest => dest.Name, src => src.Name);

			config.NewConfig<UpdateConcentrationRequest, Concentration>()
				.Map(dest => dest.Name, src => src.Name);
		}
	}
}
