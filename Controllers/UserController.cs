using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Models.DTOs;
using Models.Interfaces;
using Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IRepository<User> _userRepository;
        private readonly ApplicationDbContext _dbContext;

        public UserController(
            IRepository<User> userRepository,
            ApplicationDbContext dbContext)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            // Lấy Firebase UID từ claim
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized("User không hợp lệ");
            }

            // Tìm user từ firebaseUid
            var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return NotFound("Profile not found");
            }

            // Tạo profile response
            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                ProfilePicture = user.ProfilePicture ?? string.Empty
            };

            return Ok(profileDto);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            // Lấy Firebase UID từ claim
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized("User không hợp lệ");
            }

            // Tìm user từ firebaseUid
            var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return NotFound("Profile not found");
            }

            // Cập nhật thông tin
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            
            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth;
                
            if (!string.IsNullOrEmpty(request.ProfilePicture))
                user.ProfilePicture = request.ProfilePicture;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<UserStatisticsDto>> GetStatistics()
        {
            // Lấy Firebase UID từ claim
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized("User không hợp lệ");
            }

            // Tìm user từ firebaseUid
            var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Lấy thống kê
            var workoutCount = await _dbContext.WorkoutSessions.CountAsync(w => w.UserId == user.Id);
            var completedChallenges = await _dbContext.UserChallenges
                .Where(uc => uc.UserId == user.Id && uc.Points > 0)
                .CountAsync();

            // Tính streak (đơn giản: 3 ngày liên tiếp tương đương 3 streak)
            int currentStreak = 3; // Giả định
            
            // Tính level (đơn giản: cứ 100 điểm lên 1 level)
            int totalPoints = workoutCount * 10 + completedChallenges * 20;
            string currentLevel = $"Cấp {totalPoints / 100 + 1}";

            var statistics = new UserStatisticsDto
            {
                TotalWorkouts = workoutCount,
                CompletedChallenges = completedChallenges,
                CurrentStreak = currentStreak,
                TotalPoints = totalPoints,
                CurrentLevel = currentLevel
            };

            return Ok(statistics);
        }
    }
} 