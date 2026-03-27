using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ProfileService : IProfileService
	{
		private readonly IProfileRepository _profileRepo;
		private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
		private readonly IMapper _mapper;

		public ProfileService(
				IProfileRepository profileRepo,
				IMapper mapper,
				IValidator<UpdateProfileRequest> updateProfileValidator)
		{
			_profileRepo = profileRepo;
			_mapper = mapper;
			_updateProfileValidator = updateProfileValidator;
		}

		private async Task<CustomerProfile> CreateProfileAsync(Guid userId)
		{
			var existingProfile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
			if (existingProfile is not null)
				return existingProfile;

			var profile = CustomerProfile.Create(userId);

			await _profileRepo.AddAsync(profile);
			var saved = await _profileRepo.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to create profile");

			return profile;
		}

		public async Task<BaseResponse<string>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
		{
			var validationResult = await _updateProfileValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw AppException.BadRequest("Validation failed", errors);
			}

			var profile = await _profileRepo.GetByUserIdWithPreferencesAsync(userId) ?? throw AppException.NotFound("Profile not found");

			var noteIds = request.NotePreferenceIds?.Distinct().ToList() ?? [];
			var familyIds = request.FamilyPreferenceIds?.Distinct().ToList() ?? [];
			var attributeIds = request.AttributePreferenceIds?.Distinct().ToList() ?? [];

			await ValidatePreferenceIdsAsync(noteIds, familyIds, attributeIds);

			profile.UpdateBasicInfo(request.DateOfBirth, request.MinBudget, request.MaxBudget);
			profile.UpdatePreferences(noteIds, familyIds, attributeIds);

			_profileRepo.Update(profile);
			var saved = await _profileRepo.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to update profile");

			return BaseResponse<string>.Ok(profile.Id.ToString(), "Profile updated successfully");
		}

		public async Task<BaseResponse<ProfileResponse>> GetProfileAsync(Guid userId)
		{
			var profile = await CreateProfileAsync(userId);

			var response = _mapper.Map<ProfileResponse>(profile);
			return BaseResponse<ProfileResponse>.Ok(response, "Profile retrieved successfully");
		}

		private async Task ValidatePreferenceIdsAsync(
			List<int> noteIds,
			List<int> familyIds,
			List<int> attributeIds)
		{
			if (noteIds.Count > 0)
			{
				var missingNoteIds = await _profileRepo.GetMissingNoteIdsAsync(noteIds);
				if (missingNoteIds.Count > 0)
				{
					throw AppException.BadRequest($"Invalid note preference IDs: {string.Join(", ", missingNoteIds)}");
				}
			}

			if (familyIds.Count > 0)
			{
				var missingFamilyIds = await _profileRepo.GetMissingFamilyIdsAsync(familyIds);
				if (missingFamilyIds.Count > 0)
				{
					throw AppException.BadRequest($"Invalid family preference IDs: {string.Join(", ", missingFamilyIds)}");
				}
			}

			if (attributeIds.Count > 0)
			{
				var missingAttributeIds = await _profileRepo.GetMissingAttributeValueIdsAsync(attributeIds);
				if (missingAttributeIds.Count > 0)
				{
					throw AppException.BadRequest($"Invalid attribute preference IDs: {string.Join(", ", missingAttributeIds)}");
				}
			}
		}
	}
}
