namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies
{
	public record UpdateOlfactoryFamilyRequest
	{
		public required string Name { get; init; }
	}
}