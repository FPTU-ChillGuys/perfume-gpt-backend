namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations
{
	public record CreateConcentrationRequest
	{
		public required string Name { get; init; }
	}
}