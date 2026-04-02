using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CampaignsController : BaseApiController
	{
		private readonly ICampaignService _campaignService;
		private readonly IValidator<CreateCampaignRequest> _createCampaignValidator;
		private readonly IValidator<UpdateCampaignRequest> _updateCampaignValidator;
		private readonly IValidator<CreateCampaignPromotionItemRequest> _createCampaignPromotionItemValidator;
		private readonly IValidator<UpdateCampaignPromotionItemRequest> _updateCampaignPromotionItemValidator;
		private readonly IValidator<CreateCampaignVoucherRequest> _createCampaignVoucherValidator;
		private readonly IValidator<UpdateCampaignVoucherRequest> _updateCampaignVoucherValidator;

		public CampaignsController(ICampaignService campaignService,
			IValidator<CreateCampaignRequest> createCampaignValidator,
			IValidator<UpdateCampaignRequest> updateCampaignValidator,
			IValidator<CreateCampaignPromotionItemRequest> createCampaignPromotionItemValidator,
			IValidator<CreateCampaignVoucherRequest> createCampaignVoucherValidator,
			IValidator<UpdateCampaignVoucherRequest> updateCampaignVoucherValidator,
			IValidator<UpdateCampaignPromotionItemRequest> updateCampaignPromotionItemValidator)
		{
			_campaignService = campaignService;
			_createCampaignValidator = createCampaignValidator;
			_updateCampaignValidator = updateCampaignValidator;
			_createCampaignPromotionItemValidator = createCampaignPromotionItemValidator;
			_createCampaignVoucherValidator = createCampaignVoucherValidator;
			_updateCampaignVoucherValidator = updateCampaignVoucherValidator;
			_updateCampaignPromotionItemValidator = updateCampaignPromotionItemValidator;
		}

		[HttpPost]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateCampaign([FromBody] CreateCampaignRequest request)
		{
			var validation = await ValidateRequestAsync(_createCampaignValidator, request);
			if (validation != null) return validation;

			var response = await _campaignService.CreateCampaignAsync(request);
			return HandleResponse(response);
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<CampaignResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<CampaignResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<CampaignResponse>>>> GetCampaigns([FromQuery] GetPagedCampaignsRequest request)
		{
			var response = await _campaignService.GetPagedCampaignsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{campaignId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<CampaignResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<CampaignResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<CampaignResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<CampaignResponse>>> GetCampaignById([FromRoute] Guid campaignId)
		{
			var response = await _campaignService.GetCampaignByIdAsync(campaignId);
			return HandleResponse(response);
		}

		[HttpGet("{campaignId:guid}/items")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<List<CampaignPromotionItemResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<CampaignPromotionItemResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<CampaignPromotionItemResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<CampaignPromotionItemResponse>>>> GetCampaignItemsByCampaignId([FromRoute] Guid campaignId)
		{
			var response = await _campaignService.GetCampaignItemsByCampaignIdAsync(campaignId);
			return HandleResponse(response);
		}

		[HttpPut("{campaignId:guid}/status")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaignStatus([FromRoute] Guid campaignId, [FromBody] UpdateCampaignStatusRequest request)
		{
			var validation = ValidateRequestBody<UpdateCampaignStatusRequest>(request);
			if (validation != null) return validation;

			var response = await _campaignService.UpdateCampaignStatusAsync(campaignId, request);
			return HandleResponse(response);
		}

		[HttpPut("{campaignId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaign([FromRoute] Guid campaignId, [FromBody] UpdateCampaignRequest request)
		{
			var validation = await ValidateRequestAsync(_updateCampaignValidator, request);
			if (validation != null) return validation;

			var response = await _campaignService.UpdateCampaignAsync(campaignId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{campaignId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCampaign([FromRoute] Guid campaignId)
		{
			var response = await _campaignService.DeleteCampaignAsync(campaignId);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/items")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> AddCampaignItem([FromRoute] Guid id, [FromBody] CreateCampaignPromotionItemRequest request)
		{
			var validation = await ValidateRequestAsync(_createCampaignPromotionItemValidator, request);
			if (validation != null) return validation;

			var response = await _campaignService.AddCampaignItemAsync(id, request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/items/{itemId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaignItem([FromRoute] Guid id, [FromRoute] Guid itemId, [FromBody] UpdateCampaignPromotionItemRequest request)
		{
			var validation = await ValidateRequestAsync(_updateCampaignPromotionItemValidator, request);
			if (validation != null) return validation;

			var response = await _campaignService.UpdateCampaignItemAsync(id, itemId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}/items/{itemId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCampaignItem([FromRoute] Guid id, [FromRoute] Guid itemId)
		{
			var response = await _campaignService.DeleteCampaignItemAsync(id, itemId);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/vouchers")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> AddCampaignVoucher([FromRoute] Guid id, [FromBody] CreateCampaignVoucherRequest request)
		{
			var validation = await ValidateRequestAsync(_createCampaignVoucherValidator, request);
			if (validation != null) return validation;

			var response = await _campaignService.AddCampaignVoucherAsync(id, request);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}/vouchers/{voucherId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<VoucherResponse>>> GetCampaignVoucherById([FromRoute] Guid id, [FromRoute] Guid voucherId)
		{
			var response = await _campaignService.GetCampaignVoucherByIdAsync(id, voucherId);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/vouchers/{voucherId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaignVoucher([FromRoute] Guid id, [FromRoute] Guid voucherId, [FromBody] UpdateCampaignVoucherRequest request)
		{
			var validation = await ValidateRequestAsync(_updateCampaignVoucherValidator, request);
			if (validation != null) return validation;

			var response = await _campaignService.UpdateCampaignVoucherAsync(id, voucherId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}/vouchers/{voucherId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCampaignVoucher([FromRoute] Guid id, [FromRoute] Guid voucherId)
		{
			var response = await _campaignService.DeleteCampaignVoucherAsync(id, voucherId);
			return HandleResponse(response);
		}
	}
}
