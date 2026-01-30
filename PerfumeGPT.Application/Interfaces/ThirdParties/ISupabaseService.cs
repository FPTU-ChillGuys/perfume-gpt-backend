namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISupabaseService
	{
		Task<string?> UploadImageAsync(Stream fileStream, string fileName, string bucketName);
		Task<bool> DeleteImageAsync(string filePath, string bucketName);
		string GetPublicUrl(string filePath, string bucketName);
		Task<string?> UploadProductImageAsync(Stream fileStream, string fileName);
		Task<string?> UploadVariantImageAsync(Stream fileStream, string fileName);
		Task<string?> UploadAvatarImageAsync(Stream fileStream, string fileName);
		Task<bool> DeleteProductImageAsync(string filePath);
		Task<bool> DeleteVariantImageAsync(string filePath);
		Task<bool> DeleteAvatarImageAsync(string filePath);
	}
}

