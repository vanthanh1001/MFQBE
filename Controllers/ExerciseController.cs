using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using Models.DTOs;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExerciseController : ControllerBase
{
    private readonly IRepository<Exercise> _exerciseRepository;

    public ExerciseController(IRepository<Exercise> exerciseRepository)
    {
        _exerciseRepository = exerciseRepository;
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
        var exercise = new Exercise
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Sets = createDto.Sets,
            Reps = createDto.Reps,
            RestTime = createDto.RestTime
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
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = exercise.Id }, responseDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateExerciseDto updateDto)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
            return NotFound();

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
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
            return NotFound();

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