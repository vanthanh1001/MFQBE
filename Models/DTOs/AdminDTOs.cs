using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Models.Enums;

namespace Models.DTOs
{
    #region User Management
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public UserRole Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UserDetailDto : AdminUserDto
    {
        public List<UserActivityDto> RecentActivities { get; set; }
        public UserStatisticsDto Statistics { get; set; }
        public List<SubscriptionDto> Subscriptions { get; set; }
        public List<PaymentDto> RecentPayments { get; set; }
    }

    public class UserActivityDto
    {
        public int Id { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string? RelatedEntityId { get; set; }
    }

    public class CreateAdminDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        public string DisplayName { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ResetUserPasswordDto
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
    #endregion

    #region Content Management
    public class ContentReviewDto
    {
        public int Id { get; set; }
        public string ContentType { get; set; }
        public int ContentId { get; set; }
        public string ContentName { get; set; }
        public string CreatorName { get; set; }
        public int CreatorId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; }
        public string? ReviewerNote { get; set; }
    }

    public class ContentReviewActionDto
    {
        [Required]
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
    #endregion

    #region Payment Management
    public class PaymentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenew { get; set; }
    }

    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
        public List<string> Features { get; set; }
    }

    public class CreateSubscriptionPlanDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        [Range(0, 1000000)]
        public decimal Price { get; set; }
        
        [Required]
        public string Currency { get; set; }
        
        [Required]
        [Range(1, 3650)]
        public int DurationDays { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public List<string> Features { get; set; }
    }
    #endregion

    #region Analytics
    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalTrainers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalExercises { get; set; }
        public int TotalWorkoutPlans { get; set; }
        public int TotalWorkoutSessions { get; set; }
        public int TotalChallenges { get; set; }
        public int NewUsersLast30Days { get; set; }
        public List<DateCount> UserRegistrationsByDate { get; set; }
    }

    public class RevenueStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenuePreviousMonth { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public List<DateAmount> RevenueByDate { get; set; }
        public List<PlanRevenue> RevenueByPlan { get; set; }
    }

    public class PlanRevenue
    {
        public string PlanName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int SubscriptionCount { get; set; }
    }

    public class UserActivityStatisticsDto
    {
        public int DailyActiveUsers { get; set; }
        public int WeeklyActiveUsers { get; set; }
        public int MonthlyActiveUsers { get; set; }
        public List<DateCount> LoginsByDate { get; set; }
        public List<DateCount> WorkoutSessionsByDate { get; set; }
        public List<ActivityCount> MostPopularActivities { get; set; }
    }

    public class ActivityCount
    {
        public string ActivityName { get; set; }
        public int Count { get; set; }
    }

    public class DateAmount
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
    #endregion

    #region Common
    public class ChangeRoleRequest
    {
        [Required]
        public UserRole NewRole { get; set; }
    }

    public class ChangeStatusRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class DateCount
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
    #endregion
} 