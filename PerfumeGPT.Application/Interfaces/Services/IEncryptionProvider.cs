namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IEncryptionProvider
	{
		string? Encrypt(string? plainText);
		string? Decrypt(string? cipherText);
	}
}
