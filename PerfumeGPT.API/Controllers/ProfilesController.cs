using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfilesController : BaseApiController
    {
        private readonly IProfileService _profileService;

        public ProfilesController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var validation = ValidateRequestBody<UpdateProfileRequest>(request);
            if (validation != null) return validation;

            var userId = GetCurrentUserId();

            var result = await _profileService.UpdateProfileAsync(userId, request);
            return HandleResponse(result);
        }
    }
}
