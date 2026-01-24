using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class VoucherRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CreateVoucherRequest, Voucher>();
			config.NewConfig<UpdateVoucherRequest, Voucher>();
			config.NewConfig<Voucher, VoucherResponse>();
		}
	}
}
