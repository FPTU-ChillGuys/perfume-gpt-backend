using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IRecipientService
	{
		Task<RecipientInfo> CreateRecipientInfoAsync(Guid orderId, RecipientInformation? request, Guid? savedAddressId, Guid? customerId = null);
		Task<RecipientInfo> UpdateRecipientInfoAsync(RecipientInfo existingRecipient, RecipientInformation? request, Guid? savedAddressId, Guid userId);
		Task<RecipientInformation> ResolveRecipientDataAsync(RecipientInformation? recipientInfo, Guid? savedAddressId, Guid? customerId);
	}
}
