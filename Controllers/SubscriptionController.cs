using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DTOs;
using Models.Enums;
using FitnessApp.API.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            ISubscriptionService subscriptionService, 
            ApplicationDbContext context,
            ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _context = context;
            _logger = logger;
        }

        // Kiểm tra xem người dùng hiện tại có phải là admin không
        private async Task<bool> IsCurrentUserAdmin()
        {
            var firebaseUid = User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return false;
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);

            return currentUser != null && currentUser.Role == UserRole.Admin;
        }

        #region Public Endpoints

        [HttpGet("plans")]
        public async Task<ActionResult<ApiResponse<IEnumerable<SubscriptionPlanDto>>>> GetSubscriptionPlans()
        {
            try
            {
                var plans = await _subscriptionService.GetAllSubscriptionPlansAsync();
                return Ok(new ApiResponse<IEnumerable<SubscriptionPlanDto>> 
                { 
                    Success = true, 
                    Data = plans 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscription plans: {ex.Message}");
                return StatusCode(500, new ApiResponse<IEnumerable<SubscriptionPlanDto>> 
                { 
                    Success = false, 
                    Message = "Lỗi khi lấy thông tin gói tập",
                    Data = null
                });
            }
        }

        [HttpGet("plans/{id}")]
        public async Task<ActionResult<ApiResponse<SubscriptionPlanDetailDto>>> GetSubscriptionPlanById(int id)
        {
            try
            {
                var plan = await _subscriptionService.GetSubscriptionPlanByIdAsync(id);
                
                if (plan == null)
                    return NotFound(new ApiResponse<SubscriptionPlanDetailDto>
                    {
                        Success = false,
                        Message = $"Gói tập với ID {id} không tồn tại",
                        Data = null
                    });

                return Ok(new ApiResponse<SubscriptionPlanDetailDto>
                {
                    Success = true,
                    Data = plan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscription plan {id}: {ex.Message}");
                return StatusCode(500, new ApiResponse<SubscriptionPlanDetailDto> 
                { 
                    Success = false, 
                    Message = "Lỗi khi lấy thông tin gói tập",
                    Data = null
                });
            }
        }

        [HttpPost("subscribe")]
        public async Task<ActionResult<ApiResponse<UserSubscriptionDto>>> Subscribe([FromBody] CreateUserSubscriptionDto request)
        {
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
                return Unauthorized(new ApiResponse<UserSubscriptionDto>
                {
                    Success = false,
                    Message = "Vui lòng đăng nhập để tiếp tục",
                    Data = null
                });
                
            // Tìm userId từ firebaseUid
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
            if (user == null)
                return Unauthorized(new ApiResponse<UserSubscriptionDto>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại",
                    Data = null
                });
            
            var userId = user.Id;

            try 
            {
                var subscription = await _subscriptionService.SubscribeAsync(userId, request);
                
                if (subscription == null)
                    return BadRequest(new ApiResponse<UserSubscriptionDto>
                    {
                        Success = false,
                        Message = "Không thể đăng ký gói tập. Vui lòng kiểm tra thông tin và thử lại.",
                        Data = null
                    });

                return Ok(new ApiResponse<UserSubscriptionDto>
                { 
                    Success = true,
                    Message = "Đăng ký gói tập thành công", 
                    Data = subscription 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error subscribing user {userId} to plan {request.SubscriptionPlanId}: {ex.Message}");
                return StatusCode(500, new ApiResponse<UserSubscriptionDto>
                { 
                    Success = false, 
                    Message = "Lỗi đăng ký gói tập",
                    Data = null
                });
            }
        }

        [HttpGet("my-subscriptions")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserSubscriptionDto>>>> GetMySubscriptions([FromQuery] bool activeOnly = false)
        {
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
                return Unauthorized(new ApiResponse<IEnumerable<UserSubscriptionDto>>
                {
                    Success = false,
                    Message = "Vui lòng đăng nhập để tiếp tục",
                    Data = null
                });
                
            // Tìm userId từ firebaseUid
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
            if (user == null)
                return Unauthorized(new ApiResponse<IEnumerable<UserSubscriptionDto>>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại",
                    Data = null
                });
            
            var userId = user.Id;
            
            try
            {
                var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId, activeOnly);
                return Ok(new ApiResponse<IEnumerable<UserSubscriptionDto>>
                { 
                    Success = true, 
                    Data = subscriptions 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscriptions for user {userId}: {ex.Message}");
                return StatusCode(500, new ApiResponse<IEnumerable<UserSubscriptionDto>>
                { 
                    Success = false, 
                    Message = "Lỗi lấy thông tin gói tập",
                    Data = null
                });
            }
        }

        [HttpPut("cancel/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> CancelSubscription(int id)
        {
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Vui lòng đăng nhập để tiếp tục",
                    Data = null
                });
                
            // Tìm userId từ firebaseUid
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
            if (user == null)
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại",
                    Data = null
                });
            
            var userId = user.Id;

            try
            {
                var result = await _subscriptionService.CancelSubscriptionAsync(userId, id);
                if (!result)
                    return NotFound(new ApiResponse<object>
                    { 
                        Success = false, 
                        Message = $"Không tìm thấy gói đăng ký với ID {id} hoặc bạn không có quyền hủy gói này",
                        Data = null
                    });

                return Ok(new ApiResponse<object>
                { 
                    Success = true, 
                    Message = "Hủy gói tập thành công",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling subscription {id} for user {userId}: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                { 
                    Success = false, 
                    Message = "Lỗi hủy gói tập",
                    Data = null
                });
            }
        }

        [HttpGet("check-active")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckActiveSubscription()
        {
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Vui lòng đăng nhập để tiếp tục",
                    Data = false
                });
                
            // Tìm userId từ firebaseUid
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
            if (user == null)
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại",
                    Data = false
                });
            
            var userId = user.Id;

            try
            {
                var hasActiveSubscription = await _subscriptionService.CheckIfUserHasActiveSubscriptionAsync(userId);
                return Ok(new ApiResponse<bool>
                { 
                    Success = true, 
                    Data = hasActiveSubscription
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking active subscription for user {userId}: {ex.Message}");
                return StatusCode(500, new ApiResponse<bool>
                { 
                    Success = false, 
                    Message = "Lỗi kiểm tra gói tập",
                    Data = false
                });
            }
        }

        #endregion

        #region Admin Endpoints

        [HttpPost("admin/plans")]
        public async Task<ActionResult<ApiResponse<SubscriptionPlanDto>>> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var planDto = await _subscriptionService.CreateSubscriptionPlanAsync(request);
                return Ok(new ApiResponse<SubscriptionPlanDto>
                {
                    Success = true,
                    Message = "Tạo gói tập thành công",
                    Data = planDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating subscription plan: {ex.Message}");
                return StatusCode(500, new ApiResponse<SubscriptionPlanDto> 
                { 
                    Success = false, 
                    Message = "Lỗi tạo gói tập",
                    Data = null
                });
            }
        }

        [HttpPut("admin/plans/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSubscriptionPlan(int id, [FromBody] CreateSubscriptionPlanDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var result = await _subscriptionService.UpdateSubscriptionPlanAsync(id, request);
                if (!result)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Gói tập với ID {id} không tồn tại",
                        Data = null
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật gói tập thành công",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating subscription plan {id}: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Lỗi cập nhật gói tập",
                    Data = null
                });
            }
        }

        [HttpGet("admin/users/{userId}/subscriptions")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserSubscriptionDto>>>> GetUserSubscriptions(int userId)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);
                return Ok(new ApiResponse<IEnumerable<UserSubscriptionDto>>
                { 
                    Success = true, 
                    Data = subscriptions 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscriptions for user {userId}: {ex.Message}");
                return StatusCode(500, new ApiResponse<IEnumerable<UserSubscriptionDto>>
                { 
                    Success = false, 
                    Message = "Lỗi lấy thông tin gói tập",
                    Data = null
                });
            }
        }

        [HttpPut("admin/subscriptions/{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSubscriptionStatus(int id, [FromBody] SubscriptionStatusUpdateDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var result = await _subscriptionService.UpdateSubscriptionStatusAsync(id, request.Status);
                if (!result)
                    return NotFound(new ApiResponse<object>
                    { 
                        Success = false, 
                        Message = $"Không tìm thấy gói đăng ký với ID {id}",
                        Data = null
                    });

                return Ok(new ApiResponse<object>
                { 
                    Success = true, 
                    Message = "Cập nhật trạng thái gói tập thành công",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating subscription status for {id}: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                { 
                    Success = false, 
                    Message = "Lỗi cập nhật trạng thái gói tập",
                    Data = null
                });
            }
        }

        [HttpGet("admin/subscriptions")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserSubscriptionDto>>>> GetAllSubscriptions(
            [FromQuery] bool activeOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Lấy tất cả đăng ký từ tất cả người dùng
                var query = _context.UserSubscriptions
                    .Include(us => us.User)
                    .Include(us => us.SubscriptionPlan)
                    .AsQueryable();

                if (activeOnly)
                {
                    query = query.Where(us => us.Status == SubscriptionStatus.Active && us.EndDate >= DateTime.UtcNow);
                }

                var totalCount = await query.CountAsync();
                
                var subscriptions = await query
                    .OrderByDescending(us => us.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(us => new UserSubscriptionDto
                    {
                        Id = us.Id,
                        UserId = us.UserId,
                        Username = us.User.Username,
                        SubscriptionPlanId = us.SubscriptionPlanId,
                        SubscriptionPlanName = us.SubscriptionPlan.Name,
                        StartDate = us.StartDate,
                        EndDate = us.EndDate,
                        Status = us.Status,
                        PaidAmount = us.PaidAmount
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new 
                    {
                        Items = subscriptions,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all subscriptions: {ex.Message}");
                return StatusCode(500, new ApiResponse<IEnumerable<UserSubscriptionDto>>
                {
                    Success = false,
                    Message = "Lỗi lấy danh sách đăng ký gói tập",
                    Data = null
                });
            }
        }

        [HttpGet("admin/stats")]
        public async Task<ActionResult<ApiResponse<object>>> GetSubscriptionStats()
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var now = DateTime.UtcNow;
                
                // Tổng số gói tập
                var totalPlans = await _context.SubscriptionPlans.CountAsync();
                
                // Số gói tập đang hoạt động
                var activePlans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .CountAsync();
                
                // Tổng số đăng ký
                var totalSubscriptions = await _context.UserSubscriptions.CountAsync();
                
                // Số đăng ký đang hoạt động
                var activeSubscriptions = await _context.UserSubscriptions
                    .Where(us => us.Status == SubscriptionStatus.Active && us.EndDate >= now)
                    .CountAsync();
                
                // Doanh thu trong tháng hiện tại
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                
                var currentMonthRevenue = await _context.UserSubscriptions
                    .Where(us => us.CreatedAt >= startOfMonth && us.CreatedAt <= endOfMonth)
                    .SumAsync(us => us.PaidAmount);
                
                // Lấy 5 gói tập phổ biến nhất
                var popularPlans = await _context.SubscriptionPlans
                    .Select(p => new 
                    {
                        Plan = p,
                        SubscriptionCount = _context.UserSubscriptions.Count(us => us.SubscriptionPlanId == p.Id)
                    })
                    .OrderByDescending(x => x.SubscriptionCount)
                    .Take(5)
                    .Select(x => new
                    {
                        Id = x.Plan.Id,
                        Name = x.Plan.Name,
                        Price = x.Plan.Price,
                        SubscriptionCount = x.SubscriptionCount
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        TotalPlans = totalPlans,
                        ActivePlans = activePlans,
                        TotalSubscriptions = totalSubscriptions,
                        ActiveSubscriptions = activeSubscriptions,
                        CurrentMonthRevenue = currentMonthRevenue,
                        PopularPlans = popularPlans
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscription stats: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi lấy thống kê gói tập",
                    Data = null
                });
            }
        }

        #endregion
    }
} 