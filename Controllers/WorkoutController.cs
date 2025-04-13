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
    public class WorkoutController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WorkoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("plans")]
        public async Task<ActionResult<IEnumerable<WorkoutPlanDto>>> GetWorkoutPlans()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var plans = await _context.WorkoutPlans
                .Include(wp => wp.Exercises)
                .Where(wp => wp.UserId == userId)
                .ToListAsync();

            var planDtos = plans.Select(p => new WorkoutPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DurationInMinutes = EstimateWorkoutDuration(p.Exercises.ToList()),
                Difficulty = CalculateDifficulty(p.Exercises.ToList()),
                ExerciseCount = p.Exercises.Count
            }).ToList();

            return Ok(planDtos);
        }

        [HttpGet("plans/{id}")]
        public async Task<ActionResult<WorkoutPlanDetailDto>> GetWorkoutPlanDetail(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var plan = await _context.WorkoutPlans
                .Include(wp => wp.Exercises)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserId == userId);

            if (plan == null)
            return NotFound($"Workout plan with id {id} not found");

            var exerciseDtos = plan.Exercises.Select(e => new ExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Sets = e.Sets,
                Reps = e.Reps,
                RestTime = e.RestTime,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            var planDto = new WorkoutPlanDetailDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                DurationInMinutes = EstimateWorkoutDuration(plan.Exercises.ToList()),
                Difficulty = CalculateDifficulty(plan.Exercises.ToList()),
                ExerciseCount = plan.Exercises.Count,
                Exercises = exerciseDtos
            };

            return Ok(planDto);
        }

        [HttpPost("plans")]
        public async Task<ActionResult<WorkoutPlanDto>> CreateWorkoutPlan([FromBody] CreateWorkoutPlanDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            // Kiểm tra các exerciseId có tồn tại không
            var exerciseIds = request.ExerciseIds;
            var exercises = await _context.Exercises
                .Where(e => exerciseIds.Contains(e.Id))
                .ToListAsync();

            if (exercises.Count != exerciseIds.Count)
            {
                var foundIds = exercises.Select(e => e.Id).ToList();
                var notFoundIds = exerciseIds.Except(foundIds).ToList();
                return BadRequest($"Không tìm thấy các bài tập với ID: {string.Join(", ", notFoundIds)}");
            }

            var workoutPlan = new WorkoutPlan
            {
                Name = request.Name,
                Description = request.Description,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Exercises = exercises
            };

            _context.WorkoutPlans.Add(workoutPlan);
            await _context.SaveChangesAsync();

            var planDto = new WorkoutPlanDto
            {
                Id = workoutPlan.Id,
                Name = workoutPlan.Name,
                Description = workoutPlan.Description,
                DurationInMinutes = EstimateWorkoutDuration(exercises),
                Difficulty = CalculateDifficulty(exercises),
                ExerciseCount = exercises.Count
            };

            return CreatedAtAction(nameof(GetWorkoutPlanDetail), new { id = workoutPlan.Id }, planDto);
        }

        [HttpPut("plans/{id}")]
        public async Task<IActionResult> UpdateWorkoutPlan(int id, [FromBody] CreateWorkoutPlanDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var workoutPlan = await _context.WorkoutPlans
                .Include(wp => wp.Exercises)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserId == userId);

            if (workoutPlan == null)
                return NotFound($"Workout plan with id {id} not found");

            // Kiểm tra các exerciseId có tồn tại không
            var exerciseIds = request.ExerciseIds;
            var exercises = await _context.Exercises
                .Where(e => exerciseIds.Contains(e.Id))
                .ToListAsync();

            if (exercises.Count != exerciseIds.Count)
            {
                var foundIds = exercises.Select(e => e.Id).ToList();
                var notFoundIds = exerciseIds.Except(foundIds).ToList();
                return BadRequest($"Không tìm thấy các bài tập với ID: {string.Join(", ", notFoundIds)}");
            }

            // Cập nhật thông tin
            workoutPlan.Name = request.Name;
            workoutPlan.Description = request.Description;
            workoutPlan.UpdatedAt = DateTime.UtcNow;
            
            // Cập nhật danh sách bài tập
            workoutPlan.Exercises.Clear();
            foreach (var exercise in exercises)
            {
                workoutPlan.Exercises.Add(exercise);
            }

            _context.WorkoutPlans.Update(workoutPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("plans/{id}")]
        public async Task<IActionResult> DeleteWorkoutPlan(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var workoutPlan = await _context.WorkoutPlans
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserId == userId);

            if (workoutPlan == null)
                return NotFound($"Workout plan with id {id} not found");

            _context.WorkoutPlans.Remove(workoutPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("exercises/categories")]
        public async Task<ActionResult<IEnumerable<ExerciseCategoryDto>>> GetExerciseCategories()
        {
            // Các danh mục bài tập mẫu
            var categories = new List<ExerciseCategoryDto>
            {
                new ExerciseCategoryDto { Id = 1, Name = "Ngực", Description = "Các bài tập cho cơ ngực", IconUrl = "chest.svg" },
                new ExerciseCategoryDto { Id = 2, Name = "Lưng", Description = "Các bài tập cho cơ lưng", IconUrl = "back.svg" },
                new ExerciseCategoryDto { Id = 3, Name = "Vai", Description = "Các bài tập cho cơ vai", IconUrl = "shoulder.svg" },
                new ExerciseCategoryDto { Id = 4, Name = "Tay trước", Description = "Các bài tập cho cơ tay trước", IconUrl = "biceps.svg" },
                new ExerciseCategoryDto { Id = 5, Name = "Tay sau", Description = "Các bài tập cho cơ tay sau", IconUrl = "triceps.svg" },
                new ExerciseCategoryDto { Id = 6, Name = "Chân", Description = "Các bài tập cho cơ chân", IconUrl = "legs.svg" },
                new ExerciseCategoryDto { Id = 7, Name = "Bụng", Description = "Các bài tập cho cơ bụng", IconUrl = "abs.svg" },
                new ExerciseCategoryDto { Id = 8, Name = "Cardio", Description = "Các bài tập Cardio", IconUrl = "cardio.svg" }
            };

            return Ok(categories);
        }

        #region Trainer Specific Endpoints
        
        [HttpPost("plans/share")]
        public async Task<ActionResult> ShareWorkoutPlan([FromBody] ShareWorkoutPlanDto request)
        {
            var trainerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            // Kiểm tra xem người chia sẻ có phải PT không
            var trainer = await _context.Users.FindAsync(trainerId);
            if (trainer == null || trainer.Role != UserRole.Trainer)
            {
                return Forbid("Chỉ PT mới có thể chia sẻ bài tập");
            }
            
            // Kiểm tra xem workout plan có tồn tại và thuộc về PT này không
            var workoutPlan = await _context.WorkoutPlans
                .Include(wp => wp.Exercises)
                .FirstOrDefaultAsync(wp => wp.Id == request.WorkoutPlanId && wp.UserId == trainerId);
                
            if (workoutPlan == null)
            {
                return NotFound($"Không tìm thấy workout plan với ID {request.WorkoutPlanId} hoặc bạn không có quyền chia sẻ kế hoạch này");
            }
            
            // Kiểm tra người dùng được chia sẻ có tồn tại không
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với ID {request.UserId}");
            }
            
            // Lấy tên hiển thị từ các trường có sẵn
            string displayName = user.DisplayName ?? user.Username;
            
            // Tạo bản sao của workout plan cho người dùng
            var sharedWorkoutPlan = new WorkoutPlan
            {
                Name = workoutPlan.Name,
                Description = $"{workoutPlan.Description} (Chia sẻ bởi {trainer.DisplayName ?? trainer.Username})",
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                Exercises = workoutPlan.Exercises.ToList()
            };
            
            _context.WorkoutPlans.Add(sharedWorkoutPlan);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = $"Đã chia sẻ workout plan cho {displayName} thành công",
                workoutPlanId = sharedWorkoutPlan.Id
            });
        }
        
        [HttpGet("trainer/clients")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetTrainerClients()
        {
            var trainerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            // Kiểm tra xem người gọi API có phải PT không
            var trainer = await _context.Users.FindAsync(trainerId);
            if (trainer == null || trainer.Role != UserRole.Trainer)
            {
                return Forbid("Chỉ PT mới có thể xem danh sách khách hàng");
            }
            
            // Lấy danh sách khách hàng (mặc định là tất cả người dùng có Role = User)
            // Trong thực tế, bạn có thể cần một bảng liên kết giữa Trainer và các khách hàng của họ
            var clients = await _context.Users
                .Where(u => u.Role == UserRole.User)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ProfilePicture = u.ProfilePicture,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    DisplayName = u.DisplayName
                })
                .ToListAsync();
                
            return Ok(clients);
        }
        
        [HttpGet("trainer/exercises")]
        public async Task<ActionResult<IEnumerable<ExerciseResponseDto>>> GetTrainerExercises()
        {
            var trainerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            // Lấy tất cả bài tập do PT này tạo
            var exercises = await _context.Exercises
                .Where(e => e.CreatedById == trainerId)
                .Select(e => new ExerciseResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    RestTime = e.RestTime,
                    CreatedById = e.CreatedById,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .ToListAsync();
                
            return Ok(exercises);
        }
        
        [HttpGet("public-exercises")]
        public async Task<ActionResult<IEnumerable<ExerciseResponseDto>>> GetPublicExercises()
        {
            // Lấy tất cả bài tập của PT hoặc những bài tập được đánh dấu là công khai
            var trainerExercises = await _context.Exercises
                .Include(e => e.CreatedBy)
                .Where(e => e.CreatedBy.Role == UserRole.Trainer)
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
                
            return Ok(trainerExercises);
        }
        
        #endregion

        #region Helper Methods
        
        private int EstimateWorkoutDuration(List<Exercise> exercises)
        {
            int totalDuration = 0;
            foreach (var exercise in exercises)
            {
                // Dựa theo số set, số rep, và thời gian nghỉ để ước tính
                int exerciseDuration = exercise.Sets * (exercise.Reps * 3 + exercise.RestTime);
                totalDuration += exerciseDuration;
            }
            
            // Chuyển từ giây sang phút, làm tròn lên
            return (int)Math.Ceiling(totalDuration / 60.0);
        }
        
        private string CalculateDifficulty(List<Exercise> exercises)
        {
            if (exercises.Count == 0)
                return "Dễ";
                
            // Tính điểm khó dựa trên số bài tập, số set, rep, và thời gian nghỉ
            double totalScore = 0;
            foreach (var exercise in exercises)
            {
                double exerciseScore = exercise.Sets * exercise.Reps / (exercise.RestTime / 30.0);
                totalScore += exerciseScore;
            }
            
            // Tính điểm trung bình
            double averageScore = totalScore / exercises.Count;
            
            // Phân loại độ khó
            if (averageScore < 10)
                return "Dễ";
            else if (averageScore < 20)
                return "Vừa";
            else
                return "Khó";
        }
        
        #endregion
    }
} 