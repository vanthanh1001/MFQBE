using Models;
using Models.DTOs;
using Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionPlanDto>> GetAllSubscriptionPlansAsync();
        Task<SubscriptionPlanDetailDto> GetSubscriptionPlanByIdAsync(int id);
        Task<SubscriptionPlanDto> CreateSubscriptionPlanAsync(CreateSubscriptionPlanDto request);
        Task<bool> UpdateSubscriptionPlanAsync(int id, CreateSubscriptionPlanDto request);
        Task<UserSubscriptionDto> SubscribeAsync(int userId, CreateUserSubscriptionDto request);
        Task<IEnumerable<UserSubscriptionDto>> GetUserSubscriptionsAsync(int userId, bool activeOnly = false);
        Task<bool> CancelSubscriptionAsync(int userId, int subscriptionId);
        Task<bool> UpdateSubscriptionStatusAsync(int subscriptionId, SubscriptionStatus status);
        Task<bool> CheckIfUserHasActiveSubscriptionAsync(int userId);
    }
} 