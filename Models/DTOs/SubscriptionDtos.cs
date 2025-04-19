using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Models.Enums;

namespace Models.DTOs
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
        public List<string> Features { get; set; } = new List<string>();
    }

    public class SubscriptionPlanDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public bool IsActive { get; set; }
        public string Features { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ActiveSubscribersCount { get; set; }
    }

    public class UserSubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string SubscriptionPlanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsActive => Status == SubscriptionStatus.Active && DateTime.UtcNow <= EndDate;
        public int RemainingDays => IsActive ? (int)(EndDate - DateTime.UtcNow).TotalDays : 0;
    }

    public class CreateUserSubscriptionDto
    {
        [Required]
        public int SubscriptionPlanId { get; set; }
        
        public string PaymentMethod { get; set; }
        
        public string TransactionId { get; set; }
    }

    public class CreateSubscriptionPlanDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public string Currency { get; set; } = "VND";
        
        [Required]
        public int DurationDays { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public List<string> Features { get; set; }
    }

    public class SubscriptionStatusUpdateDto
    {
        [Required]
        public SubscriptionStatus Status { get; set; }
        
        public string Reason { get; set; }
    }
} 