namespace PerfumeGPT.Application.DTOs.Requests.Address.GHTKs
{
	public record GetAddressLevel4Request
	{
		public required string Province { get; init; }
		public required string District { get; init; }
		public required string Ward_street { get; init; }
	}
}
