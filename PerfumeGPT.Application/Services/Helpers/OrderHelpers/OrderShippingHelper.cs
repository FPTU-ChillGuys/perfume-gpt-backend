using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
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
		private readonly IAddressService _addressService;
		private readonly IShippingService _shippingService;
		private readonly IGHNService _ghnService;

		public OrderShippingHelper(
			IUnitOfWork unitOfWork,
			IAddressService addressService,
			IShippingService shippingService,
			IGHNService ghnService)
		{
			_unitOfWork = unitOfWork;
			_addressService = addressService;
			_shippingService = shippingService;
			_ghnService = ghnService;
		}

		public async Task<BaseResponse<decimal>> SetupShippingInfoAsync(
			Guid orderId,
			RecipientInformation? recipientRequest,
			Guid? customerId,
			decimal? preCalculatedShippingFee = null,
			Order? orderToUpdate = null)
		{
			RecipientInfo recipientInfo;

			// Resolve recipient information
			if (recipientRequest == null)
			{
				if (!customerId.HasValue)
				{
					return BaseResponse<decimal>.Fail(
						"Either recipient information or customer ID must be provided.",
						ResponseErrorType.BadRequest);
				}

				var customerAddress = await _addressService.GetDefaultAddressAsync(customerId.Value);
				if (customerAddress == null || !customerAddress.Success || customerAddress.Payload == null)
				{
					return BaseResponse<decimal>.Fail(
						"Customer default address not found. Please provide recipient information.",
						ResponseErrorType.BadRequest);
				}

				recipientInfo = new RecipientInfo
				{
					OrderId = orderId,
					FullName = customerAddress.Payload.ReceiverName,
					Phone = customerAddress.Payload.Phone,
					DistrictName = customerAddress.Payload.District,
					DistrictId = customerAddress.Payload.DistrictId,
					ProvinceName = customerAddress.Payload.City,
					WardCode = customerAddress.Payload.WardCode,
					WardName = customerAddress.Payload.Ward,
					FullAddress = $"{customerAddress.Payload.Street}, {customerAddress.Payload.Ward}, {customerAddress.Payload.District}, {customerAddress.Payload.City}"
				};
			}
			else
			{
				recipientInfo = new RecipientInfo
				{
					OrderId = orderId,
					FullName = recipientRequest.FullName,
					Phone = recipientRequest.Phone,
					DistrictId = recipientRequest.DistrictId,
					DistrictName = recipientRequest.DistrictName,
					WardCode = recipientRequest.WardCode,
					WardName = recipientRequest.WardName,
					ProvinceName = recipientRequest.ProvinceName,
					FullAddress = $"{recipientRequest.WardName}, {recipientRequest.DistrictName}, {recipientRequest.ProvinceName}"
				};
			}

			await _unitOfWork.RecipientInfos.AddAsync(recipientInfo);

			// Calculate or use pre-calculated shipping fee
			decimal shippingFee;
			if (preCalculatedShippingFee.HasValue)
			{
				shippingFee = preCalculatedShippingFee.Value;
			}
			else
			{
				// Calculate shipping fee using ShippingService
				var calculatedFee = await _shippingService.CalculateShippingFeeAsync(
					recipientInfo.DistrictId,
					recipientInfo.WardCode);

				if (calculatedFee == null)
				{
					return BaseResponse<decimal>.Fail("Failed to calculate shipping fee.", ResponseErrorType.InternalError);
				}

				shippingFee = calculatedFee.Value;
			}

			// Get leadtime from GHN
			var leadTimeRequest = new GetLeadTimeRequest
			{
				ToDistrictId = recipientInfo.DistrictId,
				ToWardCode = recipientInfo.WardCode,
				ServiceId = 2 // lightweight service
			};

			int? leadTimeDays = null;
			var leadTimeResponse = await _ghnService.GetLeadTimeAsync(leadTimeRequest);
			if (leadTimeResponse?.Data != null)
			{
				// Use LeadTimeOrder if available, otherwise fall back to Unix timestamp
				if (leadTimeResponse.Data.LeadTimeOrder != null)
				{
					var days = (int)Math.Ceiling((leadTimeResponse.Data.LeadTimeOrder.ToEstimateDate - DateTime.UtcNow).TotalDays);
					leadTimeDays = days > 0 ? days : null;
				}
				else if (leadTimeResponse.Data.LeadTime > 0)
				{
					var leadTimeDate = DateTimeOffset.FromUnixTimeSeconds(leadTimeResponse.Data.LeadTime).UtcDateTime;
					var days = (int)Math.Ceiling((leadTimeDate - DateTime.UtcNow).TotalDays);
					leadTimeDays = days > 0 ? days : null;
				}
			}

			// Create shipping info
			var shippingInfo = new ShippingInfo
			{
				OrderId = orderId,
				CarrierName = CarrierName.GHN,
				TrackingNumber = null,
				ShippingFee = shippingFee,
				Status = ShippingStatus.Pending,
				LeadTime = leadTimeDays
			};

			await _unitOfWork.ShippingInfos.AddAsync(shippingInfo);

			// Update order total if order instance provided
			if (orderToUpdate != null)
			{
				var totalAmount = orderToUpdate.TotalAmount + shippingFee;
				orderToUpdate.TotalAmount = totalAmount;
				_unitOfWork.Orders.Update(orderToUpdate);
			}

			// Don't save - let transaction orchestrator handle it
			return BaseResponse<decimal>.Ok(shippingFee);
		}

		public ShippingStatus? MapOrderStatusToShippingStatus(OrderStatus orderStatus)
		{
			return orderStatus switch
			{
				OrderStatus.Processing => ShippingStatus.Pending,
				OrderStatus.Shipped => ShippingStatus.Shipped,
				OrderStatus.Delivered => ShippingStatus.Delivered,
				OrderStatus.Canceled => ShippingStatus.Cancelled,
				OrderStatus.Returned => ShippingStatus.Returned,
				_ => null
			};
		}

		public async Task<BaseResponse<string>> CreateGHNShippingOrderAsync(
			Order order,
			RecipientInfo recipientInfo)
		{
			try
			{
				// Load order details with variant information
				var orderWithDetails = await _unitOfWork.Orders.GetByConditionAsync(o => o.Id == order.Id, o => o.Include(o => o.OrderDetails));
				if (orderWithDetails?.OrderDetails == null || orderWithDetails.OrderDetails.Count == 0)
				{
					return BaseResponse<string>.Fail(
						"Order details not found.",
						ResponseErrorType.NotFound);
				}

				// Calculate total weight and dimensions from order items
				int totalWeight = 0;
				int maxLength = 0;
				int maxWidth = 0;
				int totalHeight = 0;

				// For service_type_id = 2 (lightweight), we use aggregate dimensions
				// Assuming each item weighs approximately 100g and has standard perfume dimensions
				foreach (var detail in orderWithDetails.OrderDetails)
				{
					totalWeight += detail.Quantity * 100; // 100g per item (adjust as needed)
					maxLength = Math.Max(maxLength, 15); // 15cm standard perfume box length
					maxWidth = Math.Max(maxWidth, 10); // 10cm standard width
					totalHeight += detail.Quantity * 10; // 10cm per item stacked
				}

				// Create GHN shipping order request
				var ghnRequest = new DTOs.Requests.GHNs.CreateShippingOrderRequest
				{
					ToName = recipientInfo.FullName,
					ToPhone = recipientInfo.Phone,
					ToAddress = recipientInfo.FullAddress,
					ToWardName = recipientInfo.WardName,
					ToDistrictName = recipientInfo.DistrictName,
					ToProvinceName = recipientInfo.ProvinceName,
					ClientOrderCode = order.Id.ToString(),
					CodAmount = (int)order.TotalAmount,
					Content = "Perfume Order",
					Weight = totalWeight,
					Length = maxLength,
					Width = maxWidth,
					Height = totalHeight,
					ServiceTypeId = 2, // Lightweight service
					PaymentTypeId = 2, // buyer pays shipping fee
					RequiredNote = "KHONGCHOXEMHANG",
					InsuranceValue = (int)Math.Min(order.TotalAmount, 5000000), // Max 5M VND
				};

				// Call GHN API to create shipping order
				var ghnResponse = await _ghnService.CreateShippingOrderAsync(ghnRequest);
				if (ghnResponse == null)
				{
					return BaseResponse<string>.Fail(
						"Failed to create GHN shipping order.",
						ResponseErrorType.InternalError);
				}

				// Update shipping info with tracking number
				var shippingInfo = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(order.Id);
				if (shippingInfo != null)
				{
					shippingInfo.TrackingNumber = ghnResponse.OrderCode;
					_unitOfWork.ShippingInfos.Update(shippingInfo);
				}

				return BaseResponse<string>.Ok(ghnResponse.OrderCode, "GHN shipping order created successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error creating GHN shipping order: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		//private async Task<string> GetWardNameByCodeAsync(string wardCode)
		//{
		//	// This is a placeholder - you may need to implement proper ward lookup
		//	// For now, return the ward code as name
		//	return wardCode;
		//}

		//private async Task<string> GetDistrictNameByIdAsync(int districtId)
		//{
		//	// This is a placeholder - you may need to implement proper district lookup
		//	return districtId.ToString();
		//}

		//private async Task<string> GetProvinceNameByDistrictIdAsync(int districtId)
		//{
		//	// This is a placeholder - you may need to implement proper province lookup
		//	return "HCM"; // Default to Ho Chi Minh City
		//}
	}
}
