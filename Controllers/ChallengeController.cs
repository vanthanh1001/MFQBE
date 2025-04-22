using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using Models.Enums;

namespace Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Challenge> _challengeRepository;
        private readonly IRepository<UserChallenge> _userChallengeRepository;
        private readonly IRepository<User> _userRepository;

        public ChallengeController(
            ApplicationDbContext context,
            IRepository<Challenge> challengeRepository,
            IRepository<UserChallenge> userChallengeRepository,
            IRepository<User> userRepository)
        {
            _context = context;
            _challengeRepository = challengeRepository;
            _userChallengeRepository = userChallengeRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetAllChallenges()
        {
            var challenges = await _challengeRepository.GetAllAsync();
            
            var challengeDtos = challenges.Select(c => new ChallengeDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RewardPoints = c.RewardPoints,
                RewardDescription = c.RewardDescription
            }).ToList();
            
            return Ok(challengeDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChallengeDto>> GetChallenge(int id)
        {
            var challenge = await _challengeRepository.GetByIdAsync(id);
            if (challenge == null)
                return NotFound($"Challenge with ID {id} not found");
                
            return Ok(new ChallengeDto
            {
                Id = challenge.Id,
                Name = challenge.Name,
                Description = challenge.Description,
                StartDate = challenge.StartDate,
                EndDate = challenge.EndDate,
                RewardPoints = challenge.RewardPoints,
                RewardDescription = challenge.RewardDescription
            });
        }

        [HttpPost]
        public async Task<ActionResult<ChallengeDto>> CreateChallenge([FromBody] ChallengeDto challengeDto)
        {
            // Lấy Firebase UID từ claim
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized("User không hợp lệ");
            }

            // Tìm userId từ firebaseUid
            var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return Unauthorized("User không tồn tại");
            }

            // Kiểm tra quyền Admin
            if (user.Role != UserRole.Admin)
            {
                return Forbid("Bạn không có quyền tạo thử thách. Chỉ Admin mới có thể tạo thử thách mới.");
            }

            if (challengeDto.StartDate >= challengeDto.EndDate)
            {
                return BadRequest("End date must be later than start date");
            }
            
            var challenge = new Challenge
            {
                Name = challengeDto.Name,
                Description = challengeDto.Description,
                StartDate = challengeDto.StartDate,
                EndDate = challengeDto.EndDate,
                RewardPoints = challengeDto.RewardPoints,
                RewardDescription = challengeDto.RewardDescription,
                CreatedAt = DateTime.UtcNow
            };
            
            await _challengeRepository.AddAsync(challenge);
            await _challengeRepository.SaveChangesAsync();
            
            challengeDto.Id = challenge.Id;
            return CreatedAtAction(nameof(GetChallenge), new { id = challenge.Id }, challengeDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChallenge(int id, [FromBody] ChallengeDto challengeDto)
        {
            // Lấy Firebase UID từ claim
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized("User không hợp lệ");
            }

            // Tìm userId từ firebaseUid
            var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return Unauthorized("User không tồn tại");
            }

            // Kiểm tra quyền Admin
            if (user.Role != UserRole.Admin)
            {
                return Forbid("Bạn không có quyền cập nhật thử thách. Chỉ Admin mới có thể cập nhật thử thách.");
            }
                
            if (id != challengeDto.Id)
                return BadRequest("Challenge ID mismatch");
                
            var challenge = await _challengeRepository.GetByIdAsync(id);
            if (challenge == null)
                return NotFound($"Challenge with ID {id} not found");
                
            if (challengeDto.StartDate >= challengeDto.EndDate)
            {
                return BadRequest("End date must be later than start date");
            }
            
            challenge.Name = challengeDto.Name;
            challenge.Description = challengeDto.Description;
            challenge.StartDate = challengeDto.StartDate;
            challenge.EndDate = challengeDto.EndDate;
            challenge.RewardPoints = challengeDto.RewardPoints;
            challenge.RewardDescription = challengeDto.RewardDescription;
            challenge.UpdatedAt = DateTime.UtcNow;
            
            await _challengeRepository.UpdateAsync(challenge);
            await _challengeRepository.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChallenge(int id)
        {
            // Lấy Firebase UID từ claim
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized("User không hợp lệ");
            }

            // Tìm userId từ firebaseUid
            var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return Unauthorized("User không tồn tại");
            }

            // Kiểm tra quyền Admin
            if (user.Role != UserRole.Admin)
            {
                return Forbid("Bạn không có quyền xóa thử thách. Chỉ Admin mới có thể xóa thử thách.");
            }
                
            var challenge = await _challengeRepository.GetByIdAsync(id);
            if (challenge == null)
                return NotFound($"Challenge with ID {id} not found");
                
            await _challengeRepository.DeleteAsync(challenge);
            await _challengeRepository.SaveChangesAsync();
            
            return NoContent();
        }
        
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetActiveChallenges()
        {
            var now = DateTime.UtcNow;
            
            var activeChallenges = await _challengeRepository.FindAsync(c => 
                c.StartDate <= now && c.EndDate >= now);
                
            var challengeDtos = activeChallenges.Select(c => new ChallengeDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RewardPoints = c.RewardPoints,
                RewardDescription = c.RewardDescription
            }).ToList();
            
            return Ok(challengeDtos);
        }
        
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetUpcomingChallenges()
        {
            var now = DateTime.UtcNow;
            
            var upcomingChallenges = await _challengeRepository.FindAsync(c => 
                c.StartDate > now);
                
            var challengeDtos = upcomingChallenges.Select(c => new ChallengeDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RewardPoints = c.RewardPoints,
                RewardDescription = c.RewardDescription
            }).ToList();
            
            return Ok(challengeDtos);
        }
        
        [HttpGet("completed")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetCompletedChallenges()
        {
            var now = DateTime.UtcNow;
            
            var completedChallenges = await _challengeRepository.FindAsync(c => 
                c.EndDate < now);
                
            var challengeDtos = completedChallenges.Select(c => new ChallengeDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RewardPoints = c.RewardPoints,
                RewardDescription = c.RewardDescription
            }).ToList();
            
            return Ok(challengeDtos);
        }
        
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinChallenge(int id)
        {
            // Get current user ID
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();
                
            // Check if challenge exists
            var challenge = await _challengeRepository.GetByIdAsync(id);
            if (challenge == null)
                return NotFound($"Challenge with ID {id} not found");
                
            // Check if challenge is active
            var now = DateTime.UtcNow;
            if (challenge.StartDate > now)
                return BadRequest("This challenge has not started yet");
                
            if (challenge.EndDate < now)
                return BadRequest("This challenge has already ended");
                
            // Check if user already joined
            var existingUserChallenge = (await _userChallengeRepository.FindAsync(uc => 
                uc.UserId == userId && uc.ChallengeId == id)).FirstOrDefault();
                
            if (existingUserChallenge != null)
                return BadRequest("You have already joined this challenge");
                
            // Create user challenge entry
            var userChallenge = new UserChallenge
            {
                UserId = userId,
                ChallengeId = id,
                Points = 0,
                JoinDate = now,
                CreatedAt = now
            };
            
            await _userChallengeRepository.AddAsync(userChallenge);
            await _userChallengeRepository.SaveChangesAsync();
            
            return Ok(new { message = $"Successfully joined challenge: {challenge.Name}" });
        }
        
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<UserChallengeDto>>> GetUserChallenges()
        {
            // Get current user ID
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();
                
            // Get challenges the user has joined using EF Core navigation properties
            var userChallenges = await _context.UserChallenges
                .Include(uc => uc.Challenge)
                .Where(uc => uc.UserId == userId)
                .ToListAsync();
                
            var userChallengeDtos = userChallenges.Select(uc => new UserChallengeDto
            {
                ChallengeId = uc.ChallengeId,
                ChallengeName = uc.Challenge.Name,
                ChallengeDescription = uc.Challenge.Description,
                StartDate = uc.Challenge.StartDate,
                EndDate = uc.Challenge.EndDate,
                JoinDate = uc.JoinDate,
                Points = uc.Points,
                RewardPoints = uc.Challenge.RewardPoints,
                IsCompleted = uc.Points >= uc.Challenge.RewardPoints,
                IsActive = uc.Challenge.StartDate <= DateTime.UtcNow && uc.Challenge.EndDate >= DateTime.UtcNow
            }).ToList();
            
            return Ok(userChallengeDtos);
        }
        
        [HttpPost("{id}/progress")]
        public async Task<IActionResult> UpdateChallengeProgress(int id, [FromBody] int pointsEarned)
        {
            if (pointsEarned <= 0)
                return BadRequest("Points earned must be greater than zero");
                
            // Get current user ID
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();
                
            // Check if user has joined this challenge
            var userChallenge = (await _userChallengeRepository.FindAsync(uc => 
                uc.UserId == userId && uc.ChallengeId == id)).FirstOrDefault();
                
            if (userChallenge == null)
                return BadRequest("You have not joined this challenge");
                
            // Check if challenge is active
            var challenge = await _challengeRepository.GetByIdAsync(id);
            var now = DateTime.UtcNow;
            if (challenge.StartDate > now || challenge.EndDate < now)
                return BadRequest("This challenge is not active");
                
            // Update points
            userChallenge.Points += pointsEarned;
            userChallenge.UpdatedAt = now;
            
            await _userChallengeRepository.UpdateAsync(userChallenge);
            await _userChallengeRepository.SaveChangesAsync();
            
            // Check if challenge is completed
            bool isCompleted = userChallenge.Points >= challenge.RewardPoints;
            
            return Ok(new
            {
                message = $"Added {pointsEarned} points to your challenge progress",
                totalPoints = userChallenge.Points,
                targetPoints = challenge.RewardPoints,
                isCompleted
            });
        }
        
        [HttpGet("leaderboard/{id}")]
        public async Task<ActionResult<IEnumerable<ChallengeLeaderboardEntryDto>>> GetChallengeLeaderboard(int id)
        {
            // Check if challenge exists
            var challenge = await _challengeRepository.GetByIdAsync(id);
            if (challenge == null)
                return NotFound($"Challenge with ID {id} not found");
                
            // Get leaderboard data
            var leaderboardEntries = await _context.UserChallenges
                .Include(uc => uc.User)
                .Where(uc => uc.ChallengeId == id)
                .OrderByDescending(uc => uc.Points)
                .Take(100) // Limit to top 100
                .Select(uc => new ChallengeLeaderboardEntryDto
                {
                    UserId = uc.UserId,
                    Username = uc.User.Username,
                    ProfilePicture = uc.User.ProfilePicture,
                    Points = uc.Points,
                    Level = DetermineUserLevel(uc.User)
                })
                .ToListAsync();
                
            return Ok(leaderboardEntries);
        }
        
        // Helper method to determine user level based on user activity
        private string DetermineUserLevel(User user)
        {
            int totalPoints = user.UserChallenges.Sum(uc => uc.Points);
            int completedChallenges = user.UserChallenges.Count(uc => 
                uc.Points >= uc.Challenge.RewardPoints);
                
            if (totalPoints >= 1000 || completedChallenges >= 10)
                return "Expert";
            else if (totalPoints >= 500 || completedChallenges >= 5)
                return "Intermediate";
            else
                return "Beginner";
        }
    }
} 