using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class AddressRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Address, AddressResponse>();

			config.NewConfig<CreateAddressRequest, Address>()
				.Ignore(dest => dest.Id)
				.Ignore(dest => dest.UserId)
				.Ignore(dest => dest.IsDefault)
				.Ignore(dest => dest.CreatedAt)
				.Ignore(dest => dest.UpdatedAt)
				.Ignore(dest => dest.User);

			config.NewConfig<UpdateAddressRequest, Address>()
				.Ignore(dest => dest.Id)
				.Ignore(dest => dest.UserId)
				.Ignore(dest => dest.IsDefault)
				.Ignore(dest => dest.CreatedAt)
				.Ignore(dest => dest.UpdatedAt)
				.Ignore(dest => dest.User);
		}
	}
}
