using System;
using System.Collections.Generic;

namespace Models
{
    public class WorkoutPlan
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; }
    }
} 