using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public record UpdateImportStatusRequest
	{
		public ImportStatus Status { get; init; }
	}
}
