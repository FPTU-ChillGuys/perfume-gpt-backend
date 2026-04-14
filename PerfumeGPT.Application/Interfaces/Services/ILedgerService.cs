using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ILedgerService
	{
		Task<BaseResponse<PagedResult<CashFlowLedgerItemResponse>>> GetCashFlowLedgersAsync(GetCashFlowLedgersRequest request);
		Task<BaseResponse<PagedResult<InventoryLedgerItemResponse>>> GetInventoryLedgersAsync(GetInventoryLedgersRequest request);
	}
}
