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
    public class WorkoutController : ControllerBase
    {
        [HttpGet("plans")]
        public async Task<ActionResult<IEnumerable<WorkoutPlanDto>>> GetWorkoutPlans()
        {
            // Tạm thời return empty list
            return Ok(new List<WorkoutPlanDto>());
        }

        [HttpGet("plans/{id}")]
        public async Task<ActionResult<WorkoutPlanDetailDto>> GetWorkoutPlanDetail(int id)
        {
            return NotFound($"Workout plan with id {id} not found");
        }

        [HttpPost("plans")]
        public async Task<ActionResult<WorkoutPlanDto>> CreateWorkoutPlan([FromBody] CreateWorkoutPlanDto request)
        {
            return BadRequest("Not implemented yet");
        }

        [HttpGet("exercises/categories")]
        public async Task<ActionResult<IEnumerable<ExerciseCategoryDto>>> GetExerciseCategories()
        {
            return Ok(new List<ExerciseCategoryDto>());
        }
    }
} 