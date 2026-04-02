using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class PaymentTransactionRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<PaymentTransaction, PaymentInfoResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.TotalAmount, src => src.Amount)
				.Map(dest => dest.PaymentMethod, src => src.Method)
				.Map(dest => dest.FailureReason, src => src.FailureReason)
				.Map(dest => dest.Status, src => src.TransactionStatus)
				.Map(dest => dest.TransactionType, src => src.TransactionType);
		}
	}
}
