using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Enums;

namespace Models
{
    public class UserSubscription
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int SubscriptionPlanId { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [Required]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PaidAmount { get; set; }
        
        public string TransactionId { get; set; }
        
        public string PaymentMethod { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        [ForeignKey("SubscriptionPlanId")]
        public virtual SubscriptionPlan SubscriptionPlan { get; set; }
    }
} 