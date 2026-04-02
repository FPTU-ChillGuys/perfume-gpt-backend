namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations
{
	public record UpdateConcentrationRequest
	{
		public required string Name { get; init; }
	}
}