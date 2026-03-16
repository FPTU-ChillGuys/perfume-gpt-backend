using PerfumeGPT.Application.DTOs.Requests.Batches;

namespace PerfumeGPT.Application.DTOs.Requests.ImportDetails
{
	public class VerifyImportDetailRequest
	{
		public Guid ImportDetailId { get; set; }
		public int RejectedQuantity { get; set; } = 0;
		public string? Note { get; set; }
		public List<CreateBatchRequest> Batches { get; set; } = [];
	}
}
