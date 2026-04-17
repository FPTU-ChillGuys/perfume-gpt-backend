using Microsoft.Extensions.Configuration;
using PerfumeGPT.Application.DTOs.Requests.Address.GHTKs;
using PerfumeGPT.Application.DTOs.Responses.Address.GHTKs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.GHTKs.Base;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Net.Http.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class GHTKService : IGHTKService
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly string GHTK_BaseUrl;
		private readonly string GHTK_GetAddressLevel4Url;
		private readonly string GHTK_Token;
		private readonly string GHTK_PartnerCode;

		public GHTKService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			GHTK_BaseUrl = _configuration["GHTK:BaseUrl"] ?? throw new NullReferenceException("Thiếu cấu hình GHTK:BaseUrl!");
			GHTK_GetAddressLevel4Url = _configuration["GHTK:GetAddressLevel4Url"] ?? throw new NullReferenceException("Thiếu cấu hình GHTK:GetAddressLevel4Url!");
			GHTK_Token = _configuration["GHTK:Token"] ?? throw new NullReferenceException("Thiếu cấu hình GHTK:Token!");
			GHTK_PartnerCode = _configuration["GHTK:PartnerCode"] ?? throw new NullReferenceException("Thiếu cấu hình GHTK:PartnerCode!");
		}

		public async Task<BaseResponse<AddressLevel4Response>> GetAddressLevel4Async(GetAddressLevel4Request request)
		{
			var httpRequest = new HttpRequestMessage(HttpMethod.Get, GHTK_BaseUrl + GHTK_GetAddressLevel4Url);
			httpRequest.Headers.Add("Token", GHTK_Token);
			httpRequest.Headers.Add("X-Client-Source", GHTK_PartnerCode);
			httpRequest.Content = JsonContent.Create(request);

			var response = await _httpClient.SendAsync(httpRequest);

			if (!response.IsSuccessStatusCode)
			{
				return BaseResponse<AddressLevel4Response>.Ok(new AddressLevel4Response { Data = [] }, "Không thể lấy dữ liệu địa chỉ cấp 4 từ GHTK.");
			}

			var result = await response.Content.ReadFromJsonAsync<GHTKApiResponse<List<string>>>();
			return BaseResponse<AddressLevel4Response>.Ok(new AddressLevel4Response { Data = result?.Data ?? [] });
		}
	}
}
