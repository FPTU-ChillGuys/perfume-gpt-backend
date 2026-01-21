using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ImportDetailRepository : GenericRepository<ImportDetail>, IImportDetailRepository
	{
		public ImportDetailRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
