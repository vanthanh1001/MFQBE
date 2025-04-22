using Microsoft.EntityFrameworkCore;
using Models;
using Models.DTOs;
using Models.Enums;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPlanDto>> GetAllSubscriptionPlansAsync()
        {
            var plansFromDb = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .ToListAsync();
            
            return plansFromDb.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = "VND",
                DurationDays = p.DurationInDays,
                IsActive = p.IsActive,
                Features = string.IsNullOrEmpty(p.Features) ? new List<string>() : p.Features.Split(',').ToList()
            }).ToList();
        }

        public async Task<SubscriptionPlanDetailDto> GetSubscriptionPlanByIdAsync(int id)
        {
            var planFromDb = await _context.SubscriptionPlans
                .Include(p => p.UserSubscriptions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (planFromDb == null)
                return null;

            return new SubscriptionPlanDetailDto
            {
                Id = planFromDb.Id,
                Name = planFromDb.Name,
                Description = planFromDb.Description,
                Price = planFromDb.Price,
                DurationInDays = planFromDb.DurationInDays,
                IsActive = planFromDb.IsActive,
                Features = planFromDb.Features,
                CreatedAt = planFromDb.CreatedAt,
                UpdatedAt = planFromDb.UpdatedAt,
                ActiveSubscribersCount = planFromDb.UserSubscriptions.Count(us => us.Status == SubscriptionStatus.Active && us.EndDate >= DateTime.UtcNow)
            };
        }

        public async Task<SubscriptionPlanDto> CreateSubscriptionPlanAsync(CreateSubscriptionPlanDto request)
        {
            string featuresString = request.Features != null ? string.Join(",", request.Features) : null;
            
            var plan = new SubscriptionPlan
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                DurationInDays = request.DurationDays,
                IsActive = request.IsActive,
                Features = featuresString,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            return new SubscriptionPlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                Currency = request.Currency,
                DurationDays = plan.DurationInDays,
                IsActive = plan.IsActive,
                Features = request.Features ?? new List<string>()
            };
        }

        public async Task<bool> UpdateSubscriptionPlanAsync(int id, CreateSubscriptionPlanDto request)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null)
                return false;

            string featuresString = request.Features != null ? string.Join(",", request.Features) : null;

            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.Price = request.Price;
            plan.DurationInDays = request.DurationDays;
            plan.IsActive = request.IsActive;
            plan.Features = featuresString;
            plan.UpdatedAt = DateTime.UtcNow;

            _context.SubscriptionPlans.Update(plan);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserSubscriptionDto> SubscribeAsync(int userId, CreateUserSubscriptionDto request)
        {
            // Kiểm tra gói tập có tồn tại không
            var plan = await _context.SubscriptionPlans.FindAsync(request.SubscriptionPlanId);
            if (plan == null)
                return null;

            if (!plan.IsActive)
                return null;

            // Tìm thông tin user
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            // Kiểm tra xem người dùng có đang có gói tập đang hoạt động không
            var hasActiveSubscription = await CheckIfUserHasActiveSubscriptionAsync(userId);
            if (hasActiveSubscription)
                return null;

            // Tạo đăng ký mới
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(plan.DurationInDays);

            var subscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                StartDate = startDate,
                EndDate = endDate,
                Status = SubscriptionStatus.Active,
                PaidAmount = plan.Price,
                PaymentMethod = request.PaymentMethod,
                TransactionId = request.TransactionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return new UserSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                Username = user.Username,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                SubscriptionPlanName = plan.Name,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status,
                PaidAmount = subscription.PaidAmount
            };
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetUserSubscriptionsAsync(int userId, bool activeOnly = false)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<UserSubscriptionDto>();

            var query = _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Where(us => us.UserId == userId);

            if (activeOnly)
                query = query.Where(us => us.Status == SubscriptionStatus.Active && us.EndDate >= DateTime.UtcNow);

            var subscriptions = await query
                .OrderByDescending(us => us.CreatedAt)
                .ToListAsync();

            return subscriptions.Select(us => new UserSubscriptionDto
            {
                Id = us.Id,
                UserId = us.UserId,
                Username = user.Username,
                SubscriptionPlanId = us.SubscriptionPlanId,
                SubscriptionPlanName = us.SubscriptionPlan.Name,
                StartDate = us.StartDate,
                EndDate = us.EndDate,
                Status = us.Status,
                PaidAmount = us.PaidAmount
            }).ToList();
        }

        public async Task<bool> CancelSubscriptionAsync(int userId, int subscriptionId)
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(us => us.Id == subscriptionId && us.UserId == userId);

            if (subscription == null)
                return false;

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.UpdatedAt = DateTime.UtcNow;

            _context.UserSubscriptions.Update(subscription);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateSubscriptionStatusAsync(int subscriptionId, SubscriptionStatus status)
        {
            var subscription = await _context.UserSubscriptions.FindAsync(subscriptionId);
            if (subscription == null)
                return false;

            subscription.Status = status;
            subscription.UpdatedAt = DateTime.UtcNow;

            _context.UserSubscriptions.Update(subscription);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CheckIfUserHasActiveSubscriptionAsync(int userId)
        {
            return await _context.UserSubscriptions
                .AnyAsync(us => us.UserId == userId && us.Status == SubscriptionStatus.Active && us.EndDate >= DateTime.UtcNow);
        }
    }
} 