using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class GetPagedImportTicketsRequest : PagingAndSortingQuery
	{
		public int? SupplierId { get; set; }
		public ImportStatus? Status { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
	}
}
