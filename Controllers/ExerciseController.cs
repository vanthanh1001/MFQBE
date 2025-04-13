using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using Models.DTOs;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.Enums;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExerciseController : ControllerBase
{
    private readonly IRepository<Exercise> _exerciseRepository;
    private readonly IRepository<User> _userRepository;

    public ExerciseController(
        IRepository<Exercise> exerciseRepository,
        IRepository<User> userRepository)
    {
        _exerciseRepository = exerciseRepository;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseResponseDto>>> GetAll()
    {
        var exercises = await _exerciseRepository.GetAllAsync();
        var exerciseDtos = exercises.Select(e => new ExerciseResponseDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Sets = e.Sets,
            Reps = e.Reps,
            RestTime = e.RestTime,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });
        return Ok(exerciseDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseResponseDto>> GetById(int id)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
            return NotFound();

        var exerciseDto = new ExerciseResponseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            Description = exercise.Description,
            Sets = exercise.Sets,
            Reps = exercise.Reps,
            RestTime = exercise.RestTime,
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };
        return Ok(exerciseDto);
    }

    [HttpPost]
    public async Task<ActionResult<ExerciseResponseDto>> Create(CreateExerciseDto createDto)
    {
        // Lấy userId từ claim
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Unauthorized("User không hợp lệ");
        }

        // Kiểm tra vai trò người dùng
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized("User không tồn tại");
        }

        // Chỉ cho phép Trainer hoặc Admin tạo bài tập
        if (user.Role != UserRole.Trainer && user.Role != UserRole.Admin)
        {
            return Forbid("Bạn không có quyền tạo bài tập. Chỉ PT mới có thể tạo bài tập mới.");
        }

        var exercise = new Exercise
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Sets = createDto.Sets,
            Reps = createDto.Reps,
            RestTime = createDto.RestTime,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _exerciseRepository.AddAsync(exercise);
        await _exerciseRepository.SaveChangesAsync();

        var responseDto = new ExerciseResponseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            Description = exercise.Description,
            Sets = exercise.Sets,
            Reps = exercise.Reps,
            RestTime = exercise.RestTime,
            CreatedById = exercise.CreatedById,
            CreatedByUsername = user.Username,
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = exercise.Id }, responseDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateExerciseDto updateDto)
    {
        // Lấy userId từ claim
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Unauthorized("User không hợp lệ");
        }

        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
            return NotFound();

        // Kiểm tra vai trò người dùng và quyền sở hữu
        var user = await _userRepository.GetByIdAsync(userId);
        
        // Chỉ cho phép Trainer/Admin hoặc người tạo sửa bài tập
        bool isAdmin = user.Role == UserRole.Admin;
        bool isOwner = exercise.CreatedById == userId;
        
        if (!isAdmin && !isOwner)
        {
            return Forbid("Bạn không có quyền sửa bài tập này");
        }

        exercise.Name = updateDto.Name;
        exercise.Description = updateDto.Description;
        exercise.Sets = updateDto.Sets;
        exercise.Reps = updateDto.Reps;
        exercise.RestTime = updateDto.RestTime;
        exercise.UpdatedAt = DateTime.UtcNow;

        await _exerciseRepository.UpdateAsync(exercise);
        await _exerciseRepository.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Lấy userId từ claim
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Unauthorized("User không hợp lệ");
        }

        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
            return NotFound();

        // Kiểm tra vai trò người dùng và quyền sở hữu
        var user = await _userRepository.GetByIdAsync(userId);
        
        // Chỉ cho phép Admin hoặc người tạo xóa bài tập
        bool isAdmin = user.Role == UserRole.Admin;
        bool isOwner = exercise.CreatedById == userId;
        
        if (!isAdmin && !isOwner)
        {
            return Forbid("Bạn không có quyền xóa bài tập này");
        }

        await _exerciseRepository.DeleteAsync(exercise);
        await _exerciseRepository.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ExerciseResponseDto>>> Search([FromQuery] string name)
    {
        var exercises = await _exerciseRepository.FindAsync(e => e.Name.Contains(name));
        var exerciseDtos = exercises.Select(e => new ExerciseResponseDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Sets = e.Sets,
            Reps = e.Reps,
            RestTime = e.RestTime,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });
        return Ok(exerciseDtos);
    }
} 