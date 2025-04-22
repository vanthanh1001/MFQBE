using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Models.DTOs;
using System.Text.Json;
using System.Linq;
using System.Security.Claims;

namespace Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MealController : ControllerBase
    {
        private readonly IRepository<Meal> _mealRepository;
        private readonly IRepository<User> _userRepository;

        public MealController(IRepository<Meal> mealRepository, IRepository<User> userRepository)
        {
            _mealRepository = mealRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealDto>>> GetMeals([FromQuery] DateTime? date = null)
        {
            try
            {
                var firebaseUid = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm user từ Firebase UID
                var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return Unauthorized("Người dùng không tồn tại");
                }

                var userId = user.Id;

                IEnumerable<Meal> meals;
                if (date.HasValue)
                {
                    var dateOnly = date.Value.Date;
                    meals = await _mealRepository.FindAsync(m => m.UserId == userId && m.Date.Date == dateOnly);
                }
                else
                {
                    meals = await _mealRepository.FindAsync(m => m.UserId == userId);
                }

                var mealDtos = new List<MealDto>();
                foreach (var meal in meals)
                {
                    var ingredients = !string.IsNullOrEmpty(meal.Ingredients) 
                        ? JsonSerializer.Deserialize<List<string>>(meal.Ingredients) 
                        : new List<string>();

                    mealDtos.Add(new MealDto
                    {
                        Id = meal.Id,
                        Name = meal.Name,
                        Type = meal.Type,
                        Calories = meal.Calories,
                        Ingredients = ingredients
                    });
                }

                return Ok(mealDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MealDto>> GetMeal(int id)
        {
            try
            {
                var firebaseUid = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm user từ Firebase UID
                var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return Unauthorized("Người dùng không tồn tại");
                }

                var userId = user.Id;

                var meal = await _mealRepository.GetByIdAsync(id);
                if (meal == null || meal.UserId != userId)
                {
                    return NotFound($"Không tìm thấy bữa ăn với ID {id}");
                }

                var ingredients = !string.IsNullOrEmpty(meal.Ingredients) 
                    ? JsonSerializer.Deserialize<List<string>>(meal.Ingredients) 
                    : new List<string>();

                var mealDto = new MealDto
                {
                    Id = meal.Id,
                    Name = meal.Name,
                    Type = meal.Type,
                    Calories = meal.Calories,
                    Ingredients = ingredients
                };

                return Ok(mealDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<MealDto>> CreateMeal([FromBody] CreateMealDto createMealDto)
        {
            try
            {
                var firebaseUid = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm user từ Firebase UID
                var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return Unauthorized("Người dùng không tồn tại");
                }

                var userId = user.Id;

                var ingredients = JsonSerializer.Serialize(createMealDto.Ingredients);

                var meal = new Meal
                {
                    UserId = userId,
                    Date = DateTime.UtcNow.Date, // Sử dụng ngày hiện tại
                    Name = createMealDto.Name,
                    Type = createMealDto.Type,
                    Calories = createMealDto.Calories,
                    Ingredients = ingredients,
                    CreatedAt = DateTime.UtcNow
                };

                await _mealRepository.AddAsync(meal);
                await _mealRepository.SaveChangesAsync();

                var mealDto = new MealDto
                {
                    Id = meal.Id,
                    Name = meal.Name,
                    Type = meal.Type,
                    Calories = meal.Calories,
                    Ingredients = createMealDto.Ingredients
                };

                return CreatedAtAction(nameof(GetMeal), new { id = meal.Id }, mealDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeal(int id, [FromBody] UpdateMealDto updateMealDto)
        {
            try
            {
                var firebaseUid = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm user từ Firebase UID
                var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return Unauthorized("Người dùng không tồn tại");
                }

                var userId = user.Id;

                var meal = await _mealRepository.GetByIdAsync(id);
                if (meal == null || meal.UserId != userId)
                {
                    return NotFound($"Không tìm thấy bữa ăn với ID {id}");
                }

                var ingredients = JsonSerializer.Serialize(updateMealDto.Ingredients);

                meal.Name = updateMealDto.Name;
                meal.Type = updateMealDto.Type;
                meal.Calories = updateMealDto.Calories;
                meal.Ingredients = ingredients;
                meal.UpdatedAt = DateTime.UtcNow;

                await _mealRepository.UpdateAsync(meal);
                await _mealRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            try
            {
                var firebaseUid = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized("Không thể xác định người dùng");
                }

                // Tìm user từ Firebase UID
                var users = await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid);
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return Unauthorized("Người dùng không tồn tại");
                }

                var userId = user.Id;

                var meal = await _mealRepository.GetByIdAsync(id);
                if (meal == null || meal.UserId != userId)
                {
                    return NotFound($"Không tìm thấy bữa ăn với ID {id}");
                }

                await _mealRepository.DeleteAsync(meal);
                await _mealRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
    }
} 