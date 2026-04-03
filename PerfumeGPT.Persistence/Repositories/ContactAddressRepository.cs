using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ContactAddressRepository : GenericRepository<ContactAddress>, IContactAddressRepository
	{
		public ContactAddressRepository(PerfumeDbContext context) : base(context) { }
	}
}
