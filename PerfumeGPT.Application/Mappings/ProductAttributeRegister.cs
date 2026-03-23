using Mapster;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Domain.Entities;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Application.Mappings
{
	public class ProductAttributeRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// Attribute mappings
			config.NewConfig<Attribute, AttributeLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.InternalCode, src => src.InternalCode.ToUpper())
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.IsVariantLevel, src => src.IsVariantLevel);

			// ProductAttribute mappings
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
