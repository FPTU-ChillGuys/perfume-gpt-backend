using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderShippingHelper : IOrderShippingHelper
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IContactAddressService _contactAddressService;
		private readonly IGHNService _ghnService;

		public OrderShippingHelper(
			IUnitOfWork unitOfWork,
			IGHNService ghnService,
			IContactAddressService contactAddressService)
		{
			_unitOfWork = unitOfWork;
			_ghnService = ghnService;
			_contactAddressService = contactAddressService;
		}

		public async Task SetupShippingInfoAsync(Order order, ContactAddressInformation? contactAddressRequest, Guid? customerId, Guid? savedAddressId)
		{
			// 1. Create contact address
			var contactAddress = await _contactAddressService.CreateContactAddressAsync(contactAddressRequest, savedAddressId, customerId);
			order.AttachContactAddress(contactAddress.Id);
			// 2. Get lead time
			var EstimatedDeliveryDate = await GetLeadTimeAsync(contactAddress.DistrictId, contactAddress.WardCode);

			// 3. Create shipping info
			var shippingInfo = ShippingInfo.Create(CarrierName.GHN, ShippingType.Forward, 0, EstimatedDeliveryDate);

			await _unitOfWork.ShippingInfos.AddAsync(shippingInfo);

			order.AttachForwardShipping(shippingInfo.Id);
			order.ForwardShipping = shippingInfo;
		}

		public async Task<DateTime?> GetLeadTimeAsync(int districtId, string wardCode)
		{
			var leadTimeRequest = new GetLeadTimeRequest
			{
				ToDistrictId = districtId,
				ToWardCode = wardCode,
				ServiceId = 2 // lightweight service
			};

			var leadTimeResponse = await _ghnService.GetLeadTimeAsync(leadTimeRequest);
			if (leadTimeResponse?.Data == null)
			{
				return null;
			}

			if (leadTimeResponse.Data.LeadTimeOrder != null)
			{
				var estimateDate = leadTimeResponse.Data.LeadTimeOrder.ToEstimateDate;

				return estimateDate > DateTime.UtcNow ? estimateDate : null;
			}

			if (leadTimeResponse.Data.LeadTime > 0)
			{
				var estimateDate = DateTimeOffset.FromUnixTimeSeconds(leadTimeResponse.Data.LeadTime).UtcDateTime;

				return estimateDate > DateTime.UtcNow ? estimateDate : null;
			}

			return null;
		}

		public ShippingStatus? MapOrderStatusToShippingStatus(OrderStatus orderStatus)
		{
			return orderStatus switch
			{
				OrderStatus.Processing => ShippingStatus.Pending,
				OrderStatus.Delivering => ShippingStatus.Delivering,
				OrderStatus.Delivered => ShippingStatus.Delivered,
				OrderStatus.Cancelled => ShippingStatus.Cancelled,
				OrderStatus.Returning => ShippingStatus.Returning,
				OrderStatus.Returned => ShippingStatus.Returned,
				_ => null
			};
		}

		public async Task<bool> CreateGHNShippingOrderAsync(Order order, ContactAddress contactAddress)
		{
			var shippingInfo = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(order.Id)
				  ?? throw AppException.NotFound("Shipping info not found.");

			bool isPaid = order.PaymentStatus == PaymentStatus.Paid;
			var codAmount = isPaid ? 0m : order.TotalAmount;

			return await CreateGHNShippingOrderInternalAsync(
				order.Id,
				codAmount,
				shippingInfo,
				contactAddress);
		}

		public async Task<bool> CreateGHNShippingOrderAsync(OrderReturnRequest returnRequest, ContactAddress contactAddress)
		{
			if (!returnRequest.ReturnShippingId.HasValue)
				throw AppException.BadRequest("Return shipping is not attached to the return request.");

			var shippingInfo = await _unitOfWork.ShippingInfos.GetByIdAsync(returnRequest.ReturnShippingId.Value)
				?? throw AppException.NotFound("Return shipping info not found.");

			return await CreateGHNShippingOrderInternalAsync(
				returnRequest.OrderId,
				0,
				shippingInfo,
			 contactAddress);
		}

		private async Task<bool> CreateGHNShippingOrderInternalAsync(
			Guid orderId,
			decimal codAmount,
			ShippingInfo shippingInfo,
		   ContactAddress contactAddress)
		{
			var orderWithDetails = await _unitOfWork.Orders.GetOrderWithDetailsForShippingAsync(orderId);
			if (orderWithDetails?.OrderDetails == null || orderWithDetails.OrderDetails.Count == 0)
				throw AppException.NotFound("Order details not found.");

			// Calculate total weight and dimensions from order items
			int totalWeight = 0;
			int maxLength = 0;
			int maxWidth = 0;
			int totalHeight = 0;

			var ghnItems = new List<ShippingOrderItem>();
			var contentBuilder = new List<string>();

			// For service_type_id = 2 (lightweight), we use aggregate dimensions
			// Assuming each item weighs approximately 100g and has standard perfume dimensions
			foreach (var detail in orderWithDetails.OrderDetails)
			{
				totalWeight += detail.Quantity * detail.ProductVariant.VolumeMl; //  per item (adjust as needed)
				maxLength = Math.Max(maxLength, 15); // 15cm standard perfume box length
				maxWidth = Math.Max(maxWidth, 10); // 10cm standard width
				totalHeight += detail.Quantity * 10; // 10cm per item stacked

				string itemName = ExtractProductNameFromSnapshot(detail.Snapshot) ?? "Nước hoa cao cấp";
				contentBuilder.Add($"{itemName} (x{detail.Quantity})");

				ghnItems.Add(new ShippingOrderItem
				{
					Name = itemName,
					Code = detail.ProductVariant.Barcode,
					Quantity = detail.Quantity,
					Price = (int)detail.UnitPrice,
					Weight = detail.ProductVariant.VolumeMl,
					Length = 15,
					Width = 10,
					Height = 10,
					Category = new ShippingOrderItemCategory { Level1 = "Mỹ phẩm" }
				});
			}
			bool isReturn = shippingInfo.Type == ShippingType.Return;

			string generatedContent = isReturn ? "Hoàn trả: " : "Giao hàng: ";
			generatedContent += string.Join(", ", contentBuilder);
			if (generatedContent.Length > 200)
			{
				generatedContent = generatedContent.Substring(0, 197) + "...";
			}

			string clientOrderCode = isReturn ? $"RET-{orderWithDetails.Code}" : orderWithDetails.Code;

			var primaryStore = isReturn ? await _ghnService.GetPrimaryStoreAsync() : null;

			string storeName = !string.IsNullOrWhiteSpace(primaryStore?.Name) ? primaryStore.Name : "Perfume Store";
			string storePhone = !string.IsNullOrWhiteSpace(primaryStore?.Phone) ? primaryStore.Phone : contactAddress.ContactPhoneNumber;
			string storeAddress = !string.IsNullOrWhiteSpace(primaryStore?.Address) ? primaryStore.Address : "Store address not found";
			string storeWard = "Phường Long Thạnh Mỹ";
			string storeDistrict = "Thủ Đức";
			string storeProvince = "Hồ Chí Minh";

			// Create GHN shipping order request
			var ghnRequest = new CreateShippingOrderRequest
			{
				FromName = isReturn ? contactAddress.ContactName : null,
				FromPhone = isReturn ? contactAddress.ContactPhoneNumber : null,
				FromAddress = isReturn ? contactAddress.FullAddress : null,
				FromWardName = isReturn ? contactAddress.WardName : null,
				FromDistrictName = isReturn ? contactAddress.DistrictName : null,
				FromProvinceName = isReturn ? contactAddress.ProvinceName : null,

				ToName = isReturn ? storeName : contactAddress.ContactName,
				ToPhone = isReturn ? storePhone : contactAddress.ContactPhoneNumber,
				ToAddress = isReturn ? storeAddress : contactAddress.FullAddress,
				ToWardName = isReturn ? storeWard : contactAddress.WardName,
				ToDistrictName = isReturn ? storeDistrict : contactAddress.DistrictName,
				ToProvinceName = isReturn ? storeProvince : contactAddress.ProvinceName,

				ClientOrderCode = clientOrderCode,
				CodAmount = (int)codAmount,
				Content = generatedContent,
				Items = ghnItems,
				Weight = totalWeight,
				Length = maxLength,
				Width = maxWidth,
				Height = totalHeight,
				ServiceTypeId = 2,
				PaymentTypeId = isReturn ? 2 : 1,
				RequiredNote = "KHONGCHOXEMHANG",
				InsuranceValue = (int)Math.Min(orderWithDetails.TotalAmount, 5000000),
			};

			// Call GHN API to create shipping order
			var ghnResponse = await _ghnService.CreateShippingOrderAsync(ghnRequest)
				?? throw AppException.Internal("Failed to create GHN shipping order.");

			// Update shipping info with tracking number
			shippingInfo.SetTrackingNumber(ghnResponse.OrderCode);
			_unitOfWork.ShippingInfos.Update(shippingInfo);

			return await _unitOfWork.SaveChangesAsync();
		}

		private static string? ExtractProductNameFromSnapshot(string snapshotJson)
		{
			if (string.IsNullOrWhiteSpace(snapshotJson)) return null;

			try
			{
				using var document = System.Text.Json.JsonDocument.Parse(snapshotJson);
				if (document.RootElement.TryGetProperty("Sku", out var nameProp))
					return nameProp.GetString();
			}
			catch
			{
			}

			return null;
		}
	}
}
