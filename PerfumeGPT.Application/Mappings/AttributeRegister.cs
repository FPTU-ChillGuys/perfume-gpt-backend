using Mapster;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;

namespace PerfumeGPT.Application.Mappings
{
	public class AttributeRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Domain.Entities.Attribute, AttributeLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.IsVariantLevel, src => src.IsVariantLevel);
		}
	}
}
