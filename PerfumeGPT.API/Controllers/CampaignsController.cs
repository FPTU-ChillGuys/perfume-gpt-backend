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

		public CampaignsController(ICampaignService campaignService)
		{
			_campaignService = campaignService;
		}

		[HttpPost]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateCampaign([FromBody] CreateCampaignRequest request)
		{
			var response = await _campaignService.CreateCampaignAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("home")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<List<CampaignResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<CampaignResponse>>>> GetHomeCampaigns()
		{
			var response = await _campaignService.GetHomeCampaignsAsync();
			return HandleResponse(response);
		}

		[HttpGet("lookup/active")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<List<CampaignLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<CampaignLookupItem>>>> GetActiveCampaignLookupList()
		{
			var response = await _campaignService.GetActiveCampaignLookupListAsync();
			return HandleResponse(response);
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<CampaignResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<CampaignResponse>>>> GetCampaigns([FromQuery] GetPagedCampaignsRequest request)
		{
			var response = await _campaignService.GetPagedCampaignsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{campaignId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<CampaignResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CampaignResponse>>> GetCampaignById([FromRoute] Guid campaignId)
		{
			var response = await _campaignService.GetCampaignByIdAsync(campaignId);
			return HandleResponse(response);
		}

		[HttpGet("{campaignId:guid}/items")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<List<CampaignPromotionItemResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<CampaignPromotionItemResponse>>>> GetCampaignItemsByCampaignId([FromRoute] Guid campaignId)
		{
			var response = await _campaignService.GetCampaignItemsByCampaignIdAsync(campaignId);
			return HandleResponse(response);
		}

		[HttpGet("{campaignId:guid}/items/{itemId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<CampaignPromotionItemResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CampaignPromotionItemResponse>>> GetCampaignItemById([FromRoute] Guid campaignId, [FromRoute] Guid itemId)
		{
			var response = await _campaignService.GetCampaignItemByIdAsync(campaignId, itemId);
			return HandleResponse(response);
		}

		[HttpPut("{campaignId:guid}/status")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaignStatus([FromRoute] Guid campaignId, [FromBody] UpdateCampaignStatusRequest request)
		{
			var response = await _campaignService.UpdateCampaignStatusAsync(campaignId, request);
			return HandleResponse(response);
		}

		[HttpPut("{campaignId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaign([FromRoute] Guid campaignId, [FromBody] UpdateCampaignRequest request)
		{
			var response = await _campaignService.UpdateCampaignAsync(campaignId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{campaignId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCampaign([FromRoute] Guid campaignId)
		{
			var response = await _campaignService.DeleteCampaignAsync(campaignId);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/items")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> AddCampaignItem([FromRoute] Guid id, [FromBody] CreateCampaignPromotionItemRequest request)
		{
			var response = await _campaignService.AddCampaignItemAsync(id, request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/items/{itemId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaignItem([FromRoute] Guid id, [FromRoute] Guid itemId, [FromBody] UpdateCampaignPromotionItemRequest request)
		{
			var response = await _campaignService.UpdateCampaignItemAsync(id, itemId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}/items/{itemId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCampaignItem([FromRoute] Guid id, [FromRoute] Guid itemId)
		{
			var response = await _campaignService.DeleteCampaignItemAsync(id, itemId);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/vouchers")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> AddCampaignVoucher([FromRoute] Guid id, [FromBody] CreateCampaignVoucherRequest request)
		{
			var response = await _campaignService.AddCampaignVoucherAsync(id, request);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}/vouchers")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<List<VoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<VoucherResponse>>>> GetCampaignVouchersByCampaignId([FromRoute] Guid id)
		{
			var response = await _campaignService.GetCampaignVouchersByCampaignIdAsync(id);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}/vouchers/{voucherId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<VoucherResponse>>> GetCampaignVoucherById([FromRoute] Guid id, [FromRoute] Guid voucherId)
		{
			var response = await _campaignService.GetCampaignVoucherByIdAsync(id, voucherId);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/vouchers/{voucherId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCampaignVoucher([FromRoute] Guid id, [FromRoute] Guid voucherId, [FromBody] UpdateCampaignVoucherRequest request)
		{
			var response = await _campaignService.UpdateCampaignVoucherAsync(id, voucherId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}/vouchers/{voucherId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCampaignVoucher([FromRoute] Guid id, [FromRoute] Guid voucherId)
		{
			var response = await _campaignService.DeleteCampaignVoucherAsync(id, voucherId);
			return HandleResponse(response);
		}
	}
}
