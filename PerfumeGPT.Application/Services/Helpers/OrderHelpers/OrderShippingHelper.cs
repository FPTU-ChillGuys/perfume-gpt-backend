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

		public async Task SetupShippingInfoAsync(Order order, ContactAddressInformation? contactAddressRequest, Guid? customerId, Guid? savedAddressId, decimal? shippingFee = null)
		{
			// 1. Create contact address
			var contactAddress = await _contactAddressService.CreateContactAddressAsync(contactAddressRequest, savedAddressId, customerId);
			order.AttachContactAddress(contactAddress.Id);
			// 2. Get lead time
			var EstimatedDeliveryDate = await GetLeadTimeAsync(contactAddress.DistrictId, contactAddress.WardCode);
			var resolvedShippingFee = shippingFee ?? await CalculateShippingFeeAsync(order, contactAddress);

			// 3. Create shipping info
			var shippingInfo = ShippingInfo.Create(CarrierName.GHN, ShippingType.Forward, resolvedShippingFee, EstimatedDeliveryDate);

			await _unitOfWork.ShippingInfos.AddAsync(shippingInfo);

			order.AttachForwardShipping(shippingInfo.Id);
			order.ForwardShipping = shippingInfo;
		}

		private async Task<decimal> CalculateShippingFeeAsync(Order order, ContactAddress contactAddress)
		{
			if (order.OrderDetails == null || order.OrderDetails.Count == 0)
				return 0m;

			var variantIds = order.OrderDetails.Select(x => x.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);
			var variantById = variants.ToDictionary(x => x.Id, x => x);

			int totalWeight = 0;
			int maxLength = 0;
			int maxWidth = 0;
			int totalHeight = 0;

			var shippingItems = new List<ShippingOrderItem>();
			foreach (var item in order.OrderDetails)
			{
				var variant = variantById.GetValueOrDefault(item.VariantId);
				var itemWeight = Math.Max(1, variant?.VolumeMl ?? 100);

				totalWeight += item.Quantity * itemWeight;
				maxLength = Math.Max(maxLength, 15);
				maxWidth = Math.Max(maxWidth, 10);
				totalHeight += item.Quantity * 10;

				shippingItems.Add(new ShippingOrderItem
				{
					Name = ExtractProductNameFromSnapshot(item.Snapshot) ?? "Nước hoa cao cấp",
					Code = variant?.Barcode,
					Quantity = item.Quantity,
					Price = (int)Math.Round(item.UnitPrice, MidpointRounding.AwayFromZero),
					Length = 15,
					Width = 10,
					Height = 10,
					Weight = itemWeight,
					Category = new ShippingOrderItemCategory { Level1 = "Mỹ phẩm" }
				});
			}

			var shippingFeeRequest = new CalculateShippingFeeRequest
			{
				ToDistrictId = contactAddress.DistrictId,
				ToWardCode = contactAddress.WardCode,
				Length = Math.Max(15, maxLength),
				Width = Math.Max(10, maxWidth),
				Height = Math.Max(10, totalHeight),
				Weight = Math.Max(100, totalWeight),
				Items = shippingItems
			};

			var shippingFeeResponse = await _ghnService.CalculateShippingFeeAsync(shippingFeeRequest);
			return shippingFeeResponse?.Data?.Total ?? 0m;
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

		public async Task<bool> CreateGHNShippingOrderAsync(Order order, ContactAddress contactAddress)
		{
			var shippingInfo = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(order.Id)
				 ?? throw AppException.NotFound("Không tìm thấy thông tin vận chuyển.");

			// Nếu khách đã trả đủ 100% thì shipper không thu đồng nào.
			// Nếu chưa đủ (chưa cọc hoặc mới cọc), shipper thu ĐÚNG phần còn lại.
			var codAmount = order.PaymentStatus == PaymentStatus.Paid ? 0m : order.RemainingAmount;

			return await CreateGHNShippingOrderInternalAsync(order.Id, codAmount, shippingInfo, contactAddress);
		}

		public async Task<bool> CreateGHNShippingOrderAsync(OrderReturnRequest returnRequest, ContactAddress contactAddress)
		{
			if (!returnRequest.ReturnShippingId.HasValue)
				throw AppException.BadRequest("Thông tin vận chuyển hoàn trả chưa được gắn với yêu cầu trả hàng.");

			var shippingInfo = await _unitOfWork.ShippingInfos.GetByIdAsync(returnRequest.ReturnShippingId.Value)
			  ?? throw AppException.NotFound("Không tìm thấy thông tin vận chuyển hoàn trả.");

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
				throw AppException.NotFound("Không tìm thấy chi tiết đơn hàng.");

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
				PaymentTypeId = isReturn ? 1 : 2,
				RequiredNote = "KHONGCHOXEMHANG",
				InsuranceValue = (int)Math.Min(orderWithDetails.TotalAmount, 5000000),
			};

			// Call GHN API to create shipping order
			var ghnResponse = await _ghnService.CreateShippingOrderAsync(ghnRequest)
			 ?? throw AppException.Internal("Tạo đơn vận chuyển GHN thất bại.");

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
