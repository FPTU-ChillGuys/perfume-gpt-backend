using Microsoft.Extensions.Configuration;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Extensions;
using Supabase;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class SupabaseService : ISupabaseService
	{
		private readonly Client _supabaseClient;
		private readonly SupabaseSettings _settings;
		private readonly Task _initializeTask;

		public SupabaseService(IConfiguration configuration)
		{
			_settings = new SupabaseSettings
			{
				Url = configuration["SUPABASE__URL"] ?? configuration["Supabase:Url"] ?? string.Empty,
				ApiKey = configuration["SUPABASE__API_KEY"] ?? configuration["Supabase:API_KEY"] ?? string.Empty,
				BucketProductName = configuration["SUPABASE__BUCKET_Product_NAME"] ?? configuration["Supabase:BucketProductName"] ?? "Products",
				BucketVariantName = configuration["SUPABASE__BUCKET_Variant_NAME"] ?? configuration["Supabase:BucketVariantName"] ?? "ProductVariants",
				BucketAvatarName = configuration["SUPABASE__BUCKET_Avatar_NAME"] ?? configuration["Supabase:BucketAvatarName"] ?? "ProfileAvatars",
				BucketPreviewName = configuration["SUPABASE__BUCKET_Preview_NAME"] ?? configuration["Supabase:BucketPreviewName"] ?? "Previews"
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
		}

		public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string bucketName)
		{
			try
			{
				await EnsureInitializedAsync();

				if (string.IsNullOrWhiteSpace(bucketName))
				{
					throw new ArgumentException("Bucket name is required.");
				}

				if (string.IsNullOrWhiteSpace(fileName))
				{
					throw new ArgumentException("File name is required.");
				}

				if (fileStream == null || fileStream.Length == 0)
				{
					throw new ArgumentException("File stream is empty or null.");
				}

				if (fileStream.CanSeek)
				{
					fileStream.Position = 0;
				}

				var safeFileName = Path.GetFileName(fileName).Trim();
				var uniqueFileName = $"{Guid.NewGuid():N}_{safeFileName}";

				using var memoryStream = new MemoryStream();
				await fileStream.CopyToAsync(memoryStream);
				var fileBytes = memoryStream.ToArray();

				await _supabaseClient.Storage
					.From(bucketName)
					.Upload(fileBytes, uniqueFileName);

				var publicUrl = GetPublicUrl(uniqueFileName, bucketName);
				return publicUrl;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error uploading image: {ex.Message}");
				return null;
			}
		}

		public async Task<bool> DeleteImageAsync(string filePath, string bucketName)
		{
			try
			{
				await EnsureInitializedAsync();

				if (string.IsNullOrWhiteSpace(bucketName))
				{
					return false;
				}

				if (string.IsNullOrWhiteSpace(filePath))
				{
					return false;
				}

				var fileName = ExtractFileNameFromUrl(filePath);
				if (string.IsNullOrWhiteSpace(fileName))
				{
					return false;
				}

				await _supabaseClient.Storage
					.From(bucketName)
					.Remove(fileName);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error deleting image: {ex.Message}");
				return false;
			}
		}

		public string GetPublicUrl(string filePath, string bucketName)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(bucketName))
				{
					return string.Empty;
				}

				var publicUrl = _supabaseClient.Storage
					.From(bucketName)
					.GetPublicUrl(filePath);

				return publicUrl ?? string.Empty;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error getting public URL: {ex.Message}");
				return string.Empty;
			}
		}

		public async Task<string?> UploadVariantImageAsync(Stream fileStream, string fileName)
		   => await UploadImageAsync(fileStream, fileName, _settings.BucketVariantName);

		public async Task<string?> UploadProductImageAsync(Stream fileStream, string fileName)
		   => await UploadImageAsync(fileStream, fileName, _settings.BucketProductName);

		public async Task<string?> UploadPreviewImageAsync(Stream fileStream, string fileName)
		   => await UploadImageAsync(fileStream, fileName, _settings.BucketPreviewName);

		public async Task<string?> UploadAvatarImageAsync(Stream fileStream, string fileName)
		   => await UploadImageAsync(fileStream, fileName, _settings.BucketAvatarName);

		public async Task<bool> DeleteVariantImageAsync(string filePath)
		   => await DeleteImageAsync(filePath, _settings.BucketVariantName);

		public async Task<bool> DeleteProductImageAsync(string filePath)
		   => await DeleteImageAsync(filePath, _settings.BucketProductName);

		public async Task<bool> DeletePreviewImageAsync(string filePath)
		   => await DeleteImageAsync(filePath, _settings.BucketPreviewName);

		public async Task<bool> DeleteAvatarImageAsync(string filePath)
		   => await DeleteImageAsync(filePath, _settings.BucketAvatarName);

		private async Task EnsureInitializedAsync()
		{
			await _initializeTask;
		}

		private static string ExtractFileNameFromUrl(string url)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(url))
				{
					return string.Empty;
				}

				var uri = new Uri(url);
				var segments = uri.AbsolutePath.Split('/');
				return segments.Length > 0 ? Uri.UnescapeDataString(segments[^1]) : string.Empty;
			}
			catch
			{
				return Path.GetFileName(url);
			}
		}
	}
}
