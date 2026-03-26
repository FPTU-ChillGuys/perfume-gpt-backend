namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISupabaseService
	{
		Task<string?> UploadImageAsync(Stream fileStream, string fileName, string bucketName);
		Task<bool> DeleteImageAsync(string filePath, string bucketName);
		string GetPublicUrl(string filePath, string bucketName);
	}
}

