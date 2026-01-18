using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
    public class LoyaltyPointService : ILoyaltyPointService
    {
        private readonly IValidator<CreateLoyaltyPointRequest> _createLoyaltyPointValidator;
        private readonly IValidator<UpdateLoyaltyPointRequest> _updateLoyaltyPointValidator;
        private readonly ILoyaltyPointRepository _loyaltyPointRepository;
        private readonly IMapper _mapper;

        public LoyaltyPointService(IValidator<CreateLoyaltyPointRequest> createLoyaltyPointValidator, IValidator<UpdateLoyaltyPointRequest> updateLoyaltyPointValidator, ILoyaltyPointRepository loyaltyPointRepository, IMapper mapper)
        {
            _createLoyaltyPointValidator = createLoyaltyPointValidator;
            _updateLoyaltyPointValidator = updateLoyaltyPointValidator;
            _loyaltyPointRepository = loyaltyPointRepository;
            _mapper = mapper;
        }

        public async Task<string> CreateLoyaltyPointAsync(CreateLoyaltyPointRequest request)
        {
            var validationResult = await _createLoyaltyPointValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return string.Empty;
            }

            // prevent duplicate loyalty point record for same user
            var existing = await _loyaltyPointRepository.FirstOrDefaultAsync(lp => lp.UserId == request.UserId);
            if (existing != null)
                return existing.Id.ToString();

            var entity = _mapper.Map<LoyaltyPoint>(request);
            entity.CreatedAt = DateTime.UtcNow;

            await _loyaltyPointRepository.AddAsync(entity);
            var saved = await _loyaltyPointRepository.SaveChangesAsync();
            if (!saved)
                return string.Empty;

            return entity.Id.ToString();
        }

        public async Task<int> PlusPointAsync(Guid userId, int points)
        {
            if (userId == Guid.Empty) throw new ArgumentException("userId is required", nameof(userId));
            if (points <= 0) return 0;

            var existing = await _loyaltyPointRepository.FirstOrDefaultAsync(lp => lp.UserId == userId);
            if (existing == null)
            {
                var newLp = new LoyaltyPoint
                {
                    UserId = userId,
                    PointBalance = points
                };

                await _loyaltyPointRepository.AddAsync(newLp);
                var saved = await _loyaltyPointRepository.SaveChangesAsync();
                return saved ? newLp.PointBalance : 0;
            }

            existing.PointBalance += points;
            _loyaltyPointRepository.Update(existing);
            var ok = await _loyaltyPointRepository.SaveChangesAsync();
            return ok ? existing.PointBalance : 0;
        }

        public async Task<int> UpdateLoyaltyPointAsync(Guid userId, UpdateLoyaltyPointRequest request)
        {
            var validationResult = await _updateLoyaltyPointValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return 0;
            }
            if (userId == Guid.Empty) throw new ArgumentException("userId is required", nameof(userId));

            var existing = await _loyaltyPointRepository.FirstOrDefaultAsync(lp => lp.UserId == userId);
            if (existing == null)
            {
                var newLp = new LoyaltyPoint
                {
                    UserId = userId,
                    PointBalance = request.PointBalance
                };
                await _loyaltyPointRepository.AddAsync(newLp);
                var saved = await _loyaltyPointRepository.SaveChangesAsync();
                return saved ? newLp.PointBalance : 0;
            }

            existing.PointBalance = request.PointBalance;
            _loyaltyPointRepository.Update(existing);
            var ok = await _loyaltyPointRepository.SaveChangesAsync();
            return ok ? existing.PointBalance : 0;
        }
    }
}
