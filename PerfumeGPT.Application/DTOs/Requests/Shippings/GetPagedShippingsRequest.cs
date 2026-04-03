using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Shippings
{
	public record GetPagedShippingsRequest : PagingAndSortingQuery
	{
		public ShippingStatus? Status { get; init; }
		public CarrierName? CarrierName { get; init; }
		public ShippingType? ShippingType { get; init; }
		public Guid? OrderId { get; init; }
		public string? TrackingNumber { get; init; }
	}
}
