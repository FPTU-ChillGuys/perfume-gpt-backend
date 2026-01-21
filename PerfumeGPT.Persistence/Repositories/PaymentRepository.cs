using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class PaymentRepository : GenericRepository<PaymentTransaction>, IPaymentRepository
	{
		public PaymentRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
