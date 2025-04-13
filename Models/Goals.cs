using System;
using System.Collections.Generic;

namespace Models
{
    public class FitnessGoal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // weight, strength, endurance, etc.
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime Deadline { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public virtual User User { get; set; }
    }
} 