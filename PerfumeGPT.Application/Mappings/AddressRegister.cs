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

			config.NewConfig<CreateAddressRequest, Address>();

			config.NewConfig<UpdateAddressRequest, Address>();
		}
	}
}
