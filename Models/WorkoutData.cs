using System;
using System.Collections.Generic;

namespace FitnessApp.API.Models;

public class WorkoutData
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; }
    public int Duration { get; set; }
    public string Notes { get; set; }
}

public class ExerciseData
{
    public string Name { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int Weight { get; set; }
} 