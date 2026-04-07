using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Persistence.Converters
{
	public class EncryptionConverter : ValueConverter<string?, string?>
	{
		public EncryptionConverter(IEncryptionProvider provider, ConverterMappingHints? mappingHints = null)
			: base(
				v => provider.Encrypt(v),
				v => provider.Decrypt(v),
				mappingHints)
		{
		}
	}
}
