using PerfumeGPT.Application.DTOs.Responses.Carts;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IPosClient
	{
		Task ReceiveBarcode(string barcode);
		Task UpdateCustomerDisplay(CartDisplayDto cartData);
	}
}
