using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Models.DTOs;

namespace Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            return NotFound("Profile not found");
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            return BadRequest("Not implemented yet");
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<UserStatisticsDto>> GetStatistics()
        {
            return NotFound("Statistics not found");
        }
    }
} 