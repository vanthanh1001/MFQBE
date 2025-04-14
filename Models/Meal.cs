using System;
using System.Collections.Generic;

namespace Models
{
    public class Meal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Breakfast, Lunch, Dinner, Snack
        public int Calories { get; set; }
        public string Ingredients { get; set; } // JSON string of ingredients
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
} 