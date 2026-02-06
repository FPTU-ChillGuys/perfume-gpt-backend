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
			// Order -> OrderResponse
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
				.Map(dest => dest.VoucherId, src => src.VoucherId)
				.Map(dest => dest.VoucherCode, src => src.Voucher != null ? src.Voucher.Code : null)
				.Map(dest => dest.PaymentExpiresAt, src => src.PaymentExpiresAt)
				.Map(dest => dest.PaidAt, src => src.PaidAt)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
				.Map(dest => dest.ShippingInfo, src => src.ShippingInfo)
				.Map(dest => dest.RecipientInfo, src => src.RecipientInfo)
				.Map(dest => dest.OrderDetails, src => src.OrderDetails);

			// Order -> OrderListItem
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
				.Map(dest => dest.ShippingStatus, src => src.ShippingInfo != null ? src.ShippingInfo.Status : (ShippingStatus?)null)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

			// ShippingInfo -> ShippingInfoResponse
			config.NewConfig<ShippingInfo, ShippingInfoResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CarrierName, src => src.CarrierName)
				.Map(dest => dest.TrackingNumber, src => src.TrackingNumber)
				.Map(dest => dest.ShippingFee, src => src.ShippingFee)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.LeadTime, src => src.LeadTime);

			// RecipientInfo -> RecipientInfoResponse
			config.NewConfig<RecipientInfo, RecipientInfoResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.FullName, src => src.FullName)
				.Map(dest => dest.Phone, src => src.Phone)
				.Map(dest => dest.DistrictName, src => src.DistrictName)
				.Map(dest => dest.WardName, src => src.WardName)
				.Map(dest => dest.ProvinceName, src => src.ProvinceName)
				.Map(dest => dest.FullAddress, src => src.FullAddress);

			// OrderDetail -> OrderDetailResponse
			config.NewConfig<OrderDetail, OrderDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VariantId, src => src.VariantId)
				.Map(dest => dest.VariantName, src => src.ProductVariant != null ? $"{src.ProductVariant.Sku} - {src.ProductVariant.VolumeMl}ml" : string.Empty)
				.Map(dest => dest.ImageUrl, src => src.ProductVariant != null && src.ProductVariant.Media.Count > 0
					? src.ProductVariant.Media.FirstOrDefault(m => m.IsPrimary) != null
						? src.ProductVariant.Media.First(m => m.IsPrimary).Url
						: src.ProductVariant.Media.First().Url
					: null)
				.Map(dest => dest.Quantity, src => src.Quantity)
				.Map(dest => dest.UnitPrice, src => src.UnitPrice)
				.Map(dest => dest.Total, src => src.UnitPrice * src.Quantity);
		}
	}
}
