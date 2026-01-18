using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _profileRepo;
        private readonly IValidator<CreateProfileRequest> _createProfileValidator;
        private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
        private readonly IMapper _mapper;

        public ProfileService(IProfileRepository profileRepo, IValidator<CreateProfileRequest> createProfileValidator, IMapper mapper, IValidator<UpdateProfileRequest> updateProfileValidator)
        {
            _profileRepo = profileRepo;
            _createProfileValidator = createProfileValidator;
            _mapper = mapper;
            _updateProfileValidator = updateProfileValidator;
        }

        public async Task<BaseResponse<string>> CreateProfileAsync(CreateProfileRequest request)
        {
            var validationResult = await _createProfileValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
            }

            // prevent duplicate profile for same user
            var exists = await _profileRepo.AnyAsync(p => p.UserId == request.UserId);
            if (exists)
                return BaseResponse<string>.Fail("Profile already exists for this user", ResponseErrorType.Conflict);

            var profile = _mapper.Map<CustomerProfile>(request);
            profile.CreatedAt = DateTime.UtcNow;

            await _profileRepo.AddAsync(profile);
            var saved = await _profileRepo.SaveChangesAsync();
            if (!saved)
                return BaseResponse<string>.Fail("Failed to create profile", ResponseErrorType.InternalError);

            return BaseResponse<string>.Ok(profile.Id.ToString(), "Profile created successfully");
        }

        public async Task<BaseResponse<string>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            // Validate request
            var validationResult = await _updateProfileValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
            }

            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
                return BaseResponse<string>.Fail("Profile not found", ResponseErrorType.NotFound);

            // Map only allowed fields
            _mapper.Map(request, profile);

            _profileRepo.Update(profile);
            var saved = await _profileRepo.SaveChangesAsync();
            if (!saved)
                return BaseResponse<string>.Fail("Failed to update profile", ResponseErrorType.InternalError);

            return BaseResponse<string>.Ok(profile.Id.ToString(), "Profile updated successfully");
        }
    }
}
