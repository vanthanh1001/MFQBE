using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DTOs;
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
    public class WorkoutSessionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WorkoutSessionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutSessionDto>>> GetWorkoutSessions()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var sessions = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutPlan)
                .Include(ws => ws.Performances)
                    .ThenInclude(p => p.Exercise)
                .Where(ws => ws.UserId == userId)
                .OrderByDescending(ws => ws.StartTime)
                .ToListAsync();

            var sessionDtos = sessions.Select(s => new WorkoutSessionDto
            {
                Id = s.Id,
                WorkoutPlanId = s.WorkoutPlanId,
                WorkoutPlanName = s.WorkoutPlan?.Name,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                DurationInMinutes = s.DurationInMinutes,
                Notes = s.Notes,
                Performances = s.Performances.Select(p => new ExercisePerformanceDto
                {
                    Id = p.Id,
                    ExerciseId = p.ExerciseId,
                    ExerciseName = p.Exercise.Name,
                    ActualSets = p.ActualSets,
                    ActualReps = p.ActualReps,
                    WeightUsed = p.WeightUsed,
                    ActualRestTime = p.ActualRestTime,
                    Notes = p.Notes
                }).ToList()
            }).ToList();

            return Ok(sessionDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutSessionDto>> GetWorkoutSession(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var session = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutPlan)
                .Include(ws => ws.Performances)
                    .ThenInclude(p => p.Exercise)
                .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

            if (session == null)
                return NotFound();

            var sessionDto = new WorkoutSessionDto
            {
                Id = session.Id,
                WorkoutPlanId = session.WorkoutPlanId,
                WorkoutPlanName = session.WorkoutPlan?.Name,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                DurationInMinutes = session.DurationInMinutes,
                Notes = session.Notes,
                Performances = session.Performances.Select(p => new ExercisePerformanceDto
                {
                    Id = p.Id,
                    ExerciseId = p.ExerciseId,
                    ExerciseName = p.Exercise.Name,
                    ActualSets = p.ActualSets,
                    ActualReps = p.ActualReps,
                    WeightUsed = p.WeightUsed,
                    ActualRestTime = p.ActualRestTime,
                    Notes = p.Notes
                }).ToList()
            };

            return Ok(sessionDto);
        }

        [HttpPost]
        public async Task<ActionResult<WorkoutSessionDto>> CreateWorkoutSession(CreateWorkoutSessionDto createDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Kiểm tra workoutPlanId có tồn tại không
            if (createDto.WorkoutPlanId.HasValue)
            {
                var workoutPlan = await _context.WorkoutPlans.FindAsync(createDto.WorkoutPlanId.Value);
                if (workoutPlan == null)
                    return BadRequest("Không tìm thấy kế hoạch tập luyện");
            }

            // Kiểm tra các exerciseId có tồn tại không
            if (createDto.Performances != null)
            {
                var exerciseIds = createDto.Performances.Select(p => p.ExerciseId).Distinct().ToList();
                var existingExercises = await _context.Exercises
                    .Where(e => exerciseIds.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToListAsync();

                var notFoundExercises = exerciseIds.Except(existingExercises).ToList();
                if (notFoundExercises.Any())
                    return BadRequest($"Không tìm thấy bài tập với ID: {string.Join(", ", notFoundExercises)}");
            }

            var session = new WorkoutSession
            {
                UserId = userId,
                WorkoutPlanId = createDto.WorkoutPlanId,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                DurationInMinutes = createDto.DurationInMinutes,
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkoutSessions.Add(session);
            await _context.SaveChangesAsync();

            if (createDto.Performances != null && createDto.Performances.Count > 0)
            {
                var performances = createDto.Performances.Select(p => new ExercisePerformance
                {
                    WorkoutSessionId = session.Id,
                    ExerciseId = p.ExerciseId,
                    ActualSets = p.ActualSets,
                    ActualReps = p.ActualReps,
                    WeightUsed = p.WeightUsed,
                    ActualRestTime = p.ActualRestTime,
                    Notes = p.Notes,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.ExercisePerformances.AddRange(performances);
                await _context.SaveChangesAsync();
            }

            // Lấy thông tin session vừa tạo (bao gồm performances)
            var createdSession = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutPlan)
                .Include(ws => ws.Performances)
                    .ThenInclude(p => p.Exercise)
                .FirstOrDefaultAsync(ws => ws.Id == session.Id);

            var sessionDto = new WorkoutSessionDto
            {
                Id = createdSession.Id,
                WorkoutPlanId = createdSession.WorkoutPlanId,
                WorkoutPlanName = createdSession.WorkoutPlan?.Name,
                StartTime = createdSession.StartTime,
                EndTime = createdSession.EndTime,
                DurationInMinutes = createdSession.DurationInMinutes,
                Notes = createdSession.Notes,
                Performances = createdSession.Performances.Select(p => new ExercisePerformanceDto
                {
                    Id = p.Id,
                    ExerciseId = p.ExerciseId,
                    ExerciseName = p.Exercise.Name,
                    ActualSets = p.ActualSets,
                    ActualReps = p.ActualReps,
                    WeightUsed = p.WeightUsed,
                    ActualRestTime = p.ActualRestTime,
                    Notes = p.Notes
                }).ToList()
            };

            return CreatedAtAction(nameof(GetWorkoutSession), new { id = sessionDto.Id }, sessionDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkoutSession(int id, UpdateWorkoutSessionDto updateDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var session = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

            if (session == null)
                return NotFound();

            // Cập nhật thông tin
            if (updateDto.EndTime.HasValue)
                session.EndTime = updateDto.EndTime.Value;
                
            if (updateDto.DurationInMinutes.HasValue)
                session.DurationInMinutes = updateDto.DurationInMinutes.Value;
                
            if (updateDto.Notes != null)
                session.Notes = updateDto.Notes;
                
            session.UpdatedAt = DateTime.UtcNow;

            _context.WorkoutSessions.Update(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkoutSession(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var session = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

            if (session == null)
                return NotFound();

            _context.WorkoutSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("history")]
        public async Task<ActionResult<WorkoutHistoryDto>> GetWorkoutHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var sessions = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutPlan)
                .Include(ws => ws.Performances)
                    .ThenInclude(p => p.Exercise)
                .Where(ws => ws.UserId == userId)
                .OrderByDescending(ws => ws.StartTime)
                .Take(10)
                .ToListAsync();

            var totalWorkouts = await _context.WorkoutSessions
                .CountAsync(ws => ws.UserId == userId);
                
            var totalDuration = await _context.WorkoutSessions
                .Where(ws => ws.UserId == userId)
                .SumAsync(ws => ws.DurationInMinutes);

            var historyDto = new WorkoutHistoryDto
            {
                TotalWorkouts = totalWorkouts,
                TotalDuration = totalDuration,
                RecentSessions = sessions.Select(s => new WorkoutSessionDto
                {
                    Id = s.Id,
                    WorkoutPlanId = s.WorkoutPlanId,
                    WorkoutPlanName = s.WorkoutPlan?.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationInMinutes = s.DurationInMinutes,
                    Notes = s.Notes,
                    Performances = s.Performances.Select(p => new ExercisePerformanceDto
                    {
                        Id = p.Id,
                        ExerciseId = p.ExerciseId,
                        ExerciseName = p.Exercise.Name,
                        ActualSets = p.ActualSets,
                        ActualReps = p.ActualReps,
                        WeightUsed = p.WeightUsed,
                        ActualRestTime = p.ActualRestTime,
                        Notes = p.Notes
                    }).ToList()
                }).ToList()
            };

            return Ok(historyDto);
        }
    }
} 