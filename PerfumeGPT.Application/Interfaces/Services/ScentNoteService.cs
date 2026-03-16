using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ScentNotes;
using PerfumeGPT.Application.DTOs.Requests.ScentNotes;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using Mapster;
using MapsterMapper;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public class ScentNoteService : IScentNoteService
	{
		private readonly IScentNoteRepository _scentNoteRepository;
		private readonly IMapper _mapper;

		public ScentNoteService(IScentNoteRepository scentNoteRepository, IMapper mapper)
		{
			_scentNoteRepository = scentNoteRepository;
			_mapper = mapper;
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
			var result = await _scentNoteRepository.GetScentNoteByIdAsync(id);
			if (result == null)
				return BaseResponse<ScentNoteResponse>.Fail("ScentNote not found", ResponseErrorType.NotFound);
			return BaseResponse<ScentNoteResponse>.Ok(result);
		}

		public async Task<BaseResponse<ScentNoteResponse>> CreateScentNoteAsync(CreateScentNoteRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _scentNoteRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				return BaseResponse<ScentNoteResponse>.Fail("ScentNote name already exists.", ResponseErrorType.Conflict);

			var entity = _mapper.Map<ScentNote>(request);
			entity.Name = normalizedName;

			await _scentNoteRepository.AddAsync(entity);
			await _scentNoteRepository.SaveChangesAsync();
			return BaseResponse<ScentNoteResponse>.Ok(entity.Adapt<ScentNoteResponse>());
		}

		public async Task<BaseResponse<ScentNoteResponse>> UpdateScentNoteAsync(int id, UpdateScentNoteRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _scentNoteRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				return BaseResponse<ScentNoteResponse>.Fail("ScentNote name already exists.", ResponseErrorType.Conflict);

			var entity = await _scentNoteRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<ScentNoteResponse>.Fail("ScentNote not found", ResponseErrorType.NotFound);

			_mapper.Map(request, entity);
			entity.Name = normalizedName;

			_scentNoteRepository.Update(entity);
			await _scentNoteRepository.SaveChangesAsync();

			return BaseResponse<ScentNoteResponse>.Ok(entity.Adapt<ScentNoteResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteScentNoteAsync(int id)
		{
			var entity = await _scentNoteRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<bool>.Fail("ScentNote not found", ResponseErrorType.NotFound);

			if (entity.ProductScentNoteMaps.Count != 0 || entity.CustomerScentNotePreferences.Count != 0)
				return BaseResponse<bool>.Fail("Cannot delete ScentNote that is associated with products.", ResponseErrorType.BadRequest);

			_scentNoteRepository.Remove(entity);
			await _scentNoteRepository.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}
	}
}
