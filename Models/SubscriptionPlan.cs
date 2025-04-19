using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }
        
        [Required]
        public int DurationInDays { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(255)]
        public string Features { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; }
    }
} 