namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record UpdateCodRequest
	{
		public int CodAmount { get; init; }
		public required string OrderCode { get; init; }
	}
}
