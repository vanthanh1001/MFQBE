using System;
using System.Collections.Generic;

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

    public class NutritionTrackingDto
    {
        public DateTime Date { get; set; }
        public int TotalCalories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }
        public List<MealDto> Meals { get; set; }
    }
} 