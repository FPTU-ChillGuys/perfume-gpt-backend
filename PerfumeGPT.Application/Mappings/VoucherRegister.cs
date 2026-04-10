using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class VoucherRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Voucher, VoucherResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Code, src => src.Code)
				.Map(dest => dest.DiscountValue, src => src.DiscountValue)
				.Map(dest => dest.DiscountType, src => src.DiscountType)
				.Map(dest => dest.CampaignId, src => src.CampaignId)
				.Map(dest => dest.ApplyType, src => src.ApplyType)
				.Map(dest => dest.TargetItemType, src => src.TargetItemType)
				.Map(dest => dest.RequiredPoints, src => src.RequiredPoints)
              .Map(dest => dest.MaxDiscountAmount, src => src.MaxDiscountAmount)
				.Map(dest => dest.MinOrderValue, src => src.MinOrderValue)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.IsExpired, src => src.ExpiryDate < DateTime.UtcNow)
				.Map(dest => dest.TotalQuantity, src => src.TotalQuantity)
				.Map(dest => dest.RemainingQuantity, src => src.RemainingQuantity)
                .Map(dest => dest.MaxUsagePerUser, src => src.MaxUsagePerUser)
				.Map(dest => dest.IsPublic, src => src.IsPublic)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);

			config.NewConfig<UserVoucher, UserVoucherResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VoucherId, src => src.VoucherId)
				.Map(dest => dest.Code, src => src.Voucher.Code)
				.Map(dest => dest.DiscountValue, src => src.Voucher.DiscountValue)
				.Map(dest => dest.DiscountType, src => src.Voucher.DiscountType.ToString())
				.Map(dest => dest.MinOrderValue, src => src.Voucher.MinOrderValue)
				.Map(dest => dest.ExpiryDate, src => src.Voucher.ExpiryDate)
				.Map(dest => dest.IsUsed, src => src.IsUsed)
				.Map(dest => dest.Status, src => src.Status.ToString())
				.Map(dest => dest.IsExpired, src => src.Voucher.ExpiryDate < DateTime.UtcNow)
				.Map(dest => dest.RedeemedAt, src => src.CreatedAt);

			config.NewConfig<Voucher, RedeemableVoucherResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Code, src => src.Code)
				.Map(dest => dest.DiscountValue, src => src.DiscountValue)
				.Map(dest => dest.DiscountType, src => src.DiscountType.ToString())
				.Map(dest => dest.RequiredPoints, src => src.RequiredPoints)
              .Map(dest => dest.MaxDiscountAmount, src => src.MaxDiscountAmount)
				.Map(dest => dest.MinOrderValue, src => src.MinOrderValue)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.IsExpired, src => src.ExpiryDate < DateTime.UtcNow)
				.Map(dest => dest.RemainingQuantity, src => src.RemainingQuantity)
             .Map(dest => dest.MaxUsagePerUser, src => src.MaxUsagePerUser)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);
		}
	}
}
