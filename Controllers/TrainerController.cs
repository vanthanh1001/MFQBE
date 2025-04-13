using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DTOs;
using Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrainerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Exercise> _exerciseRepository;

        public TrainerController(
            ApplicationDbContext context,
            IRepository<User> userRepository,
            IRepository<Exercise> exerciseRepository)
        {
            _context = context;
            _userRepository = userRepository;
            _exerciseRepository = exerciseRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllTrainers()
        {
            var trainers = await _userRepository.FindAsync(u => u.Role == UserRole.Trainer);
            
            var trainerDtos = trainers.Select(t => new UserDto
            {
                Id = t.Id,
                Username = t.Username,
                Email = t.Email,
                DisplayName = t.DisplayName,
                FirstName = t.FirstName,
                LastName = t.LastName,
                ProfilePicture = t.ProfilePicture,
                PhoneNumber = t.PhoneNumber,
                Role = t.Role,
                CreatedAt = t.CreatedAt
            });
            
            return Ok(trainerDtos);
        }

        [HttpGet("exercises")]
        public async Task<ActionResult<IEnumerable<ExerciseResponseDto>>> GetTrainerExercises()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userRepository.GetByIdAsync(currentUserId);

            if (user.Role != UserRole.Trainer && user.Role != UserRole.Admin)
            {
                return Forbid("Chỉ PT mới có quyền truy cập API này");
            }

            var exercises = await _context.Exercises
                .Where(e => e.CreatedById == currentUserId)
                .Include(e => e.CreatedBy)
                .Select(e => new ExerciseResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    RestTime = e.RestTime,
                    CreatedById = e.CreatedById,
                    CreatedByUsername = e.CreatedBy.Username,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .ToListAsync();
                
            return Ok(exercises);
        }

        [HttpGet("clients")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetClientsForTrainer()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userRepository.GetByIdAsync(currentUserId);

            if (user.Role != UserRole.Trainer)
            {
                return Forbid("Chỉ PT mới có quyền truy cập API này");
            }

            // Trong thực tế, bạn có thể cần một bảng liên kết giữa Trainer và User
            // Hiện tại giả định tất cả User đều là khách hàng tiềm năng
            var clients = await _userRepository.FindAsync(u => u.Role == UserRole.User);
            
            var clientDtos = clients.Select(c => new UserDto
            {
                Id = c.Id,
                Username = c.Username,
                Email = c.Email,
                DisplayName = c.DisplayName,
                FirstName = c.FirstName,
                LastName = c.LastName,
                ProfilePicture = c.ProfilePicture,
                PhoneNumber = c.PhoneNumber,
                CreatedAt = c.CreatedAt
            });
            
            return Ok(clientDtos);
        }

        [HttpPost("become-trainer")]
        public async Task<ActionResult> BecomeTrainer()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            if (user.Role == UserRole.Trainer)
                return BadRequest("Bạn đã là PT rồi");

            // Chuyển người dùng thành PT
            user.Role = UserRole.Trainer;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return Ok(new { message = "Bạn đã trở thành PT thành công!" });
        }

        [HttpPost("workouts/share")]
        public async Task<ActionResult> ShareWorkoutWithClient([FromBody] ShareWorkoutPlanDto request)
        {
            var trainerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var trainer = await _userRepository.GetByIdAsync(trainerId);

            if (trainer.Role != UserRole.Trainer)
                return Forbid("Chỉ PT mới có thể chia sẻ bài tập");

            // Kiểm tra xem workout plan có tồn tại và thuộc về PT này không
            var workoutPlan = await _context.WorkoutPlans
                .Include(wp => wp.Exercises)
                .FirstOrDefaultAsync(wp => wp.Id == request.WorkoutPlanId && wp.UserId == trainerId);
                
            if (workoutPlan == null)
                return NotFound("Không tìm thấy workout plan hoặc bạn không có quyền truy cập");

            // Kiểm tra user được chia sẻ
            var client = await _userRepository.GetByIdAsync(request.UserId);
            if (client == null)
                return NotFound("Không tìm thấy người dùng");

            // Tạo một bản sao workout plan cho khách hàng
            var sharedWorkoutPlan = new WorkoutPlan
            {
                Name = workoutPlan.Name,
                Description = $"{workoutPlan.Description} (Được chia sẻ bởi {trainer.DisplayName})",
                UserId = client.Id,
                CreatedAt = DateTime.UtcNow,
                Exercises = workoutPlan.Exercises.ToList()
            };

            await _context.WorkoutPlans.AddAsync(sharedWorkoutPlan);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Đã chia sẻ bài tập cho {client.DisplayName} thành công",
                workoutPlanId = sharedWorkoutPlan.Id
            });
        }

        [HttpPost("upload-avatar")]
        public async Task<ActionResult> UploadTrainerAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file được gửi lên");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userRepository.GetByIdAsync(userId);

            if (user.Role != UserRole.Trainer)
                return Forbid("Chỉ PT mới có thể sử dụng API này");

            try
            {
                // Xử lý upload file (trong thực tế sẽ lưu lên storage hoặc đường dẫn thư mục)
                // Ví dụ đơn giản - thực tế bạn cần triển khai service upload file (có thể sử dụng Firebase Storage)
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine("wwwroot/images/avatars", fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Cập nhật avatar cho trainer
                user.ProfilePicture = $"/images/avatars/{fileName}";
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                return Ok(new { avatarUrl = user.ProfilePicture });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi upload avatar: {ex.Message}");
            }
        }
    }
} 