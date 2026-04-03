using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Mappings
{
	public class OrderRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Order, OrderResponse>()
				.Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.FullName : null)
				.Map(dest => dest.CustomerEmail, src => src.Customer != null ? src.Customer.Email : null)
				.Map(dest => dest.StaffName, src => src.Staff != null ? src.Staff.FullName : null)
				.Map(dest => dest.VoucherId, src => src.UserVoucher != null ? src.UserVoucher.Voucher.Id.ToString() : null)
				.Map(dest => dest.VoucherCode, src => src.UserVoucher != null ? src.UserVoucher.Voucher.Code : null)
				.Map(dest => dest.ShippingInfo, src => src.ForwardShipping)
				.Map(dest => dest.RecipientInfo, src => src.ContactAddress);

			config.NewConfig<Order, UserOrderResponse>()
				.Map(dest => dest.IsReturnable, src => src.Status == OrderStatus.Delivered && src.ForwardShipping != null && src.ForwardShipping.ShippedDate.HasValue
					? src.ForwardShipping.ShippedDate.Value >= DateTime.UtcNow.AddDays(-7)
					: (bool?)null)
				.Map(dest => dest.VoucherCode, src => src.UserVoucher != null ? src.UserVoucher.Voucher.Code : null)
				.Map(dest => dest.ShippingInfo, src => src.ForwardShipping)
				.Map(dest => dest.RecipientInfo, src => src.ContactAddress);

			config.NewConfig<Order, OrderListItem>()
				.Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.FullName : null)
				.Map(dest => dest.StaffName, src => src.Staff != null ? src.Staff.FullName : null)
				.Map(dest => dest.ItemCount, src => src.OrderDetails.Count)
				.Map(dest => dest.IsReturnalbe, src => src.Status == OrderStatus.Delivered && src.ForwardShipping != null && src.ForwardShipping.ShippedDate.HasValue
					? src.ForwardShipping.ShippedDate.Value >= DateTime.UtcNow.AddDays(-7)
					: (bool?)null)
			   .Map(dest => dest.ShippingStatus, src => src.ForwardShipping != null ? src.ForwardShipping.Status : (ShippingStatus?)null);
		}
	}
}
