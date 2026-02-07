using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class RecipientInfoRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<RecipientInfo, RecipientInfoResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.FullName, src => src.FullName)
				.Map(dest => dest.Phone, src => src.Phone)
				.Map(dest => dest.DistrictName, src => src.DistrictName)
				.Map(dest => dest.WardName, src => src.WardName)
				.Map(dest => dest.ProvinceName, src => src.ProvinceName)
				.Map(dest => dest.FullAddress, src => src.FullAddress);
		}
	}
}
