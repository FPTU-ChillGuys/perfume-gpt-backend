using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Extensions;
using Supabase;

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
				BucketOrderReturnRequestName = configuration["SUPABASE__BUCKET_OrderReturnRequest_NAME"] ?? configuration["Supabase:BucketOrderReturnRequestName"] ?? "OrderReturnRequests"
			};

			if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
			{
				throw new InvalidOperationException("Supabase URL and API Key must be configured.");
			}

			var options = new SupabaseOptions
			{
				AutoRefreshToken = true,
				AutoConnectRealtime = false
			};

			_supabaseClient = new Client(_settings.Url, _settings.ApiKey, options);
			_initializeTask = _supabaseClient.InitializeAsync();
			_logger = logger;
		}

		public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string bucketName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
			ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

			if (fileStream == null || fileStream.Length == 0)
				throw new ArgumentException("File stream is empty or null.");

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
