using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IRecipientService
	{
        Task<BaseResponse<RecipientInfo>> CreateRecipientInfoAsync(
            Guid orderId,
            RecipientInformation request,
            Guid? customerId = null);

        Task<BaseResponse<RecipientInfo>> UpdateRecipientInfoAsync(
            RecipientInfo existingRecipient,
            RecipientInformation request,
            Guid userId);

        Task<BaseResponse<RecipientInformation>> ResolveRecipientDataAsync(
            RecipientInformation? request,
            Guid? customerId);
	}
}
