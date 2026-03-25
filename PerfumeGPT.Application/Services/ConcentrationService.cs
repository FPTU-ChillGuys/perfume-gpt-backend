using FluentValidation;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ConcentrationService : IConcentrationService
	{
		#region Dependencies
		private readonly IConcentrationRepository _concentrationRepository;
		private readonly IValidator<CreateConcentrationRequest> _createValidator;
		private readonly IValidator<UpdateConcentrationRequest> _updateValidator;

		public ConcentrationService(
			 IConcentrationRepository concentrationRepository,
			 IValidator<CreateConcentrationRequest> createValidator,
			 IValidator<UpdateConcentrationRequest> updateValidator)
		{
			_concentrationRepository = concentrationRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<ConcentrationLookupDto>>> GetConcentrationLookup()
		{
			return BaseResponse<List<ConcentrationLookupDto>>.Ok(await _concentrationRepository.GetConcentrationLookupsAsync());
		}

		public async Task<BaseResponse<ConcentrationResponse>> GetConcentrationByIdAsync(int id)
		{
			var result = await _concentrationRepository.GetConcentrationByIdAsync(id)
				  ?? throw AppException.NotFound("Concentration not found");

			return BaseResponse<ConcentrationResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<ConcentrationResponse>>> GetAllConcentrationsAsync()
		{
			var result = await _concentrationRepository.GetAllConcentrationsAsync();
			return BaseResponse<List<ConcentrationResponse>>.Ok(result);
		}

		public async Task<BaseResponse<ConcentrationResponse>> CreateConcentrationAsync(CreateConcentrationRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = Concentration.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _concentrationRepository.AnyAsync(c => c.Name.ToUpper() == normalizedName);
			if (exists)
				throw AppException.Conflict("Concentration name already exists.");

			var entity = Concentration.Create(normalizedName);
			await _concentrationRepository.AddAsync(entity);

			var saved = await _concentrationRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create concentration");

			return BaseResponse<ConcentrationResponse>.Ok(entity.Adapt<ConcentrationResponse>());
		}

		public async Task<BaseResponse<ConcentrationResponse>> UpdateConcentrationAsync(int id, UpdateConcentrationRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = await _concentrationRepository.GetByIdAsync(id)
				?? throw AppException.NotFound("Concentration not found");

			var normalizedName = Concentration.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _concentrationRepository.AnyAsync(c => c.Id != id && c.Name.ToUpper() == normalizedName);
			if (exists)
				throw AppException.Conflict("Concentration name already exists.");

			entity.Rename(normalizedName);
			_concentrationRepository.Update(entity);

			var saved = await _concentrationRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update concentration");

			return BaseResponse<ConcentrationResponse>.Ok(entity.Adapt<ConcentrationResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteConcentrationAsync(int id)
		{
			var entity = await _concentrationRepository.GetByIdAsync(id)
				?? throw AppException.NotFound("Concentration not found");

			var hasVariants = await _concentrationRepository.HasVariantsAsync(id);
			Concentration.EnsureCanBeDeleted(hasVariants);

			_concentrationRepository.Remove(entity);

			var saved = await _concentrationRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete concentration");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
