using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Models.DTOs;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NutritionController : ControllerBase
    {
        private readonly IRepository<Nutrition> _nutritionRepository;
        private readonly IRepository<User> _userRepository;

        public NutritionController(IRepository<Nutrition> nutritionRepository, IRepository<User> userRepository)
        {
            _nutritionRepository = nutritionRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NutritionTrackingDto>>> GetUserNutrition()
        {
            try
            {
                // Lấy user ID từ claim (giả sử đã được lưu trong token)
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                var nutritionEntries = await _nutritionRepository.FindAsync(n => n.UserId == userId);
                var nutritionDtos = nutritionEntries.Select(n => new NutritionTrackingDto
                {
                    Date = n.Date,
                    TotalCalories = n.TotalCalories,
                    ProteinGrams = n.ProteinGrams,
                    CarbsGrams = n.CarbsGrams,
                    FatGrams = n.FatGrams,
                    Meals = new List<MealDto>() // Cần thêm logic để lấy bữa ăn
                }).ToList();

                return Ok(nutritionDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet("{date}")]
        public async Task<ActionResult<NutritionTrackingDto>> GetNutritionByDate(DateTime date)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm dữ liệu dinh dưỡng theo ngày
                var dateOnly = date.Date; // Loại bỏ thời gian, chỉ giữ ngày
                var nutritionEntry = (await _nutritionRepository.FindAsync(n => 
                    n.UserId == userId && n.Date.Date == dateOnly)).FirstOrDefault();

                if (nutritionEntry == null)
                {
                    return NotFound($"Không tìm thấy dữ liệu dinh dưỡng cho ngày {date.ToShortDateString()}");
                }

                var nutritionDto = new NutritionTrackingDto
                {
                    Date = nutritionEntry.Date,
                    TotalCalories = nutritionEntry.TotalCalories,
                    ProteinGrams = nutritionEntry.ProteinGrams,
                    CarbsGrams = nutritionEntry.CarbsGrams,
                    FatGrams = nutritionEntry.FatGrams,
                    Meals = new List<MealDto>() // Cần thêm logic để lấy bữa ăn
                };

                return Ok(nutritionDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<NutritionTrackingDto>> AddNutrition([FromBody] NutritionTrackingDto nutritionDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Kiểm tra xem đã có dữ liệu cho ngày này chưa
                var dateOnly = nutritionDto.Date.Date;
                var existingEntry = (await _nutritionRepository.FindAsync(n => 
                    n.UserId == userId && n.Date.Date == dateOnly)).FirstOrDefault();

                if (existingEntry != null)
                {
                    return BadRequest($"Đã tồn tại dữ liệu dinh dưỡng cho ngày {dateOnly.ToShortDateString()}");
                }

                // Tạo mới entry dinh dưỡng
                var nutrition = new Nutrition
                {
                    UserId = userId,
                    Date = dateOnly,
                    TotalCalories = nutritionDto.TotalCalories,
                    ProteinGrams = nutritionDto.ProteinGrams,
                    CarbsGrams = nutritionDto.CarbsGrams,
                    FatGrams = nutritionDto.FatGrams,
                    CreatedAt = DateTime.UtcNow
                };

                await _nutritionRepository.AddAsync(nutrition);
                await _nutritionRepository.SaveChangesAsync();

                // Trả về DTO với ID mới
                nutritionDto.Date = nutrition.Date;
                return CreatedAtAction(nameof(GetNutritionByDate), new { date = nutrition.Date }, nutritionDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("{date}")]
        public async Task<IActionResult> UpdateNutrition(DateTime date, [FromBody] NutritionTrackingDto nutritionDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm entry dinh dưỡng hiện có
                var dateOnly = date.Date;
                var nutritionEntry = (await _nutritionRepository.FindAsync(n => 
                    n.UserId == userId && n.Date.Date == dateOnly)).FirstOrDefault();

                if (nutritionEntry == null)
                {
                    return NotFound($"Không tìm thấy dữ liệu dinh dưỡng cho ngày {date.ToShortDateString()}");
                }

                // Cập nhật dữ liệu
                nutritionEntry.TotalCalories = nutritionDto.TotalCalories;
                nutritionEntry.ProteinGrams = nutritionDto.ProteinGrams;
                nutritionEntry.CarbsGrams = nutritionDto.CarbsGrams;
                nutritionEntry.FatGrams = nutritionDto.FatGrams;
                nutritionEntry.UpdatedAt = DateTime.UtcNow;

                await _nutritionRepository.UpdateAsync(nutritionEntry);
                await _nutritionRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpDelete("{date}")]
        public async Task<IActionResult> DeleteNutrition(DateTime date)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm entry dinh dưỡng
                var dateOnly = date.Date;
                var nutritionEntry = (await _nutritionRepository.FindAsync(n => 
                    n.UserId == userId && n.Date.Date == dateOnly)).FirstOrDefault();

                if (nutritionEntry == null)
                {
                    return NotFound($"Không tìm thấy dữ liệu dinh dưỡng cho ngày {date.ToShortDateString()}");
                }

                await _nutritionRepository.DeleteAsync(nutritionEntry);
                await _nutritionRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet("meal-plans")]
        public async Task<ActionResult<IEnumerable<MealPlanDto>>> GetMealPlans()
        {
            // Cần thiết kế thêm bảng MealPlan và Meal trong database
            return Ok(new List<MealPlanDto>());
        }
    }
} 