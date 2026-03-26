using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;
using PerfumeGPT.Application.Interfaces.Services;
using FluentValidation;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes;

namespace PerfumeGPT.Application.Services
{
	public class ScentNoteService : IScentNoteService
	{
		private readonly IScentNoteRepository _scentNoteRepository;
		private readonly IValidator<CreateScentNoteRequest> _createValidator;
		private readonly IValidator<UpdateScentNoteRequest> _updateValidator;

		public ScentNoteService(IScentNoteRepository scentNoteRepository, IValidator<UpdateScentNoteRequest> updateValidator, IValidator<CreateScentNoteRequest> createValidator)
		{
			_scentNoteRepository = scentNoteRepository;
			_updateValidator = updateValidator;
			_createValidator = createValidator;
		}

		public async Task<BaseResponse<List<ScentNoteLookupResponse>>> GetScentNoteLookupListAsync()
		{
			return BaseResponse<List<ScentNoteLookupResponse>>.Ok(
				await _scentNoteRepository.GetScentNoteLookupListAsync()
			);
		}

		public async Task<BaseResponse<List<ScentNoteResponse>>> GetAllScentNotesAsync()
		{
			var result = await _scentNoteRepository.GetAllScentNotesAsync();
			return BaseResponse<List<ScentNoteResponse>>.Ok(result);
		}

		public async Task<BaseResponse<ScentNoteResponse>> GetScentNoteByIdAsync(int id)
		{
			var result = await _scentNoteRepository.GetScentNoteByIdAsync(id) ?? throw AppException.NotFound("ScentNote not found");
			return BaseResponse<ScentNoteResponse>.Ok(result);
		}

		public async Task<BaseResponse<ScentNoteResponse>> CreateScentNoteAsync(CreateScentNoteRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw AppException.BadRequest("Invalid request", errors);
			}
			var normalizedName = request.Name.Trim();

			var exists = await _scentNoteRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				throw AppException.Conflict("ScentNote name already exists.");

			var entity = ScentNote.Create(request.Name);

			await _scentNoteRepository.AddAsync(entity);
			var saved = await _scentNoteRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not create ScentNote.");
			return BaseResponse<ScentNoteResponse>.Ok(entity.Adapt<ScentNoteResponse>());
		}

		public async Task<BaseResponse<ScentNoteResponse>> UpdateScentNoteAsync(int id, UpdateScentNoteRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw AppException.BadRequest("Invalid request", errors);
			}

			var normalizedName = request.Name.Trim();

			var exists = await _scentNoteRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				throw AppException.Conflict("ScentNote name already exists.");

			var entity = await _scentNoteRepository.GetByIdAsync(id) ?? throw AppException.NotFound("ScentNote not found");
			entity.Rename(request.Name);

			_scentNoteRepository.Update(entity);
			var saved = await _scentNoteRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not update ScentNote.");

			return BaseResponse<ScentNoteResponse>.Ok(entity.Adapt<ScentNoteResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteScentNoteAsync(int id)
		{
			var entity = await _scentNoteRepository.GetByIdAsync(id) ?? throw AppException.NotFound("ScentNote not found");
			var hasAssociations = await _scentNoteRepository.HasAssociationsAsync(id);
			ScentNote.EnsureCanDelete(hasAssociations);

			_scentNoteRepository.Remove(entity);
			var saved = await _scentNoteRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not delete ScentNote.");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
