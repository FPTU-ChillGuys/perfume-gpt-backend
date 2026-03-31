using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Shippings
{
	public class GetPagedShippingsRequest : PagingAndSortingQuery
	{
		public ShippingStatus? Status { get; set; }
		public CarrierName? CarrierName { get; set; }
		public Guid? OrderId { get; set; }
		public string? TrackingNumber { get; set; }
	}
}
