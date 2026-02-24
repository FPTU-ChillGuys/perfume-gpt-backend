using System.Threading.Tasks;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByPhoneNumberAsync(string phoneNumber);
    }
}
