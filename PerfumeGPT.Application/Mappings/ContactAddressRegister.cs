using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ContactAddressRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<ContactAddress, RecipientInfoResponse>()
				.Map(dest => dest.RecipientPhoneNumber, src => src.ContactPhoneNumber)
				.Map(dest => dest.RecipientName, src => src.ContactName);

			config.NewConfig<Address, ContactAddressInformation>()
				.Map(dest => dest.ContactPhoneNumber, src => src.RecipientPhoneNumber)
				.Map(dest => dest.ContactName, src => src.RecipientName)
				.Map(dest => dest.DistrictId, src => src.DistrictId)
				.Map(dest => dest.DistrictName, src => src.District)
				.Map(dest => dest.WardCode, src => src.WardCode)
				.Map(dest => dest.WardName, src => src.Ward)
				.Map(dest => dest.ProvinceId, src => src.ProvinceId)
				.Map(dest => dest.ProvinceName, src => src.City)
				.Map(dest => dest.FullAddress, src => src.Street + ", " + src.Ward + ", " + src.District + ", " + src.City);

			config.NewConfig<AddressResponse, ContactAddressInformation>()
				.Map(dest => dest.ContactPhoneNumber, src => src.RecipientPhoneNumber)
				.Map(dest => dest.ContactName, src => src.RecipientName)
				.Map(dest => dest.DistrictId, src => src.DistrictId)
				.Map(dest => dest.DistrictName, src => src.District)
				.Map(dest => dest.WardCode, src => src.WardCode)
				.Map(dest => dest.WardName, src => src.Ward)
				.Map(dest => dest.ProvinceId, src => src.ProvinceId)
				.Map(dest => dest.ProvinceName, src => src.City)
				.Map(dest => dest.FullAddress, src => src.Street + ", " + src.Ward + ", " + src.District + ", " + src.City);
		}
	}
}
