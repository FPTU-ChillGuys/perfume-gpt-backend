namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies
{
	public record CreateOlfactoryFamilyRequest
	{
		public required string Name { get; init; }
	}
}