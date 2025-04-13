using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs
{
    public class WorkoutSessionDto
    {
        public int Id { get; set; }
        public int? WorkoutPlanId { get; set; }
        public string WorkoutPlanName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DurationInMinutes { get; set; }
        public string Notes { get; set; }
        public List<ExercisePerformanceDto> Performances { get; set; }
    }

    public class ExercisePerformanceDto
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public int ActualSets { get; set; }
        public int ActualReps { get; set; }
        public int WeightUsed { get; set; }
        public int ActualRestTime { get; set; }
        public string Notes { get; set; }
    }

    public class CreateWorkoutSessionDto
    {
        public int? WorkoutPlanId { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int DurationInMinutes { get; set; }
        
        public string Notes { get; set; }
        
        public List<CreateExercisePerformanceDto> Performances { get; set; }
    }

    public class CreateExercisePerformanceDto
    {
        [Required]
        public int ExerciseId { get; set; }
        
        [Required]
        [Range(1, 100)]
        public int ActualSets { get; set; }
        
        [Required]
        [Range(1, 1000)]
        public int ActualReps { get; set; }
        
        [Required]
        [Range(0, 1000)]
        public int WeightUsed { get; set; }
        
        [Required]
        [Range(0, 600)]
        public int ActualRestTime { get; set; }
        
        public string Notes { get; set; }
    }

    public class UpdateWorkoutSessionDto
    {
        public DateTime? EndTime { get; set; }
        
        [Range(1, int.MaxValue)]
        public int? DurationInMinutes { get; set; }
        
        public string Notes { get; set; }
    }

    public class WorkoutHistoryDto
    {
        public int TotalWorkouts { get; set; }
        public int TotalDuration { get; set; }
        public List<WorkoutSessionDto> RecentSessions { get; set; }
    }
} 