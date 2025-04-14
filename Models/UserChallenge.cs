using System;

namespace Models
{
    public class UserChallenge
    {
        public int UserId { get; set; }
        public int ChallengeId { get; set; }
        public int Points { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Challenge Challenge { get; set; }
    }
} 