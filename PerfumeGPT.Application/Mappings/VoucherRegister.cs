using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Mappings
{
	public class VoucherRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Voucher, VoucherResponse>()
				.Map(dest => dest.IsExpired, src => src.ExpiryDate < DateTime.UtcNow);

			config.NewConfig<UserVoucher, UserVoucherResponse>()
				.Map(dest => dest.IsUsed, src => src.Status == UsageStatus.Used)
				.Map(dest => dest.IsExpired, src => src.Voucher.ExpiryDate < DateTime.UtcNow);

			config.NewConfig<Voucher, RedeemableVoucherResponse>()
				.Map(dest => dest.IsExpired, src => src.ExpiryDate < DateTime.UtcNow);
		}
	}
}
