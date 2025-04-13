using System;
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs
{
    public class CreateExerciseDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(1, 100)]
        public int Sets { get; set; }

        [Required]
        [Range(1, 100)]
        public int Reps { get; set; }

        [Required]
        [Range(0, 300)]
        public int RestTime { get; set; }
    }

    public class UpdateExerciseDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(1, 100)]
        public int Sets { get; set; }

        [Required]
        [Range(1, 100)]
        public int Reps { get; set; }

        [Required]
        [Range(0, 300)]
        public int RestTime { get; set; }
    }

    public class ExerciseResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int RestTime { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 