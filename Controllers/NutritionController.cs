using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models.DTOs;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NutritionController : ControllerBase
    {
        [HttpGet("meal-plans")]
        public async Task<ActionResult<IEnumerable<MealPlanDto>>> GetMealPlans()
        {
            return Ok(new List<MealPlanDto>());
        }

        [HttpGet("nutrition-tracking")]
        public async Task<ActionResult<NutritionTrackingDto>> GetNutritionTracking([FromQuery] DateTime date)
        {
            return NotFound($"No tracking data found for date {date}");
        }
    }
} 