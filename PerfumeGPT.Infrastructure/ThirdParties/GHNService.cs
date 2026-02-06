using Microsoft.Extensions.Configuration;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.GHNs.Address;
using PerfumeGPT.Application.DTOs.Responses.GHNs;
using PerfumeGPT.Application.DTOs.Responses.GHNs.Base;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Net.Http.Json;

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

		public async Task<CalculateFeeResponse?> CalculateShippingFeeAsync(CalculateFeeRequest request)
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
				//items = new[]
				//{
				//	new
				//	{
				//		name = "ItemA",
				//		quantity = 1,
				//		length = 200,
				//		width = 200,
				//		height = 200,
				//		weight = 1000
				//	}
				//}
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

		public async Task<List<DistrictResponse>> GetDistrictsByProvinceIdAsync(int provinceId)
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
			return result?.Data ?? new List<DistrictResponse>();
		}

		public async Task<List<ProvinceResponse>> GetProvincesAsync()
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
			return result?.Data ?? new List<ProvinceResponse>();
		}

		public async Task<List<WardResponse>> GetWardsByDistrictIdAsync(int districtId)
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
			return result?.Data ?? new List<WardResponse>();
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

			var result = await response.Content.ReadFromJsonAsync<GHNApiResponse<CreateShippingOrderResponse>>();
			return result?.Data;
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
	}
}
