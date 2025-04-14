using System;
using System.Collections.Generic;

namespace Models.DTOs
{
    public class ChallengeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ParticipantCount { get; set; }
        public string RewardDescription { get; set; }
        public int RewardPoints { get; set; }
    }

    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public string ProfilePicture { get; set; }
        public int Points { get; set; }
        public string Level { get; set; }
    }
    
    public class UserChallengeDto
    {
        public int ChallengeId { get; set; }
        public string ChallengeName { get; set; }
        public string ChallengeDescription { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime JoinDate { get; set; }
        public int Points { get; set; }
        public int RewardPoints { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class ChallengeLeaderboardEntryDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string ProfilePicture { get; set; }
        public int Points { get; set; }
        public string Level { get; set; }
    }
} 