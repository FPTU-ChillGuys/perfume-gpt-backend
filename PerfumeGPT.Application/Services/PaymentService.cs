using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Requests.PayOs;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.DTOs.Responses.PayOs;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class PaymentService : IPaymentService
	{
		#region Dependencies
		private const long MaxPayOsOrderCode = 9007199254740991;
		private readonly IVnPayService _vnPayService;
		private readonly IMomoService _momoService;
		private readonly IPayOsService _payOsService;
		private readonly IStockReservationService _stockReservationService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IVoucherService _voucherService;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ISignalRService _signalRService;
		private readonly ILogger<PaymentService> _logger;

		public PaymentService(
			IVnPayService vnPayService,
			IMomoService momoService,
			IPayOsService payOsService,
			IUnitOfWork unitOfWork,
			IHttpContextAccessor httpContextAccessor,
			IVoucherService voucherService,
			ILogger<PaymentService> logger,
			IBackgroundJobService backgroundJobService,
			IStockReservationService stockReservationService,
			ISignalRService signalRService)
		{
			_vnPayService = vnPayService;
			_momoService = momoService;
			_payOsService = payOsService;
			_unitOfWork = unitOfWork;
			_httpContextAccessor = httpContextAccessor;
			_voucherService = voucherService;
			_logger = logger;
			_backgroundJobService = backgroundJobService;
			_stockReservationService = stockReservationService;
			_signalRService = signalRService;
		}
		#endregion Dependencies



		public async Task<BaseResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, ConfirmPaymentRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				if (!payment.IsPending())
				{
					return BaseResponse<bool>.Ok(true, "Thanh toán đã được xử lý trước đó.");
				}

				// BỔ SUNG RÀO CHẮN: Kiểm tra xem giao dịch có phải là Tiền Mặt (CashInStore) không
				// Nếu là tiền mặt thì NHÂN VIÊN mới có quyền gọi API Confirm (có request.IsSuccess).
				// Các giao dịch VNPay/MoMo thì phải chờ Webhook/ReturnUrl, không cho Confirm bằng API này.
				if (payment.Method != PaymentMethod.CashInStore)
				{
					throw AppException.BadRequest($"Phương thức {payment.Method} không thể được xác nhận thủ công. Vui lòng chờ phản hồi từ Cổng thanh toán.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				return request.IsSuccess
					? await CompleteSuccessfulPayment(payment, order, "TIỀN MẶT TẠI QUẦY") // Đánh dấu là tiền mặt
					: await HandleFailedPayment(payment, order, request.FailureReason ?? "Khách hàng từ chối thanh toán tiền mặt.");
			});
		}

		private async Task<PaymentTransaction> ResolvePayOsPaymentFromCallbackAsync(IQueryCollection queryParameters)
		{
			if (queryParameters.TryGetValue("paymentId", out var paymentIdValue) &&
				Guid.TryParse(paymentIdValue.ToString(), out var paymentId))
			{
				var paymentById = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				if (paymentById.Method != PaymentMethod.PayOs)
				{
					throw AppException.BadRequest("Phương thức thanh toán không phải PayOS.");
				}

				return paymentById;
			}

			if (!queryParameters.TryGetValue("orderCode", out var orderCodeValue) ||
				!long.TryParse(orderCodeValue.ToString(), out var callbackOrderCode) ||
				callbackOrderCode <= 0)
			{
				throw AppException.BadRequest("Callback PayOS thiếu thông tin định danh thanh toán bắt buộc.");
			}

			var payOsPendingPayments = await _unitOfWork.Payments.GetAllAsync(
				p => p.Method == PaymentMethod.PayOs &&
					 p.TransactionType == TransactionType.Payment &&
					 p.TransactionStatus == TransactionStatus.Pending,
				include: q => q.Include(p => p.Order),
				orderBy: q => q.OrderByDescending(p => p.CreatedAt));

			var matchedPayment = payOsPendingPayments
				.FirstOrDefault(p => ResolvePayOsOrderCode(p.Order.Code, p.Id) == callbackOrderCode);

			return matchedPayment ?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán PayOS.");
		}

		private static long ResolvePayOsOrderCode(string orderCode, Guid paymentId)
		{
			if (long.TryParse(orderCode, out var parsedOrderCode) && parsedOrderCode > 0)
			{
				return NormalizePayOsOrderCode(parsedOrderCode);
			}

			var fallbackOrderCode = (long)(BitConverter.ToUInt64(paymentId.ToByteArray(), 0) % MaxPayOsOrderCode);
			if (fallbackOrderCode == 0)
			{
				fallbackOrderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % MaxPayOsOrderCode;
			}

			return fallbackOrderCode == 0 ? 1 : fallbackOrderCode;
		}

		private static long NormalizePayOsOrderCode(long orderCode)
		{
			var normalized = (long)(unchecked((ulong)orderCode) % MaxPayOsOrderCode);
			return normalized == 0 ? 1 : normalized;
		}

		public async Task<BaseResponse<string>> RetryOrChangePaymentMethodAsync(Guid paymentId, RetryOrChangePaymentRequest request)
		{
			var currentPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
				?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

			if (currentPayment.TransactionStatus == TransactionStatus.Success)
				throw AppException.BadRequest("Không thể thử lại thanh toán đã hoàn tất.");

			if (currentPayment.Method == PaymentMethod.VnPay)
			{
				var statusSyncResponse = await TryCompleteVnPayIfAlreadyPaidAsync(currentPayment);
				if (statusSyncResponse != null)
				{
					return statusSyncResponse;
				}
			}

			if (currentPayment.Method == PaymentMethod.Momo)
			{
				var statusSyncResponse = await TryCompleteMomoIfAlreadyPaidAsync(currentPayment);
				if (statusSyncResponse != null)
				{
					return statusSyncResponse;
				}
			}

			if (currentPayment.Method == PaymentMethod.PayOs)
			{
				var order = await _unitOfWork.Orders.GetByIdAsync(currentPayment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				var statusSyncResponse = await TryCompletePayOsIfAlreadyPaidAsync(currentPayment, order.Code);
				if (statusSyncResponse != null)
				{
					return statusSyncResponse;
				}
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				if (payment.TransactionStatus == TransactionStatus.Success)
					throw AppException.BadRequest("Không thể thử lại thanh toán đã hoàn tất.");

				var isOnlineMethod = payment.Method == PaymentMethod.VnPay || payment.Method == PaymentMethod.Momo || payment.Method == PaymentMethod.PayOs;
				if (!isOnlineMethod &&
					payment.TransactionStatus != TransactionStatus.Pending &&
					payment.TransactionStatus != TransactionStatus.Failed)
					throw AppException.BadRequest("Chỉ có thể thử lại thanh toán ở trạng thái đang chờ hoặc thất bại.");

				var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId)
					 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (order.PaymentExpiresAt.HasValue && order.PaymentExpiresAt.Value < DateTime.UtcNow)
					throw AppException.BadRequest("Thời gian thanh toán của đơn hàng này đã hết. Vui lòng tạo đơn hàng mới.");


				// 1. HỦY TOÀN BỘ GIAO DỊCH PENDING HIỆN TẠI (Bao gồm cả cọc Gateway và COD chờ thu)
				var existingPendingPayments = await _unitOfWork.Payments
					.GetAllAsync(p => p.OrderId == order.Id &&
									  p.TransactionType == payment.TransactionType &&
									  p.TransactionStatus == TransactionStatus.Pending);

				foreach (var pendingPayment in existingPendingPayments)
				{
					pendingPayment.MarkCancelled("Được thay thế bởi yêu cầu thanh toán mới.");
					_unitOfWork.Payments.Update(pendingPayment);
				}

				// ======================================================================
				// 2. LOGIC NÂNG CẤP: PHÂN TÍCH Ý ĐỊNH CHUYỂN ĐỔI THANH TOÁN
				// ======================================================================
				PaymentMethod transactionMethod;
				decimal amountToPay;
				bool isStillDepositFlow = false;
				decimal remainingToCollectAfterDeposit = 0;

				if (request.NewPaymentMethod.HasValue)
				{
					var targetMethod = request.NewPaymentMethod.Value;

					// - Luôn cọc nếu là COD.
					// - Chỉ cọc cho CashInStore NẾU đơn đó là đặt từ Online (Lấy tại cửa hàng).
					// => Ngược lại (Offline + CashInStore) sẽ được hiểu là trả Full 100%.
					bool requiresDepositFlow = targetMethod == PaymentMethod.CashOnDelivery ||
											  (targetMethod == PaymentMethod.CashInStore && order.Type == OrderType.Online);

					// Bật/Tắt tiền cọc dựa trên snapshot đã lưu trong Entity
					if ((requiresDepositFlow && order.RequiredDepositAmount == 0) || (!requiresDepositFlow && order.RequiredDepositAmount > 0))
					{
						order.ToggleDepositRequirement(requiresDepositFlow);
					}

					// RÀO CHẮN 1: Chọn trả Full 100% nhưng lại đính kèm cổng cọc
					if (!requiresDepositFlow && request.NewDepositMethod.HasValue)
						throw AppException.BadRequest("Đơn hàng thanh toán toàn bộ không được đính kèm cổng thanh toán cọc.");

					// Lúc này order.RequiredDepositAmount đã được bật/tắt chính xác
					if (requiresDepositFlow && order.RequiredDepositAmount > 0)
					{
						// KỊCH BẢN A: Đổi cổng Cọc
						if (!request.NewDepositMethod.HasValue)
							throw AppException.BadRequest("Vui lòng chọn cổng thanh toán để đặt cọc.");

						var depositMethod = request.NewDepositMethod.Value;

						// 💥 Vẫn cho phép CashInStore làm cổng cọc (dành cho đơn POS khách cọc bằng tiền mặt rồi giao COD)
						if (depositMethod != PaymentMethod.VnPay &&
							depositMethod != PaymentMethod.Momo &&
							depositMethod != PaymentMethod.PayOs &&
							depositMethod != PaymentMethod.CashInStore)
						{
							throw AppException.BadRequest("Cổng thanh toán đặt cọc chỉ hỗ trợ VNPay, Momo, PayOs hoặc Tiền mặt tại quầy.");
						}

						transactionMethod = depositMethod;
						amountToPay = order.RequiredDepositAmount;
						isStillDepositFlow = true;
						remainingToCollectAfterDeposit = order.TotalAmount - order.RequiredDepositAmount;
					}
					else
					{
						// KỊCH BẢN B: Đổi sang trả Full 100% (Ví dụ: Offline đổi sang Tiền mặt 100%, hoặc Online đổi sang VNPay 100%)
						transactionMethod = targetMethod;
						amountToPay = order.RemainingAmount > 0 ? order.RemainingAmount : order.TotalAmount;
					}
				}
				else
				{
					// Ý định 2: KỊCH BẢN C - CHỈ RETRY (Frontend truyền rỗng NewPaymentMethod)
					transactionMethod = payment.Method;

					// Giữ nguyên logic tính tiền của giao dịch cũ
					if (payment.Amount == order.RequiredDepositAmount && order.PaymentStatus == PaymentStatus.Unpaid)
					{
						amountToPay = order.RequiredDepositAmount;
						isStillDepositFlow = true;
						remainingToCollectAfterDeposit = order.TotalAmount - order.RequiredDepositAmount;
					}
					else
					{
						amountToPay = payment.Amount;
					}
				}

				// 3. TẠO GIAO DỊCH CHÍNH (Cọc hoặc Trả Full)
				var newPayment = payment.CreateRetry(transactionMethod, amountToPay);
				await _unitOfWork.Payments.AddAsync(newPayment);

				// 4. TÁI TẠO LẠI GIAO DỊCH PHỤ (Nếu vẫn nằm trong luồng Cọc)
				if (isStillDepositFlow && remainingToCollectAfterDeposit > 0)
				{
					var primaryOrderMethod = request?.NewPaymentMethod ??
						(order.Type == OrderType.Offline ? PaymentMethod.CashInStore : PaymentMethod.CashOnDelivery);

					var pendingRemainingTransaction = PaymentTransaction.Create(
						order.Id,
						primaryOrderMethod,
						remainingToCollectAfterDeposit);

					await _unitOfWork.Payments.AddAsync(pendingRemainingTransaction);
				}
				// ======================================================================

				// 5. CẬP NHẬT EXPIRATION TIME
				var refreshedExpiration = GetPaymentExpiration(transactionMethod);
				DateTime? finalExpirationToSet = order.PaymentExpiresAt;

				if (!order.PaymentExpiresAt.HasValue)
				{
					finalExpirationToSet = refreshedExpiration;
				}
				else if (refreshedExpiration.HasValue && refreshedExpiration.Value < order.PaymentExpiresAt.Value)
				{
					finalExpirationToSet = refreshedExpiration;
				}

				if (finalExpirationToSet != order.PaymentExpiresAt)
				{
					order.SetPaymentExpiration(finalExpirationToSet);

					var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
					foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.Reserved))
					{
						reservation.SetExpiration(finalExpirationToSet);
						_unitOfWork.StockReservations.Update(reservation);
					}
				}

				_unitOfWork.Orders.Update(order);

				var methodChanged = payment.Method != transactionMethod;
				var methodMessage = methodChanged ? $" (đổi từ {payment.Method} sang {transactionMethod})" : "";

				var paymentResponse = await GeneratePaymentResponse(newPayment, order, newPayment.RetryAttempt, methodMessage, request?.PosSessionId);

				if (!string.IsNullOrWhiteSpace(request?.PosSessionId)
					&& !string.IsNullOrWhiteSpace(paymentResponse.Payload)
					&& (transactionMethod == PaymentMethod.VnPay || transactionMethod == PaymentMethod.Momo || transactionMethod == PaymentMethod.PayOs))
				{
					try
					{
						await _signalRService.NotifyPosPaymentLinkUpdatedAsync(request.PosSessionId, new PosPaymentLinkDto
						{
							OrderId = order.Id,
							PaymentId = newPayment.Id,
							Method = transactionMethod.ToString(),
							PaymentUrl = paymentResponse.Payload
						});
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Could not broadcast POS payment link update...");
					}
				}

				return paymentResponse;
			});
		}

		public async Task<BaseResponse<PaymentTransactionOverviewResponse>> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request)
		{
			var response = await _unitOfWork.Payments.GetTransactionsForManagementAsync(request);
			return BaseResponse<PaymentTransactionOverviewResponse>.Ok(response);
		}

		// PayOs methods
		public async Task<PayOsReturnResponse> ProcessPayOsReturnAsync(IQueryCollection queryParameters, bool isCancelCallback = false)
		{
			var payment = await ResolvePayOsPaymentFromCallbackAsync(queryParameters);
			var orderCode = queryParameters.TryGetValue("orderCode", out var orderCodeValue)
				? orderCodeValue.ToString()
				: string.Empty;
			var callbackPosSessionId = queryParameters.TryGetValue("posSessionId", out var queryPosSessionId)
				? queryPosSessionId.ToString()
				: (queryParameters.TryGetValue("sessionId", out var querySessionId) ? querySessionId.ToString() : null);

			var payOsInfo = await _payOsService.GetPaymentInfoAsync(orderCode, payment.Id);
			var extractedOrderCode = payOsInfo.ExtractedOrderCode ?? payOsInfo.OrderCode.ToString();
			var extractedPosSessionId = payOsInfo.PosSessionId ?? callbackPosSessionId;

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				var payload = new PayOsReturnResponse
				{
					PaymentId = latestPayment.Id,
					OrderId = latestPayment.OrderId,
					OrderCode = extractedOrderCode,
					PosSessionId = extractedPosSessionId,
					IsSuccess = false
				};

				if (!latestPayment.IsPending())
				{
					return payload with { IsSuccess = latestPayment.TransactionStatus == TransactionStatus.Success };
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				var isCancelled = isCancelCallback ||
					(queryParameters.TryGetValue("cancel", out var cancelValue) &&
					 bool.TryParse(cancelValue.ToString(), out var parsedCancel) &&
					 parsedCancel);

				if (isCancelled)
				{
					latestPayment.MarkCancelled("Thanh toán PayOS đã bị người dùng hủy.");
					//order.MarkUnpaid();
					_unitOfWork.Payments.Update(latestPayment);
					_unitOfWork.Orders.Update(order);
					return payload;
				}

				if (!payOsInfo.IsSuccess)
				{
					await HandleFailedPayment(latestPayment, order, payOsInfo.Message ?? "Xác thực thanh toán PayOS thất bại.", payOsInfo.PaymentLinkId);
					return payload;
				}

				if (payOsInfo.Amount > 0 && latestPayment.Amount != payOsInfo.Amount)
				{
					throw AppException.BadRequest("Số tiền thanh toán không khớp.");
				}

				if (payOsInfo.IsPaid)
				{
					await CompleteSuccessfulPayment(latestPayment, order, payOsInfo.PaymentLinkId, extractedPosSessionId);
					return payload with { IsSuccess = true };
				}

				await HandleFailedPayment(latestPayment, order, $"Trạng thái thanh toán PayOS: {payOsInfo.Status ?? "KHÔNG_XÁC_ĐỊNH"}", payOsInfo.PaymentLinkId, extractedPosSessionId);
				return payload;
			});
		}

		// MoMo methods
		public async Task<MomoReturnResponse> ProcessMomoReturnAsync(IQueryCollection queryParameters)
		{
			var momoResponse = _momoService.GetPaymentResponseAsync(queryParameters);
			var orderCode = momoResponse.OrderCode;
			var posSessionId = momoResponse.PosSessionId;
			if (momoResponse.PaymentId == Guid.Empty)
			{
				throw AppException.BadRequest("Thanh toán MoMo thất bại");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var payment = await _unitOfWork.Payments.GetByIdAsync(momoResponse.PaymentId)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				var payload = new MomoReturnResponse
				{
					PaymentId = payment.Id,
					OrderId = payment.OrderId,
					OrderCode = orderCode,
					PosSessionId = posSessionId,
					IsSuccess = momoResponse.IsSuccess
				};

				if (!payment.IsPending())
				{
					return payload with { IsSuccess = payment.TransactionStatus == TransactionStatus.Success };
				}

				if (payment.Amount != momoResponse.Amount)
				{
					throw AppException.BadRequest("Số tiền thanh toán không khớp.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (momoResponse.IsSuccess)
				{
					await CompleteSuccessfulPayment(payment, order, momoResponse.TransactionNo, posSessionId);
					return payload;
				}

				await HandleFailedPayment(payment, order, momoResponse.Message, momoResponse.TransactionNo, posSessionId);
				return payload;
			});
		}

		// VnPay methods
		public async Task<VnPayReturnResponse> ProcessVnPayReturnAsync(IQueryCollection queryParameters)
		{
			try
			{
				var vnPayResponse = _vnPayService.GetPaymentResponseAsync(queryParameters);
				if (vnPayResponse == null || vnPayResponse.PaymentId == Guid.Empty)
				{
					throw AppException.BadRequest("Thanh toán VNPay thất bại");
				}
				var orderCode = vnPayResponse.OrderCode;
				var posSessionId = vnPayResponse.PosSessionId;

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var payment = await _unitOfWork.Payments.GetByIdAsync(vnPayResponse.PaymentId)
						?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

					var payload = new VnPayReturnResponse
					{
						PaymentId = payment.Id,
						OrderId = payment.OrderId,
						OrderCode = orderCode ?? string.Empty,
						PosSessionId = posSessionId,
						IsSuccess = vnPayResponse.IsSuccess
					};

					if (!payment.IsPending())
					{
						return payload with { IsSuccess = payment.TransactionStatus == TransactionStatus.Success };
					}

					if (payment.Amount != vnPayResponse.Amount)
					{
						throw AppException.BadRequest("Số tiền thanh toán không khớp.");
					}

					var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
					 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

					if (vnPayResponse.IsSuccess)
					{
						await CompleteSuccessfulPayment(payment, order, vnPayResponse.TransactionNo, posSessionId);
						return payload;
					}

					await HandleFailedPayment(payment, order, vnPayResponse.Message, vnPayResponse.TransactionNo, posSessionId);
					return payload;
				});
			}
			catch (DbUpdateConcurrencyException ex)
			{
				_logger.LogWarning(ex, "Giao dịch đã được xử lý bởi một luồng khác (Khả năng cao là IPN Webhook). Bỏ qua lỗi cập nhật.");

				var vnPayResponse = _vnPayService.GetPaymentResponseAsync(queryParameters);
				if (vnPayResponse == null || vnPayResponse.PaymentId == Guid.Empty)
				{
					throw AppException.BadRequest("Thanh toán VNPay thất bại");
				}
				var fallbackOrderCode = vnPayResponse.OrderCode;
				var fallbackPosSessionId = vnPayResponse.PosSessionId;
				var latestPayment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => p.Id == vnPayResponse.PaymentId);

				return latestPayment == null
				  ? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.")
					: new VnPayReturnResponse
					{
						PaymentId = latestPayment.Id,
						OrderId = latestPayment.OrderId,
						OrderCode = fallbackOrderCode,
						PosSessionId = fallbackPosSessionId,
						IsSuccess = latestPayment.TransactionStatus == TransactionStatus.Success
					};
			}
		}

		// private methods
		private async Task<BaseResponse<string>?> TryCompletePayOsIfAlreadyPaidAsync(PaymentTransaction payment, string orderCode)
		{
			var paymentInfo = await _payOsService.GetPaymentInfoAsync(orderCode, payment.Id);
			if (!paymentInfo.IsSuccess || !paymentInfo.IsPaid)
			{
				return null;
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				if (latestPayment.TransactionStatus == TransactionStatus.Success)
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Đơn hàng đã được thanh toán thành công trước đó.");
				}

				if (!latestPayment.IsPending())
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Cổng thanh toán đã xác nhận giao dịch.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (paymentInfo.Amount > 0 && latestPayment.Amount != paymentInfo.Amount)
				{
					throw AppException.BadRequest("Số tiền thanh toán không khớp.");
				}

				await CompleteSuccessfulPayment(latestPayment, order, paymentInfo.PaymentLinkId, paymentInfo.PosSessionId);
				return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Đơn hàng đã được thanh toán thành công trước đó.");
			});
		}

		private async Task<BaseResponse<string>?> TryCompleteMomoIfAlreadyPaidAsync(PaymentTransaction payment)
		{
			var queryRequest = new MomoQueryRequest
			{
				PaymentId = payment.Id
			};

			var queryResponse = await _momoService.QueryTransactionAsync(queryRequest);
			if (!queryResponse.IsSuccess)
			{
				return null;
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				if (latestPayment.TransactionStatus == TransactionStatus.Success)
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Đơn hàng đã được thanh toán thành công trước đó.");
				}

				if (!latestPayment.IsPending())
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Cổng thanh toán đã xác nhận giao dịch.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				await CompleteSuccessfulPayment(latestPayment, order, queryResponse.TransactionNo);
				return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Đơn hàng đã được thanh toán thành công trước đó.");
			});
		}

		private async Task<BaseResponse<string>?> TryCompleteVnPayIfAlreadyPaidAsync(PaymentTransaction payment)
		{
			var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext hiện không khả dụng.");
			var queryRequest = new VnPayQueryRequest
			{
				PaymentId = payment.Id,
				OrderInfo = $"Query payment status before retry: {payment.Id}",
				TransactionNo = payment.GatewayTransactionNo,
				TransactionDate = payment.CreatedAt.ToString("yyyyMMddHHmmss")
			};

			var queryResponse = await _vnPayService.QueryTransactionAsync(httpContext, queryRequest);
			if (!queryResponse.IsSuccess)
			{
				return null;
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Không tìm thấy bản ghi thanh toán.");

				if (latestPayment.TransactionStatus == TransactionStatus.Success)
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Đơn hàng đã được thanh toán thành công trước đó.");
				}

				if (!latestPayment.IsPending())
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Cổng thanh toán đã xác nhận giao dịch.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				await CompleteSuccessfulPayment(latestPayment, order, queryResponse.TransactionNo);
				return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Đơn hàng đã được thanh toán thành công trước đó.");
			});
		}

		private async Task<BaseResponse<bool>> CompleteSuccessfulPayment(PaymentTransaction payment, Order order, string? transactionNo = null, string? posSessionId = null)
		{
			payment.MarkSuccess(transactionNo);
			order.RecordPayment(payment.Amount, DateTime.UtcNow);

			if (order.UserVoucher != null)
			{
				await _voucherService.MarkVoucherAsUsedAsync(order.Id);
			}

			// ====================================================================
			// NÂNG CẤP LOGIC CHUYỂN TRẠNG THÁI CHO ĐƠN OFFLINE CÓ CỌC VÀ COD
			// ====================================================================
			if (order.Type == OrderType.Offline)
			{
				if (order.ForwardShippingId == null)
				{
					// Khách Mua và Lấy luôn tại quầy -> Chuyển thành Đã Giao
					if (order.Status != OrderStatus.Delivered)
					{
						order.SetStatus(OrderStatus.Delivered);
						await _stockReservationService.CommitReservationAsync(order.Id);
					}
				}
				else
				{
					// Khách Mua tại quầy, Đã cọc tiền mặt, Giao hàng về nhà (COD)
					// Trạng thái nhảy sang Preparing (Đang chuẩn bị) để kho tiến hành đóng gói
					if (order.Status == OrderStatus.Pending &&
					   (order.PaymentStatus == PaymentStatus.PartialPaid || order.PaymentStatus == PaymentStatus.Paid))
					{
						order.SetStatus(OrderStatus.Pending);
					}
				}
			}

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			var existingReceipt = await _unitOfWork.Receipts.FirstOrDefaultAsync(r => r.TransactionId == payment.Id);
			if (existingReceipt == null)
			{
				var receipt = Receipt.Create(payment.Id);
				await _unitOfWork.Receipts.AddAsync(receipt);
			}

			_backgroundJobService.EnqueueInvoiceEmail(_logger, order.Id);

			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				var successMessage = payment.Method == PaymentMethod.VnPay
					 ? "Thanh toán VNPay thành công"
					 : "Thanh toán thành công";

				try
				{
					await _signalRService.NotifyPosPaymentCompletedAsync(posSessionId, new PosPaymentCompletedDto
					{
						OrderId = order.Id,
						PaymentId = payment.Id,
						Status = "Success",
						Message = successMessage
					});
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not broadcast POS payment completion for order {OrderId} and session {SessionId}", order.Id, posSessionId);
				}
			}

			return BaseResponse<bool>.Ok(true, "Xử lý thanh toán thành công.");
		}

		private async Task<BaseResponse<bool>> HandleFailedPayment(PaymentTransaction payment, Order order, string? reason = null, string? transactionNo = null, string? posSessionId = null)
		{
			payment.MarkFailed(reason, transactionNo);
			//order.MarkUnpaid();

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				var failedMessage = string.IsNullOrWhiteSpace(reason)
					? "Thanh toán thất bại"
					: reason;

				try
				{
					await _signalRService.NotifyPosPaymentFailedAsync(posSessionId, new PosPaymentCompletedDto
					{
						OrderId = order.Id,
						PaymentId = payment.Id,
						Status = "Failed",
						Message = failedMessage
					});
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not broadcast POS payment failure for order {OrderId} and session {SessionId}", order.Id, posSessionId);
				}
			}

			var message = string.IsNullOrEmpty(reason) ? "Thanh toán thất bại." : $"Thanh toán thất bại: {reason}";
			return BaseResponse<bool>.Fail(message, ResponseErrorType.BadRequest);
		}

		private async Task<BaseResponse<string>> GeneratePaymentResponse(PaymentTransaction payment, Order order, int retryAttempt, string methodMessage, string? posSessionId = null)
		{
			switch (payment.Method)
			{
				case PaymentMethod.VnPay:
					var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext hiện không khả dụng.");
					var vnPayRequest = new VnPaymentRequest
					{
						OrderId = order.Id,
						OrderCode = order.Code,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount,
						PosSessionId = posSessionId
					};

					var paymentUrlResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
					return BaseResponse<string>.Ok(paymentUrlResponse.PaymentUrl, $"Đã tạo giao dịch thanh toán #{retryAttempt}{methodMessage}. Đang chuyển hướng đến VnPay.");

				case PaymentMethod.Momo:
					var momoHttpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext hiện không khả dụng.");
					var momoRequest = new MomoPaymentRequest
					{
						OrderId = order.Id,
						OrderCode = order.Code,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount,
						PosSessionId = posSessionId
					};

					var momoUrlResponse = await _momoService.CreatePaymentUrlAsync(momoHttpContext, momoRequest);
					return BaseResponse<string>.Ok(momoUrlResponse.PaymentUrl, $"Đã tạo giao dịch thanh toán #{retryAttempt}{methodMessage}. Đang chuyển hướng đến Momo.");

				case PaymentMethod.PayOs:
					var payOsRequest = new PayOsPaymentRequest
					{
						OrderId = order.Id,
						OrderCode = order.Code,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount,
						PosSessionId = posSessionId
					};

					var payOsUrlResponse = await _payOsService.CreatePaymentUrlAsync(payOsRequest);
					return BaseResponse<string>.Ok(payOsUrlResponse.PaymentUrl, $"Đã tạo giao dịch thanh toán #{retryAttempt}{methodMessage}. Đang chuyển hướng đến PayOs.");

				case PaymentMethod.CashOnDelivery:
				case PaymentMethod.CashInStore:
					return BaseResponse<string>.Ok(payment.Id.ToString(), $"Đã tạo giao dịch thanh toán #{retryAttempt}{methodMessage}. Thanh toán tiền mặt đang chờ xác nhận.");

				default:
					throw AppException.BadRequest("Phương thức thanh toán không được hỗ trợ.");
			}
		}

		private static DateTime? GetPaymentExpiration(PaymentMethod method)
		{
			return method switch
			{
				PaymentMethod.VnPay => DateTime.UtcNow.AddMinutes(15),
				PaymentMethod.Momo => DateTime.UtcNow.AddMinutes(30),
				PaymentMethod.PayOs => DateTime.UtcNow.AddMinutes(30),
				PaymentMethod.CashOnDelivery => null,
				PaymentMethod.CashInStore => null,
				_ => null
			};
		}
	}
}