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
    }

    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public string ProfilePicture { get; set; }
        public int Points { get; set; }
        public string Level { get; set; }
    }
} 