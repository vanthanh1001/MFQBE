using System;
using System.Collections.Generic;

namespace Models
{
    public class Exercise
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int RestTime { get; set; } // in seconds
        public int CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual User CreatedBy { get; set; }
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; }
    }
} 