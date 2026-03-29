using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Domain.Entities;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;
using PerfumeGPT.Application.Interfaces.Services;
using FluentValidation;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;

namespace PerfumeGPT.Application.Services
{
	public class ScentNoteService : IScentNoteService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateScentNoteRequest> _createValidator;
		private readonly IValidator<UpdateScentNoteRequest> _updateValidator;

		public ScentNoteService(IValidator<UpdateScentNoteRequest> updateValidator, IValidator<CreateScentNoteRequest> createValidator, IUnitOfWork unitOfWork)
		{
			_updateValidator = updateValidator;
			_createValidator = createValidator;
			_unitOfWork = unitOfWork;
		}

		public async Task<BaseResponse<List<ScentNoteLookupResponse>>> GetScentNoteLookupListAsync()
		{
			return BaseResponse<List<ScentNoteLookupResponse>>.Ok(
				await _unitOfWork.ScentNotes.GetScentNoteLookupListAsync()
			);
		}

		public async Task<BaseResponse<List<ScentNoteResponse>>> GetAllScentNotesAsync()
		{
			var result = await _unitOfWork.ScentNotes.GetAllScentNotesAsync();
			return BaseResponse<List<ScentNoteResponse>>.Ok(result);
		}

		public async Task<BaseResponse<ScentNoteResponse>> GetScentNoteByIdAsync(int id)
		{
			var result = await _unitOfWork.ScentNotes.GetScentNoteByIdAsync(id) ?? throw AppException.NotFound("ScentNote not found");
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

			var exists = await _unitOfWork.ScentNotes.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				throw AppException.Conflict("ScentNote name already exists.");

			var entity = ScentNote.Create(request.Name);

			await _unitOfWork.ScentNotes.AddAsync(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
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

			var exists = await _unitOfWork.ScentNotes.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				throw AppException.Conflict("ScentNote name already exists.");

			var entity = await _unitOfWork.ScentNotes.GetByIdAsync(id) ?? throw AppException.NotFound("ScentNote not found");
			entity.Rename(request.Name);

			_unitOfWork.ScentNotes.Update(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not update ScentNote.");

			return BaseResponse<ScentNoteResponse>.Ok(entity.Adapt<ScentNoteResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteScentNoteAsync(int id)
		{
			var entity = await _unitOfWork.ScentNotes.GetByIdAsync(id) ?? throw AppException.NotFound("ScentNote not found");
			var hasAssociations = await _unitOfWork.ScentNotes.HasAssociationsAsync(id);
			ScentNote.EnsureCanDelete(hasAssociations);

			_unitOfWork.ScentNotes.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not delete ScentNote.");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
