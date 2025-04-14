using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs
{
    public class MealPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<MealDto> Meals { get; set; }
    }

    public class MealDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Breakfast, Lunch, Dinner, Snack
        public int Calories { get; set; }
        public List<string> Ingredients { get; set; }
    }

    public class CreateMealDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Type { get; set; } // Breakfast, Lunch, Dinner, Snack
        
        [Required]
        [Range(0, 5000)]
        public int Calories { get; set; }
        
        public List<string> Ingredients { get; set; } = new List<string>();
    }

    public class UpdateMealDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Type { get; set; }
        
        [Required]
        [Range(0, 5000)]
        public int Calories { get; set; }
        
        public List<string> Ingredients { get; set; } = new List<string>();
    }

    public class NutritionTrackingDto
    {
        public DateTime Date { get; set; }
        public int TotalCalories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }
        public List<MealDto> Meals { get; set; } = new List<MealDto>();
    }

    public class CreateNutritionDto
    {
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(0, 10000)]
        public int TotalCalories { get; set; }
        
        [Required]
        [Range(0, 1000)]
        public int ProteinGrams { get; set; }
        
        [Required]
        [Range(0, 1000)]
        public int CarbsGrams { get; set; }
        
        [Required]
        [Range(0, 1000)]
        public int FatGrams { get; set; }
    }
} 