using System;
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string ProfilePicture { get; set; }
    }

    public class UpdateProfileDto
    {
        [StringLength(50)]
        public string FirstName { get; set; }
        
        [StringLength(50)]
        public string LastName { get; set; }
        
        [Phone]
        public string PhoneNumber { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public string ProfilePicture { get; set; }
    }

    public class UserStatisticsDto
    {
        public int TotalWorkouts { get; set; }
        public int CompletedChallenges { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalPoints { get; set; }
        public string CurrentLevel { get; set; }
    }
} 