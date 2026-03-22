using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class AddressRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Address, AddressResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.RecipientName, src => src.RecipientName)
				.Map(dest => dest.RecipientPhoneNumber, src => src.RecipientPhoneNumber)
				.Map(dest => dest.Street, src => src.Street)
				.Map(dest => dest.Ward, src => src.Ward)
				.Map(dest => dest.District, src => src.District)
				.Map(dest => dest.City, src => src.City)
				.Map(dest => dest.DistrictId, src => src.DistrictId)
				.Map(dest => dest.ProvinceId, src => src.ProvinceId)
				.Map(dest => dest.WardCode, src => src.WardCode)
				.Map(dest => dest.IsDefault, src => src.IsDefault);
		}
	}
}
