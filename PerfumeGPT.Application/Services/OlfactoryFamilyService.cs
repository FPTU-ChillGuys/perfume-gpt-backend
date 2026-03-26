using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Services;
using FluentValidation;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies;

namespace PerfumeGPT.Application.Services
{
	public class OlfactoryFamilyService : IOlfactoryFamilyService
	{
		#region Dependencies
		private readonly IOlfactoryFamilyRepository _olfactoryFamilyRepository;
		private readonly IValidator<CreateOlfactoryFamilyRequest> _createValidator;
		private readonly IValidator<UpdateOlfactoryFamilyRequest> _updateValidator;
		public OlfactoryFamilyService(IOlfactoryFamilyRepository olfactoryFamilyRepository, IValidator<CreateOlfactoryFamilyRequest> createValidator, IValidator<UpdateOlfactoryFamilyRequest> updateValidator)
		{
			_olfactoryFamilyRepository = olfactoryFamilyRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync()
		{
			return BaseResponse<List<OlfactoryLookupResponse>>.Ok(
				await _olfactoryFamilyRepository.GetOlfactoryFamilyLookupListAsync()
			);
		}

		public async Task<BaseResponse<List<OlfactoryFamilyResponse>>> GetAllOlfactoryFamiliesAsync()
		{
			var result = await _olfactoryFamilyRepository.GetAllOlfactoryFamiliesAsync();
			return BaseResponse<List<OlfactoryFamilyResponse>>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> GetOlfactoryFamilyByIdAsync(int id)
		{
			var result = await _olfactoryFamilyRepository.GetOlfactoryFamilyByIdAsync(id);
			if (result == null)
				throw AppException.NotFound("OlfactoryFamily not found");
			return BaseResponse<OlfactoryFamilyResponse>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> CreateOlfactoryFamilyAsync(CreateOlfactoryFamilyRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Invalid request", [.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = request.Name.Trim();

			var exists = await _olfactoryFamilyRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				throw AppException.Conflict("OlfactoryFamily name already exists.");

			var entity = OlfactoryFamily.Create(request.Name);

			await _olfactoryFamilyRepository.AddAsync(entity);
			var saved = await _olfactoryFamilyRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not create OlfactoryFamily.");
			return BaseResponse<OlfactoryFamilyResponse>.Ok(entity.Adapt<OlfactoryFamilyResponse>());
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> UpdateOlfactoryFamilyAsync(int id, UpdateOlfactoryFamilyRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Invalid request", [.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = request.Name.Trim();

			var exists = await _olfactoryFamilyRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				throw AppException.Conflict("OlfactoryFamily name already exists.");

			var entity = await _olfactoryFamilyRepository.GetByIdAsync(id) ?? throw AppException.NotFound("OlfactoryFamily not found");
			entity.Rename(request.Name);

			_olfactoryFamilyRepository.Update(entity);
			var saved = await _olfactoryFamilyRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not update OlfactoryFamily.");

			return BaseResponse<OlfactoryFamilyResponse>.Ok(entity.Adapt<OlfactoryFamilyResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteOlfactoryFamilyAsync(int id)
		{
			var entity = await _olfactoryFamilyRepository.GetByIdAsync(id) ?? throw AppException.NotFound("OlfactoryFamily not found");
			var hasAssociations = await _olfactoryFamilyRepository.HasAssociationsAsync(id);
			OlfactoryFamily.EnsureCanDelete(hasAssociations);

			_olfactoryFamilyRepository.Remove(entity);
			var saved = await _olfactoryFamilyRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not delete OlfactoryFamily.");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
