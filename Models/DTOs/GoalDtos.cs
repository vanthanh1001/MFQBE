using System;
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs
{
    public class FitnessGoalDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime Deadline { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateGoalDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public string Type { get; set; }
        
        [Required]
        [Range(0.01, 1000)]
        public decimal TargetValue { get; set; }
        
        [Range(0, 1000)]
        public decimal CurrentValue { get; set; }
        
        [Required]
        public DateTime Deadline { get; set; }
    }

    public class UpdateGoalProgressDto
    {
        [Required]
        [Range(0, 1000)]
        public decimal CurrentValue { get; set; }
        
        public bool IsCompleted { get; set; }
    }
} 