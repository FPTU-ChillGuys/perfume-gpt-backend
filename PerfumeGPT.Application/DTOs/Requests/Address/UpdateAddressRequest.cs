namespace PerfumeGPT.Application.DTOs.Requests.Address
{
	public class UpdateAddressRequest : CreateAddressRequest
	{
		bool IsDefault { get; set; }
	}
}
