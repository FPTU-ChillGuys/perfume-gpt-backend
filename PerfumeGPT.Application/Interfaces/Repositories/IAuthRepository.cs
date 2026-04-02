using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAuthRepository
	{
		public string GenerateJwtToken(User user, string role);
	}
}
