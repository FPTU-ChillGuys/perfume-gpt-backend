using Mapster;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ProductAttributeRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<ProductAttribute, ProductAttributeResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.AttributeId, src => src.AttributeId)
				.Map(dest => dest.ValueId, src => src.ValueId)
				.Map(dest => dest.Attribute, src => src.Attribute.Name)
				.Map(dest => dest.Description, src => src.Attribute.Description)
				.Map(dest => dest.Value, src => src.Value.Value);
		}
	}
}
