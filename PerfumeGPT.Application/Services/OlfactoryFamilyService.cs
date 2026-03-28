using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Services;
using FluentValidation;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;

namespace PerfumeGPT.Application.Services
{
	public class OlfactoryFamilyService : IOlfactoryFamilyService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateOlfactoryFamilyRequest> _createValidator;
		private readonly IValidator<UpdateOlfactoryFamilyRequest> _updateValidator;
		public OlfactoryFamilyService(IOlfactoryFamilyRepository olfactoryFamilyRepository, IValidator<CreateOlfactoryFamilyRequest> createValidator, IValidator<UpdateOlfactoryFamilyRequest> updateValidator, IUnitOfWork unitOfWork)
		{
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync()
		{
			return BaseResponse<List<OlfactoryLookupResponse>>.Ok(
				await _unitOfWork.OlfactoryFamilies.GetOlfactoryFamilyLookupListAsync()
			);
		}

		public async Task<BaseResponse<List<OlfactoryFamilyResponse>>> GetAllOlfactoryFamiliesAsync()
		{
			var result = await _unitOfWork.OlfactoryFamilies.GetAllOlfactoryFamiliesAsync();
			return BaseResponse<List<OlfactoryFamilyResponse>>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> GetOlfactoryFamilyByIdAsync(int id)
		{
			var result = await _unitOfWork.OlfactoryFamilies.GetOlfactoryFamilyByIdAsync(id);
			return result == null
				? throw AppException.NotFound("OlfactoryFamily not found")
				: BaseResponse<OlfactoryFamilyResponse>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> CreateOlfactoryFamilyAsync(CreateOlfactoryFamilyRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Invalid request", [.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = request.Name.Trim();

			var exists = await _unitOfWork.OlfactoryFamilies.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				throw AppException.Conflict("OlfactoryFamily name already exists.");

			var entity = OlfactoryFamily.Create(request.Name);

			await _unitOfWork.OlfactoryFamilies.AddAsync(entity);
			var saved = await _unitOfWork.OlfactoryFamilies.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not create OlfactoryFamily.");
			return BaseResponse<OlfactoryFamilyResponse>.Ok(entity.Adapt<OlfactoryFamilyResponse>());
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> UpdateOlfactoryFamilyAsync(int id, UpdateOlfactoryFamilyRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Invalid request", [.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = request.Name.Trim();

			var exists = await _unitOfWork.OlfactoryFamilies.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				throw AppException.Conflict("OlfactoryFamily name already exists.");

			var entity = await _unitOfWork.OlfactoryFamilies.GetByIdAsync(id) ?? throw AppException.NotFound("OlfactoryFamily not found");
			entity.Rename(request.Name);

			_unitOfWork.OlfactoryFamilies.Update(entity);
			var saved = await _unitOfWork.OlfactoryFamilies.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not update OlfactoryFamily.");

			return BaseResponse<OlfactoryFamilyResponse>.Ok(entity.Adapt<OlfactoryFamilyResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteOlfactoryFamilyAsync(int id)
		{
			var entity = await _unitOfWork.OlfactoryFamilies.GetByIdAsync(id) ?? throw AppException.NotFound("OlfactoryFamily not found");
			var hasAssociations = await _unitOfWork.OlfactoryFamilies.HasAssociationsAsync(id);
			OlfactoryFamily.EnsureCanDelete(hasAssociations);

			_unitOfWork.OlfactoryFamilies.Remove(entity);
			var saved = await _unitOfWork.OlfactoryFamilies.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not delete OlfactoryFamily.");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
