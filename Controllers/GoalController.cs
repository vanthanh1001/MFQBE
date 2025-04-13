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
    public class GoalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GoalController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FitnessGoalDto>>> GetGoals([FromQuery] bool activeOnly = true)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var query = _context.FitnessGoals
                .Where(g => g.UserId == userId);
                
            if (activeOnly)
                query = query.Where(g => !g.IsCompleted && g.Deadline >= DateTime.Today);
                
            var goals = await query.OrderBy(g => g.Deadline).ToListAsync();

            var goalDtos = goals.Select(g => new FitnessGoalDto
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Description,
                Type = g.Type,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue,
                ProgressPercentage = CalculateProgressPercentage(g.CurrentValue, g.TargetValue),
                Deadline = g.Deadline,
                IsCompleted = g.IsCompleted,
                CreatedAt = g.CreatedAt
            }).ToList();

            return Ok(goalDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FitnessGoalDto>> GetGoal(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var goal = await _context.FitnessGoals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound();

            var goalDto = new FitnessGoalDto
            {
                Id = goal.Id,
                Title = goal.Title,
                Description = goal.Description,
                Type = goal.Type,
                TargetValue = goal.TargetValue,
                CurrentValue = goal.CurrentValue,
                ProgressPercentage = CalculateProgressPercentage(goal.CurrentValue, goal.TargetValue),
                Deadline = goal.Deadline,
                IsCompleted = goal.IsCompleted,
                CreatedAt = goal.CreatedAt
            };

            return Ok(goalDto);
        }

        [HttpPost]
        public async Task<ActionResult<FitnessGoalDto>> CreateGoal(CreateGoalDto createDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var goal = new FitnessGoal
            {
                UserId = userId,
                Title = createDto.Title,
                Description = createDto.Description,
                Type = createDto.Type,
                TargetValue = createDto.TargetValue,
                CurrentValue = createDto.CurrentValue,
                Deadline = createDto.Deadline,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.FitnessGoals.Add(goal);
            await _context.SaveChangesAsync();

            var goalDto = new FitnessGoalDto
            {
                Id = goal.Id,
                Title = goal.Title,
                Description = goal.Description,
                Type = goal.Type,
                TargetValue = goal.TargetValue,
                CurrentValue = goal.CurrentValue,
                ProgressPercentage = CalculateProgressPercentage(goal.CurrentValue, goal.TargetValue),
                Deadline = goal.Deadline,
                IsCompleted = goal.IsCompleted,
                CreatedAt = goal.CreatedAt
            };

            return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goalDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGoal(int id, CreateGoalDto updateDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var goal = await _context.FitnessGoals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound();

            goal.Title = updateDto.Title;
            goal.Description = updateDto.Description;
            goal.Type = updateDto.Type;
            goal.TargetValue = updateDto.TargetValue;
            goal.CurrentValue = updateDto.CurrentValue;
            goal.Deadline = updateDto.Deadline;
            goal.UpdatedAt = DateTime.UtcNow;

            _context.FitnessGoals.Update(goal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/progress")]
        public async Task<IActionResult> UpdateGoalProgress(int id, UpdateGoalProgressDto progressDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var goal = await _context.FitnessGoals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound();

            goal.CurrentValue = progressDto.CurrentValue;
            goal.IsCompleted = progressDto.IsCompleted;
            goal.UpdatedAt = DateTime.UtcNow;

            _context.FitnessGoals.Update(goal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var goal = await _context.FitnessGoals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound();

            _context.FitnessGoals.Remove(goal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #region Helper Methods
        
        private int CalculateProgressPercentage(decimal current, decimal target)
        {
            if (target == 0)
                return 0;
                
            int percentage = (int)Math.Min(100, (current / target) * 100);
            return percentage;
        }
        
        #endregion
    }
} 