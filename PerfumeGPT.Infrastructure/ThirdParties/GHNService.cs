using Microsoft.Extensions.Configuration;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Address.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.DTOs.Responses.GHNs;
using PerfumeGPT.Application.DTOs.Responses.GHNs.Base;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Net.Http.Json;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class GHNService : IGHNService
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;

		public GHNService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_configuration = configuration;
		}

		public async Task<CalculateFeeResponse?> CalculateShippingFeeAsync(CalculateShippingFeeRequest request)
		{
			var token = _configuration["GHN:Token"];
			var shopId = _configuration["GHN:ShopId"];
			var calculateFeeUrl = _configuration["GHN:CalculateFeeUrl"];

			var requestBody = new
			{
				service_type_id = 2, // Lightning Service
				to_district_id = request.ToDistrictId,
				to_ward_code = request.ToWardCode.ToString(),
				length = request.Length,
				width = request.Width,
				height = request.Height,
				weight = request.Weight,
				insurance_value = 0,
				items = request.Items?.Select(item => new
				{
					name = item.Name,
					code = item.Code,
					quantity = item.Quantity,
					price = item.Price,
					length = item.Length,
					width = item.Width,
					height = item.Height,
					weight = item.Weight,
					category = item.Category != null ? new
					{
						level1 = item.Category.Level1
					} : null
				}).ToList()
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, calculateFeeUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Headers.Add("ShopId", shopId);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
			}
			else
				response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<CalculateFeeResponse>();
			return result;
		}

		public async Task<BaseResponse<List<DistrictResponse>>> GetDistrictsByProvinceIdAsync(int provinceId)
		{
			var token = _configuration["GHN:Token"];
			var getDistrictUrl = _configuration["GHN:GetDistrictUrl"];

			var requestBody = new
			{
				province_id = provinceId
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, getDistrictUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				throw new HttpRequestException($"Failed to get districts: {errorContent}");
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<List<DistrictResponse>>>();
			return BaseResponse<List<DistrictResponse>>.Ok(result?.Data ?? [], "Districts retrieved successfully");
		}

		public async Task<bool> UpdateOrderCodAsync(UpdateCodRequest request)
		{
			var token = _configuration["GHN:Token"];
			var updateCodUrl = _configuration["GHN:UpdateCodUrl"];

			var requestBody = new
			{
				order_code = request.OrderCode,
				cod_amount = request.CodAmount
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, updateCodUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				return false;
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<object>>();
			return result != null && result.Code == 200;
		}

		public async Task<BaseResponse<List<ProvinceResponse>>> GetProvincesAsync()
		{
			var token = _configuration["GHN:Token"];
			var getProvinceUrl = _configuration["GHN:GetProvincesUrl"];

			using var requestMessage = new HttpRequestMessage(HttpMethod.Get, getProvinceUrl);
			requestMessage.Headers.Add("Token", token);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				throw new HttpRequestException($"Failed to get provinces: {errorContent}");
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<List<ProvinceResponse>>>();
			return BaseResponse<List<ProvinceResponse>>.Ok(result?.Data ?? [], "Provinces retrieved successfully");
		}

		public async Task<BaseResponse<List<WardResponse>>> GetWardsByDistrictIdAsync(int districtId)
		{
			var token = _configuration["GHN:Token"];
			var getWardUrl = _configuration["GHN:GetWardUrl"];

			var requestBody = new
			{
				district_id = districtId
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, getWardUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				throw new HttpRequestException($"Failed to get wards: {errorContent}");
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<List<WardResponse>>>();
			return BaseResponse<List<WardResponse>>.Ok(result?.Data ?? [], "Wards retrieved successfully");
		}

		public async Task<CreateShippingOrderResponse?> CreateShippingOrderAsync(CreateShippingOrderRequest request)
		{
			var token = _configuration["GHN:Token"];
			var shopId = _configuration["GHN:ShopId"];
			var createOrderUrl = _configuration["GHN:CreateOrderUrl"];

			var requestBody = new
			{
				from_name = request.FromName,
				from_phone = request.FromPhone,
				from_address = request.FromAddress,
				from_ward_name = request.FromWardName,
				from_district_name = request.FromDistrictName,
				from_province_name = request.FromProvinceName,
				to_name = request.ToName,
				to_phone = request.ToPhone,
				to_address = request.ToAddress,
				to_ward_name = request.ToWardName,
				to_district_name = request.ToDistrictName,
				to_province_name = request.ToProvinceName,
				return_phone = request.ReturnPhone,
				return_address = request.ReturnAddress,
				return_district_name = request.ReturnDistrictName,
				return_ward_name = request.ReturnWardName,
				return_province_name = request.ReturnProvinceName,
				client_order_code = request.ClientOrderCode,
				cod_amount = request.CodAmount,
				content = request.Content,
				weight = request.Weight,
				length = request.Length,
				width = request.Width,
				height = request.Height,
				pick_station_id = request.PickStationId,
				insurance_value = request.InsuranceValue,
				coupon = request.Coupon,
				service_type_id = request.ServiceTypeId,
				payment_type_id = request.PaymentTypeId,
				note = request.Note,
				required_note = request.RequiredNote,
				pick_shift = request.PickShift,
				pickup_time = request.PickupTime,
				items = request.Items?.Select(item => new
				{
					name = item.Name,
					code = item.Code,
					quantity = item.Quantity,
					price = item.Price,
					length = item.Length,
					width = item.Width,
					height = item.Height,
					weight = item.Weight,
					category = item.Category != null ? new
					{
						level1 = item.Category.Level1
					} : null
				}).ToList(),
				cod_failed_amount = request.CodFailedAmount
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, createOrderUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Headers.Add("ShopId", shopId);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				throw new HttpRequestException($"Failed to create shipping order: {errorContent}");
			}

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
			};

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<CreateShippingOrderResponse>>(options);
			return result?.Data;
		}

		public async Task<bool> UpdateOrderAsync(UpdateOrderRequest request)
		{
			var token = _configuration["GHN:Token"];
			var shopId = _configuration["GHN:ShopId"];
			var updateUrl = _configuration["GHN:UpdateOrderUrl"];

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, updateUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Headers.Add("ShopId", shopId);
			requestMessage.Content = JsonContent.Create(request);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"GHN update order failed: {error}");
				return false;
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<object>>();
			return result != null && result.Code == 200;
		}

		public async Task<GetLeadTimeResponse?> GetLeadTimeAsync(GetLeadTimeRequest request)
		{
			var token = _configuration["GHN:Token"];
			var shopId = _configuration["GHN:ShopId"];
			var leadTimeUrl = _configuration["GHN:GetLeadTimeUrl"];

			var requestBody = new
			{
				to_district_id = request.ToDistrictId,
				to_ward_code = request.ToWardCode,
				service_id = request.ServiceId
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, leadTimeUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Headers.Add("ShopId", shopId);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				return null;
			}

			var result = await response.Content.ReadFromJsonAsync<GetLeadTimeResponse>();
			return result;
		}

		public async Task<ShippingOrderDetailDto?> GetOrderDetailAsync(string orderCode)
		{
			var token = _configuration["GHN:Token"];
			var detailUrl = _configuration["GHN:GetOrderDetailUrl"];

			var requestBody = new { order_code = orderCode };

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, detailUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				return null;
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<ShippingOrderDetailDto>>();
			return result?.Data;
		}

		public async Task<GhnStoreDto?> GetPrimaryStoreAsync()
		{
			var token = _configuration["GHN:Token"];
			var getStoreUrl = _configuration["GHN:GetStoreUrl"];

			var requestBody = new
			{
				offset = 0,
				limit = 1,
				client_phone = ""
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, getStoreUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				return null;
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<GetStoresResponse>>();
			return result?.Data?.Shops?.FirstOrDefault();
		}

		public async Task<BaseResponse<string>> GetOrderInfoUrlAsync(GetOrderInfoRequest request)
		{
			if (request.TrackingNumbers.Count == 0)
			{
				return BaseResponse<string>.Fail("Tracking numbers are required.", ResponseErrorType.BadRequest);
			}

			var token = _configuration["GHN:Token"];
			var getTokenUrl = _configuration["GHN:GetTokenUrl"];
			var getOrderInfoUrl = _configuration["GHN:GetOrderInfoUrl"];

			var requestBody = new
			{
				order_codes = request.TrackingNumbers
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, getTokenUrl);
			requestMessage.Headers.Add("Token", token);
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				return BaseResponse<string>.Fail($"Failed to generate GHN token: {errorContent}");
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<GetOrderInfoTokenResponse>>();
			if (result?.Code != 200 || string.IsNullOrWhiteSpace(result.Data?.Token) || string.IsNullOrWhiteSpace(getOrderInfoUrl))
			{
				return BaseResponse<string>.Fail("Invalid response from GHN when generating order info URL.");
			}

			var orderInfoUrl = getOrderInfoUrl.Contains('?')
				? $"{getOrderInfoUrl}&token={result.Data.Token}"
				: $"{getOrderInfoUrl}?token={result.Data.Token}";

			return BaseResponse<string>.Ok(orderInfoUrl, "GHN order info URL generated successfully.");
		}

		public async Task CancelOrderAsync(CancelOrderRequest request)
		{
			if (request.TrackingNumbers.Count == 0)
			{
				throw AppException.BadRequest("Tracking numbers are required.");
			}

			var token = _configuration["GHN:Token"];
			var shopId = _configuration["GHN:ShopId"];
			var cancelOrderUrl = _configuration["GHN:CancelOrderUrl"];

			var requestBody = new
			{
				order_codes = request.TrackingNumbers
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, cancelOrderUrl);
			requestMessage.Headers.Add("Token", token);
			if (!string.IsNullOrWhiteSpace(shopId))
			{
				requestMessage.Headers.Add("ShopId", shopId);
			}
			requestMessage.Content = JsonContent.Create(requestBody);

			var response = await _httpClient.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error Detail: {errorContent}");
				throw AppException.Internal($"Failed to cancel GHN order: {errorContent}");
			}

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<object>>();
			if (result?.Code != 200)
			{
				throw AppException.Internal(result?.Message ?? "GHN cancel order request failed.");
			}
		}
	}
}
