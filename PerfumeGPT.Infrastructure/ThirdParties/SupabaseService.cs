using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Extensions;
using Supabase;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class SupabaseService : ISupabaseService
	{
		private readonly Client _supabaseClient;
		private readonly ILogger<SupabaseService> _logger;
		private readonly SupabaseSettings _settings;
		private readonly Task _initializeTask;

		public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
		{
			_settings = new SupabaseSettings
			{
				Url = configuration["SUPABASE__URL"] ?? configuration["Supabase:Url"] ?? string.Empty,
				ApiKey = configuration["SUPABASE__API_KEY"] ?? configuration["Supabase:API_KEY"] ?? string.Empty,
				BucketProductName = configuration["SUPABASE__BUCKET_Product_NAME"] ?? configuration["Supabase:BucketProductName"] ?? "Products",
				BucketVariantName = configuration["SUPABASE__BUCKET_Variant_NAME"] ?? configuration["Supabase:BucketVariantName"] ?? "ProductVariants",
				BucketAvatarName = configuration["SUPABASE__BUCKET_Avatar_NAME"] ?? configuration["Supabase:BucketAvatarName"] ?? "ProfileAvatars",
				BucketPreviewName = configuration["SUPABASE__BUCKET_Preview_NAME"] ?? configuration["Supabase:BucketPreviewName"] ?? "Previews",
				BucketOrderReturnRequestName = configuration["SUPABASE__BUCKET_OrderReturnRequest_NAME"] ?? configuration["Supabase:BucketOrderReturnRequestName"] ?? "OrderReturnRequests",
				BucketBannerName = configuration["SUPABASE__BUCKET_Banner_NAME"] ?? configuration["Supabase:BucketBannerName"] ?? "Banners"
			};

			if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
			{
				throw new InvalidOperationException("Thiếu cấu hình Supabase URL hoặc API Key. Vui lòng kiểm tra lại cấu hình.");
			}

			var options = new SupabaseOptions
			{
				AutoRefreshToken = true,
				AutoConnectRealtime = false
			};

			_supabaseClient = new Client(_settings.Url, _settings.ApiKey, options);
			_initializeTask = InitializeClientAndBucketsAsync();
			_logger = logger;
		}

		public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string bucketName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
			ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

			if (fileStream == null || fileStream.Length == 0)
				throw new ArgumentException("File stream is empty hoặc null.");

			await EnsureInitializedAsync();

			if (fileStream.CanSeek)
				fileStream.Position = 0;

			var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName).Trim()}";

			try
			{
				var fileBytes = new byte[fileStream.Length];
				_ = await fileStream.ReadAsync(fileBytes);

				await _supabaseClient.Storage
					.From(bucketName)
					.Upload(fileBytes, uniqueFileName);

				return GetPublicUrl(uniqueFileName, bucketName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to upload image '{FileName}' to bucket '{Bucket}'", fileName, bucketName);
				return null;
			}
		}

		public async Task<bool> DeleteImageAsync(string filePath, string bucketName)
		{
			if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(bucketName))
				return false;

			var fileName = ExtractFileNameFromUrl(filePath);
			if (string.IsNullOrWhiteSpace(fileName))
				return false;

			await EnsureInitializedAsync();

			try
			{
				await _supabaseClient.Storage
					.From(bucketName)
					.Remove(fileName);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete image '{FilePath}' from bucket '{Bucket}'", filePath, bucketName);
				return false;
			}
		}

		public string GetPublicUrl(string filePath, string bucketName)
		{
			if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(bucketName))
				return string.Empty;

			return _supabaseClient.Storage
				.From(bucketName)
				.GetPublicUrl(filePath) ?? string.Empty;
		}

		#region Private Helpers
		private async Task InitializeClientAndBucketsAsync()
		{
			await _supabaseClient.InitializeAsync();
			await EnsureBucketExistsAsync(_settings.BucketBannerName);
		}

		private async Task EnsureBucketExistsAsync(string bucketName)
		{
			if (string.IsNullOrWhiteSpace(bucketName))
			{
				return;
			}

			using var httpClient = new HttpClient { BaseAddress = new Uri(_settings.Url) };
			httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
			httpClient.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

			var payload = new Dictionary<string, object>
			{
				["id"] = bucketName,
				["name"] = bucketName,
				["public"] = true
			};

			var response = await httpClient.PostAsJsonAsync("/storage/v1/bucket", payload);

			if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
			{
				return;
			}

			var errorBody = await response.Content.ReadAsStringAsync();

			// Supabase may return HTTP 400 with payload statusCode=409 for duplicate bucket.
			if (response.StatusCode == HttpStatusCode.BadRequest
				&& !string.IsNullOrWhiteSpace(errorBody)
				&& IsDuplicateResourceError(errorBody))
			{
				return;
			}

			_logger.LogWarning("Could not create or verify bucket '{BucketName}'. Status: {StatusCode}. Response: {ResponseBody}",
				bucketName,
				response.StatusCode,
				errorBody);
		}

		private static bool IsDuplicateResourceError(string errorBody)
		{
			try
			{
				using var document = JsonDocument.Parse(errorBody);
				var root = document.RootElement;

				var statusCode = root.TryGetProperty("statusCode", out var statusCodeElement)
					? statusCodeElement.GetString()
					: null;

				var error = root.TryGetProperty("error", out var errorElement)
					? errorElement.GetString()
					: null;

				return string.Equals(statusCode, "409", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(error, "Duplicate", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private async Task EnsureInitializedAsync() => await _initializeTask;

		private static string ExtractFileNameFromUrl(string url)
		{
			try
			{
				var segments = new Uri(url).AbsolutePath.Split('/');
				return segments.Length > 0 ? Uri.UnescapeDataString(segments[^1]) : string.Empty;
			}
			catch
			{
				return Path.GetFileName(url);
			}
		}
		#endregion Private Helpers
	}
}
