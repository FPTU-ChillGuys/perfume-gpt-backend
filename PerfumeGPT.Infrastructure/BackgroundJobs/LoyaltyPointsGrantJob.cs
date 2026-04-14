using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class LoyaltyPointsGrantJob : ILoyaltyPointsAppService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;
		private readonly IAuditScope _auditScope;

		public LoyaltyPointsGrantJob(
			IUnitOfWork unitOfWork,
			ILoyaltyTransactionService loyaltyTransactionService,
			IAuditScope auditScope)
		{
			_unitOfWork = unitOfWork;
			_loyaltyTransactionService = loyaltyTransactionService;
			_auditScope = auditScope;
		}

		public async Task GrantPointsIfEligibleAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
			if (order == null || !order.CustomerId.HasValue)
			{
				return;
			}

			if (order.Status != OrderStatus.Delivered || order.PaymentStatus != PaymentStatus.Paid)
			{
				return;
			}

			var alreadyAwarded = await _unitOfWork.LoyaltyTransactions.AnyAsync(x =>
				x.OrderId == order.Id &&
				x.TransactionType == LoyaltyTransactionType.Earn);
			if (alreadyAwarded)
			{
				return;
			}

			var shippingFee = order.ForwardShipping?.ShippingFee;
			if (!shippingFee.HasValue)
			{
				var shippingInfo = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(order.Id);
				shippingFee = shippingInfo?.ShippingFee;
			}

			var loyaltyBaseAmount = Math.Max(0m, order.TotalAmount - (shippingFee ?? 0m));
			int points = (int)(loyaltyBaseAmount / 1000m);
			if (points <= 0)
			{
				return;
			}

			using (_auditScope.BeginSystemAction())
			{
				await _loyaltyTransactionService.PlusPointAsync(
					order.CustomerId.Value,
					points,
					order.Id,
					reason: "Points awarded after delivery + 10 days.");
			}
		}
	}
}
