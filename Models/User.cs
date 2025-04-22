using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Enums;

namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FirebaseUid { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? DateOfBirth { get; set; }

        // Navigation properties
        public virtual ICollection<Exercise> CreatedExercises { get; set; }
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; }
        public virtual ICollection<UserChallenge> UserChallenges { get; set; }
        public virtual ICollection<Nutrition> Nutritions { get; set; }
        public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; }
        public virtual ICollection<UserSubscription> Subscriptions { get; set; }

        // Constructor để khởi tạo danh sách
        public User()
        {
            CreatedExercises = new List<Exercise>();
            WorkoutPlans = new List<WorkoutPlan>();
            WorkoutSessions = new List<WorkoutSession>();
            UserChallenges = new List<UserChallenge>();
            Nutritions = new List<Nutrition>();
            Subscriptions = new List<UserSubscription>();
        }
    }
} 