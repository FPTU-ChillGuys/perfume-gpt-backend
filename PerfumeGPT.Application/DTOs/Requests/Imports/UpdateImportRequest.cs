using PerfumeGPT.Application.DTOs.Requests.ImportDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class UpdateImportRequest
	{
		public int SupplierId { get; set; }
		public DateTime ExpectedArrivalDate { get; set; }
		public List<UpdateImportDetailRequest> ImportDetails { get; set; } = [];
	}
}
