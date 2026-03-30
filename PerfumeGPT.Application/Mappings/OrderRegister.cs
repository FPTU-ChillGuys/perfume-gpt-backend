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
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CustomerId, src => src.CustomerId)
				.Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.FullName : null)
				.Map(dest => dest.CustomerEmail, src => src.Customer != null ? src.Customer.Email : null)
				.Map(dest => dest.StaffId, src => src.StaffId)
				.Map(dest => dest.StaffName, src => src.Staff != null ? src.Staff.FullName : null)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.PaymentStatus, src => src.PaymentStatus)
				.Map(dest => dest.TotalAmount, src => src.TotalAmount)
				.Map(dest => dest.VoucherId, src => src.UserVoucher != null ? src.UserVoucher.Voucher.Id.ToString() : null)
				.Map(dest => dest.VoucherCode, src => src.UserVoucher != null ? src.UserVoucher.Voucher.Code : null)
				.Map(dest => dest.PaymentExpiresAt, src => src.PaymentExpiresAt)
				.Map(dest => dest.PaidAt, src => src.PaidAt)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
				.Map(dest => dest.PaymentTransactions, src => src.PaymentTransactions)
				.Map(dest => dest.ShippingInfo, src => src.ShippingInfo)
				.Map(dest => dest.RecipientInfo, src => src.RecipientInfo)
				.Map(dest => dest.OrderDetails, src => src.OrderDetails);

			config.NewConfig<Order, UserOrderResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.IsReturnable, src => src.Status == OrderStatus.Delivered && src.ShippingInfo != null && src.ShippingInfo.ShippedDate.HasValue
					? src.ShippingInfo.ShippedDate.Value >= DateTime.UtcNow.AddDays(-7)
					: (bool?)null)
				.Map(dest => dest.PaymentStatus, src => src.PaymentStatus)
				.Map(dest => dest.TotalAmount, src => src.TotalAmount)
				.Map(dest => dest.VoucherCode, src => src.UserVoucher != null ? src.UserVoucher.Voucher.Code : null)
				.Map(dest => dest.PaymentExpiresAt, src => src.PaymentExpiresAt)
				.Map(dest => dest.PaidAt, src => src.PaidAt)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
				.Map(dest => dest.PaymentTransactions, src => src.PaymentTransactions)
				.Map(dest => dest.ShippingInfo, src => src.ShippingInfo)
				.Map(dest => dest.RecipientInfo, src => src.RecipientInfo)
				.Map(dest => dest.OrderDetails, src => src.OrderDetails);

			config.NewConfig<Order, OrderListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CustomerId, src => src.CustomerId)
				.Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.FullName : null)
				.Map(dest => dest.StaffId, src => src.StaffId)
				.Map(dest => dest.StaffName, src => src.Staff != null ? src.Staff.FullName : null)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.PaymentStatus, src => src.PaymentStatus)
				.Map(dest => dest.TotalAmount, src => src.TotalAmount)
				.Map(dest => dest.ItemCount, src => src.OrderDetails.Count)
				.Map(dest => dest.IsReturnalbe, src => src.Status == OrderStatus.Delivered && src.ShippingInfo != null && src.ShippingInfo.ShippedDate.HasValue
					? src.ShippingInfo.ShippedDate.Value >= DateTime.UtcNow.AddDays(-7)
					: (bool?)null)
				.Map(dest => dest.ShippingStatus, src => src.ShippingInfo != null ? src.ShippingInfo.Status : (ShippingStatus?)null)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt);
		}
	}
}
