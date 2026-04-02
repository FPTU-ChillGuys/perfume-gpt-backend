using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies
{
	public record OlfactoryFamilyResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}