namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Concentrations
{
	public record ConcentrationResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}