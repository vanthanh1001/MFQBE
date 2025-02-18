using System;

namespace Models
{
    public class UserChallenge
    {
        public int UserId { get; set; }
        public int ChallengeId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public int Points { get; set; }
        public bool IsCompleted { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Challenge Challenge { get; set; }
    }
} 