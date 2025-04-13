using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Enums
{
    public enum UserRole
    {
        User = 0,
        Trainer = 1,
        Admin = 2
    }
}

namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirebaseUid { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<Exercise> CreatedExercises { get; set; }
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; }
        public virtual ICollection<UserChallenge> UserChallenges { get; set; }
        public virtual ICollection<Nutrition> Nutritions { get; set; }
        public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; }
    }
} 