using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ProfileService : IProfileService
	{
		private readonly IUnitOfWork _unitOfWork;

		public ProfileService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		private async Task<CustomerProfile> CreateProfileAsync(Guid userId)
		{
			var existingProfile = await _unitOfWork.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
			if (existingProfile is not null)
				return existingProfile;

			var profile = CustomerProfile.Create(userId);

			await _unitOfWork.Profiles.AddAsync(profile);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to create profile");

			return profile;
		}

		public async Task<BaseResponse<string>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
		{
			var profile = await _unitOfWork.Profiles.GetByUserIdWithPreferencesAsync(userId)
				?? throw AppException.NotFound("Profile not found");

			var notePreferences = request.NotePreferenceIds?
				   .Select(x => (x.NoteId, x.NoteType))
				   .Distinct()
				   .ToList() ?? [];
			var noteIds = notePreferences.Select(x => x.NoteId).Distinct().ToList();
			var familyIds = request.FamilyPreferenceIds?.Distinct().ToList() ?? [];
			var attributeIds = request.AttributePreferenceIds?.Distinct().ToList() ?? [];

			await ValidatePreferenceIdsAsync(noteIds, familyIds, attributeIds);

			profile.UpdateBasicInfo(request.DateOfBirth, request.MinBudget, request.MaxBudget);
			profile.UpdateNotePreferences(notePreferences);
			profile.UpdateFamilyPreferences(familyIds);
			profile.UpdateAttributePreferences(attributeIds);

			_unitOfWork.Profiles.Update(profile);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to update profile");

			return BaseResponse<string>.Ok(profile.Id.ToString(), "Profile updated successfully");
		}

		public async Task<BaseResponse<ProfileResponse>> GetProfileAsync(Guid userId)
		{
			await CreateProfileAsync(userId);

			var profile = await _unitOfWork.Profiles.GetProfileResponseByUserIdAsync(userId)
				?? throw AppException.NotFound("Profile not found");

			return BaseResponse<ProfileResponse>.Ok(profile, "Profile retrieved successfully");
		}

		private async Task ValidatePreferenceIdsAsync(
			List<int> noteIds,
			List<int> familyIds,
			List<int> attributeIds)
		{
			if (noteIds.Count > 0)
			{
				var missingNoteIds = await _unitOfWork.Profiles.GetMissingNoteIdsAsync(noteIds);
				if (missingNoteIds.Count > 0)
				{
					throw AppException.BadRequest($"Invalid note preference IDs: {string.Join(", ", missingNoteIds)}");
				}
			}

			if (familyIds.Count > 0)
			{
				var missingFamilyIds = await _unitOfWork.Profiles.GetMissingFamilyIdsAsync(familyIds);
				if (missingFamilyIds.Count > 0)
				{
					throw AppException.BadRequest($"Invalid family preference IDs: {string.Join(", ", missingFamilyIds)}");
				}
			}

			if (attributeIds.Count > 0)
			{
				var missingAttributeIds = await _unitOfWork.Profiles.GetMissingAttributeValueIdsAsync(attributeIds);
				if (missingAttributeIds.Count > 0)
				{
					throw AppException.BadRequest($"Invalid attribute preference IDs: {string.Join(", ", missingAttributeIds)}");
				}
			}
		}
	}
}
