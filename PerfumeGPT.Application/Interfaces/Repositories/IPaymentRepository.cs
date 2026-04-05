using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IPaymentRepository : IGenericRepository<PaymentTransaction>
	{
       Task<PaymentTransactionOverviewResponse> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request);
	}
}
