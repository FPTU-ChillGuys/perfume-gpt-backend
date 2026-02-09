using Mapster;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class AttributeValueRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<AttributeValue, AttributeValueLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Value, src => src.Value);
		}
	}
}
