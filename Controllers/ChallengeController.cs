using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models.DTOs;

namespace Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengeController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetChallenges()
        {
            return Ok(new List<ChallengeDto>());
        }

        [HttpGet("leaderboard")]
        public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard()
        {
            return Ok(new List<LeaderboardEntryDto>());
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinChallenge(int id)
        {
            return BadRequest($"Challenge {id} not found");
        }
    }
} 