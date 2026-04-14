using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class LedgerService : ILedgerService
	{
		private readonly IUnitOfWork _unitOfWork;

		public LedgerService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<BaseResponse<PagedResult<CashFlowLedgerItemResponse>>> GetCashFlowLedgersAsync(GetCashFlowLedgersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.CashFlowLedgers.GetPagedAsync(request);
			var pagedResult = new PagedResult<CashFlowLedgerItemResponse>(items, request.PageNumber, request.PageSize, totalCount);
			return BaseResponse<PagedResult<CashFlowLedgerItemResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<PagedResult<InventoryLedgerItemResponse>>> GetInventoryLedgersAsync(GetInventoryLedgersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.InventoryLedgers.GetPagedAsync(request);
			var pagedResult = new PagedResult<InventoryLedgerItemResponse>(items, request.PageNumber, request.PageSize, totalCount);
			return BaseResponse<PagedResult<InventoryLedgerItemResponse>>.Ok(pagedResult);
		}
	}
}
