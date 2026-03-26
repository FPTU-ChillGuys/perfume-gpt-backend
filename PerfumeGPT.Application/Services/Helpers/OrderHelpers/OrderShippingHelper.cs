using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
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
		private readonly IRecipientService _recipientService;
		private readonly IGHNService _ghnService;

		public OrderShippingHelper(
			IUnitOfWork unitOfWork,
			IGHNService ghnService,
			IRecipientService recipientService)
		{
			_unitOfWork = unitOfWork;
			_ghnService = ghnService;
			_recipientService = recipientService;
		}

		public async Task SetupShippingInfoAsync(Guid orderId, RecipientInformation? recipientRequest, Guid? customerId, Guid? savedAddressId)
		{
			// 1. Create recipient info
			var recipientInfo = await _recipientService.CreateRecipientInfoAsync(orderId, recipientRequest, savedAddressId, customerId);

			// 2. Get lead time
			var leadTimeDays = await GetLeadTimeAsync(recipientInfo.DistrictId, recipientInfo.WardCode);

			// 3. Create shipping info
			var shippingInfo = ShippingInfo.Create(orderId, CarrierName.GHN, 0, leadTimeDays);

			await _unitOfWork.ShippingInfos.AddAsync(shippingInfo);
		}

		private async Task<int?> GetLeadTimeAsync(int districtId, string wardCode)
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

			// Use LeadTimeOrder if available
			if (leadTimeResponse.Data.LeadTimeOrder != null)
			{
				var days = (int)Math.Ceiling(
					(leadTimeResponse.Data.LeadTimeOrder.ToEstimateDate - DateTime.UtcNow).TotalDays);
				return days > 0 ? days : null;
			}

			// Fall back to Unix timestamp
			if (leadTimeResponse.Data.LeadTime > 0)
			{
				var leadTimeDate = DateTimeOffset.FromUnixTimeSeconds(leadTimeResponse.Data.LeadTime).UtcDateTime;
				var days = (int)Math.Ceiling((leadTimeDate - DateTime.UtcNow).TotalDays);
				return days > 0 ? days : null;
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
				var orderWithDetails = await _unitOfWork.Orders.FirstOrDefaultAsync(o => o.Id == order.Id, o => o.Include(o => o.OrderDetails));
				if (orderWithDetails?.OrderDetails == null || orderWithDetails.OrderDetails.Count == 0)
					throw AppException.NotFound("Order details not found.");

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
				var ghnRequest = new CreateShippingOrderRequest
				{
					ToName = recipientInfo.RecipientName,
					ToPhone = recipientInfo.RecipientPhoneNumber,
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
					throw AppException.Internal("Failed to create GHN shipping order.");

				// Update shipping info with tracking number
				var shippingInfo = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(order.Id);
				if (shippingInfo != null)
				{
					shippingInfo.SetTrackingNumber(ghnResponse.OrderCode);
					_unitOfWork.ShippingInfos.Update(shippingInfo);
				}

				return BaseResponse<string>.Ok(ghnResponse.OrderCode, "GHN shipping order created successfully.");
			}
			catch (AppException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw AppException.Internal($"Error creating GHN shipping order: {ex.Message}");
			}
		}
	}
}
