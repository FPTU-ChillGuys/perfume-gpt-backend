using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderValidationService : IOrderValidationService
	{
		public BaseResponse<bool> ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
		{
			// Define valid status transitions
			var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
			{
				{ OrderStatus.Pending, [OrderStatus.Processing, OrderStatus.Canceled] },
				{ OrderStatus.Processing, [OrderStatus.Delivering, OrderStatus.Canceled] },
				{ OrderStatus.Delivering, [OrderStatus.Delivered, OrderStatus.Returned] },
				{ OrderStatus.Delivered, [OrderStatus.Returned] },
				{ OrderStatus.Canceled, [] },
				{ OrderStatus.Returned, [] }
			};

			if (currentStatus == newStatus)
			{
				return BaseResponse<bool>.Fail("Order is already in this status.", ResponseErrorType.BadRequest);
			}

			if (!(validTransitions.ContainsKey(currentStatus) && validTransitions[currentStatus].Contains(newStatus)))
			{
				return BaseResponse<bool>.Fail(
					$"Cannot change status from {currentStatus} to {newStatus}.",
					ResponseErrorType.BadRequest);
			}

			return BaseResponse<bool>.Ok(true);
		}
	}
}
