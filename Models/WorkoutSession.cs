using System;
using System.Collections.Generic;

namespace Models
{
    public class WorkoutSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? WorkoutPlanId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DurationInMinutes { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual WorkoutPlan WorkoutPlan { get; set; }
        public virtual ICollection<ExercisePerformance> Performances { get; set; }
    }

    public class ExercisePerformance
    {
        public int Id { get; set; }
        public int WorkoutSessionId { get; set; }
        public int ExerciseId { get; set; }
        public int ActualSets { get; set; }
        public int ActualReps { get; set; } 
        public int WeightUsed { get; set; }
        public int ActualRestTime { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual WorkoutSession WorkoutSession { get; set; }
        public virtual Exercise Exercise { get; set; }
    }
} 