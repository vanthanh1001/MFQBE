using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DTOs;
using Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitnessApp.API.Models;

namespace Controllers
{
    [ApiController]
    [Route("api/admin/payment")]
    [Authorize]
    public class AdminPaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminPaymentController> _logger;

        public AdminPaymentController(
            ApplicationDbContext dbContext,
            ILogger<AdminPaymentController> logger)
        {
            _dbContext = dbContext;
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

            var currentUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);

            return currentUser != null && currentUser.Role == UserRole.Admin;
        }

        #region Subscription Plans
        [HttpGet("subscription-plans")]
        public async Task<ActionResult<ApiResponse<List<SubscriptionPlanDto>>>> GetSubscriptionPlans()
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Trong một ứng dụng thực tế, bạn sẽ có bảng SubscriptionPlans
                // Ở đây tạo dữ liệu mẫu để demo
                var plans = new List<SubscriptionPlanDto>
                {
                    new SubscriptionPlanDto
                    {
                        Id = 1,
                        Name = "Basic",
                        Description = "Gói cơ bản với các tính năng chính",
                        Price = 99000,
                        Currency = "VND",
                        DurationDays = 30,
                        IsActive = true,
                        Features = new List<string> { "Theo dõi tập luyện", "Tạo workout plan", "Tạo meal plan" }
                    },
                    new SubscriptionPlanDto
                    {
                        Id = 2,
                        Name = "Premium",
                        Description = "Gói premium với đầy đủ tính năng",
                        Price = 199000,
                        Currency = "VND",
                        DurationDays = 30,
                        IsActive = true,
                        Features = new List<string> { "Theo dõi tập luyện", "Tạo workout plan", "Tạo meal plan", "Tham gia challenge", "Nhận hỗ trợ từ huấn luyện viên" }
                    },
                    new SubscriptionPlanDto
                    {
                        Id = 3,
                        Name = "Annual Premium",
                        Description = "Gói premium theo năm",
                        Price = 1990000,
                        Currency = "VND",
                        DurationDays = 365,
                        IsActive = true,
                        Features = new List<string> { "Theo dõi tập luyện", "Tạo workout plan", "Tạo meal plan", "Tham gia challenge", "Nhận hỗ trợ từ huấn luyện viên", "Lập kế hoạch tập luyện nâng cao" }
                    }
                };

                return Ok(new ApiResponse<List<SubscriptionPlanDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách gói đăng ký thành công",
                    Data = plans
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscription plans: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<SubscriptionPlanDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách gói đăng ký",
                    Data = null
                });
            }
        }

        [HttpPost("subscription-plans")]
        public async Task<ActionResult<ApiResponse<SubscriptionPlanDto>>> CreateSubscriptionPlan(
            [FromBody] CreateSubscriptionPlanDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Trong ứng dụng thực tế, bạn sẽ lưu vào database
                // Ở đây tạo một DTO mới với ID giả định
                var newPlan = new SubscriptionPlanDto
                {
                    Id = 4, // ID giả định
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Currency = request.Currency,
                    DurationDays = request.DurationDays,
                    IsActive = request.IsActive,
                    Features = request.Features ?? new List<string>()
                };

                return Ok(new ApiResponse<SubscriptionPlanDto>
                {
                    Success = true,
                    Message = "Tạo gói đăng ký mới thành công",
                    Data = newPlan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating subscription plan: {ex.Message}");
                return StatusCode(500, new ApiResponse<SubscriptionPlanDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo gói đăng ký mới",
                    Data = null
                });
            }
        }

        [HttpPut("subscription-plans/{planId}")]
        public async Task<ActionResult<ApiResponse<SubscriptionPlanDto>>> UpdateSubscriptionPlan(
            int planId,
            [FromBody] CreateSubscriptionPlanDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Giả định kịch bản mô phỏng cập nhật
                if (planId < 1 || planId > 3)
                {
                    return NotFound(new ApiResponse<SubscriptionPlanDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy gói đăng ký",
                        Data = null
                    });
                }

                var updatedPlan = new SubscriptionPlanDto
                {
                    Id = planId,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Currency = request.Currency,
                    DurationDays = request.DurationDays,
                    IsActive = request.IsActive,
                    Features = request.Features ?? new List<string>()
                };

                return Ok(new ApiResponse<SubscriptionPlanDto>
                {
                    Success = true,
                    Message = "Cập nhật gói đăng ký thành công",
                    Data = updatedPlan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating subscription plan: {ex.Message}");
                return StatusCode(500, new ApiResponse<SubscriptionPlanDto>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật gói đăng ký",
                    Data = null
                });
            }
        }
        #endregion

        #region User Subscriptions
        [HttpGet("subscriptions")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<SubscriptionDto>>>> GetSubscriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Tạo dữ liệu mẫu
                var subscriptions = new List<SubscriptionDto>();
                
                for (int i = 1; i <= 20; i++)
                {
                    string subscriptionStatus = i % 3 == 0 ? "Expired" : (i % 4 == 0 ? "Cancelled" : "Active");
                    
                    // Chỉ thêm vào danh sách nếu không có filter hoặc khớp với filter
                    if (string.IsNullOrEmpty(status) || subscriptionStatus == status)
                    {
                        subscriptions.Add(new SubscriptionDto
                        {
                            Id = i,
                            UserId = 100 + i,
                            Username = $"user{100 + i}",
                            PlanName = i % 3 == 0 ? "Basic" : (i % 2 == 0 ? "Premium" : "Annual Premium"),
                            Price = i % 3 == 0 ? 99000 : (i % 2 == 0 ? 199000 : 1990000),
                            Currency = "VND",
                            Status = subscriptionStatus,
                            StartDate = DateTime.UtcNow.AddDays(-30),
                            EndDate = DateTime.UtcNow.AddDays(i % 3 == 0 ? -1 : (i % 2 == 0 ? 30 : 335)),
                            AutoRenew = i % 5 != 0
                        });
                    }
                }

                // Lọc theo status nếu được chỉ định
                if (!string.IsNullOrEmpty(status))
                {
                    subscriptions = subscriptions.Where(s => s.Status == status).ToList();
                }

                // Phân trang
                var totalItems = subscriptions.Count;
                var paginatedItems = subscriptions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedResult<SubscriptionDto>
                {
                    Items = paginatedItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<SubscriptionDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách đăng ký thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscriptions: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<SubscriptionDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách đăng ký",
                    Data = null
                });
            }
        }

        [HttpGet("subscriptions/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetSubscriptionDetail(int subscriptionId)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Giả lập việc lấy chi tiết đăng ký từ database
                if (subscriptionId < 1 || subscriptionId > 20)
                {
                    return NotFound(new ApiResponse<SubscriptionDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin đăng ký",
                        Data = null
                    });
                }

                string subscriptionStatus = subscriptionId % 3 == 0 ? "Expired" : (subscriptionId % 4 == 0 ? "Cancelled" : "Active");
                
                var subscription = new SubscriptionDto
                {
                    Id = subscriptionId,
                    UserId = 100 + subscriptionId,
                    Username = $"user{100 + subscriptionId}",
                    PlanName = subscriptionId % 3 == 0 ? "Basic" : (subscriptionId % 2 == 0 ? "Premium" : "Annual Premium"),
                    Price = subscriptionId % 3 == 0 ? 99000 : (subscriptionId % 2 == 0 ? 199000 : 1990000),
                    Currency = "VND",
                    Status = subscriptionStatus,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(subscriptionId % 3 == 0 ? -1 : (subscriptionId % 2 == 0 ? 30 : 335)),
                    AutoRenew = subscriptionId % 5 != 0
                };

                return Ok(new ApiResponse<SubscriptionDto>
                {
                    Success = true,
                    Message = "Lấy thông tin đăng ký thành công",
                    Data = subscription
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting subscription detail: {ex.Message}");
                return StatusCode(500, new ApiResponse<SubscriptionDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin đăng ký",
                    Data = null
                });
            }
        }
        #endregion

        #region Payments
        [HttpGet("payments")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<PaymentDto>>>> GetPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Tạo dữ liệu mẫu
                var payments = new List<PaymentDto>();
                
                for (int i = 1; i <= 25; i++)
                {
                    string paymentStatus = i % 3 == 0 ? "Failed" : (i % 7 == 0 ? "Pending" : "Completed");
                    
                    // Chỉ thêm vào danh sách nếu không có filter hoặc khớp với filter
                    if (string.IsNullOrEmpty(status) || paymentStatus == status)
                    {
                        payments.Add(new PaymentDto
                        {
                            Id = i,
                            UserId = 100 + i,
                            Username = $"user{100 + i}",
                            Amount = i % 3 == 0 ? 99000 : (i % 2 == 0 ? 199000 : 1990000),
                            Currency = "VND",
                            PaymentMethod = i % 3 == 0 ? "MoMo" : (i % 2 == 0 ? "VNPay" : "Bank Transfer"),
                            TransactionId = $"TR{DateTime.UtcNow.Ticks}-{i}",
                            Status = paymentStatus,
                            CreatedAt = DateTime.UtcNow.AddDays(-i),
                            CompletedAt = paymentStatus == "Completed" ? DateTime.UtcNow.AddDays(-i).AddHours(1) : null
                        });
                    }
                }

                // Lọc theo status nếu được chỉ định
                if (!string.IsNullOrEmpty(status))
                {
                    payments = payments.Where(p => p.Status == status).ToList();
                }

                // Phân trang
                var totalItems = payments.Count;
                var paginatedItems = payments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedResult<PaymentDto>
                {
                    Items = paginatedItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<PaymentDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách thanh toán thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payments: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<PaymentDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách thanh toán",
                    Data = null
                });
            }
        }

        [HttpGet("revenue-statistics")]
        public async Task<ActionResult<ApiResponse<RevenueStatisticsDto>>> GetRevenueStatistics()
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Tạo dữ liệu thống kê doanh thu mẫu
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                
                var revenueStats = new RevenueStatisticsDto
                {
                    TotalRevenue = 25789000,
                    RevenueThisMonth = 4356000,
                    RevenuePreviousMonth = 3879000,
                    RevenueGrowthPercentage = 12.3m,
                    TotalSubscriptions = 120,
                    ActiveSubscriptions = 98,
                    RevenueByDate = GenerateRevenueByDate(),
                    RevenueByPlan = new List<PlanRevenue>
                    {
                        new PlanRevenue { PlanName = "Basic", TotalRevenue = 7890000, SubscriptionCount = 80 },
                        new PlanRevenue { PlanName = "Premium", TotalRevenue = 9800000, SubscriptionCount = 50 },
                        new PlanRevenue { PlanName = "Annual Premium", TotalRevenue = 8099000, SubscriptionCount = 4 }
                    }
                };

                return Ok(new ApiResponse<RevenueStatisticsDto>
                {
                    Success = true,
                    Message = "Lấy thống kê doanh thu thành công",
                    Data = revenueStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting revenue statistics: {ex.Message}");
                return StatusCode(500, new ApiResponse<RevenueStatisticsDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thống kê doanh thu",
                    Data = null
                });
            }
        }
        #endregion

        #region Helper Methods
        private List<DateAmount> GenerateRevenueByDate()
        {
            var result = new List<DateAmount>();
            var today = DateTime.UtcNow.Date;
            var random = new Random();

            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var amount = (decimal)(random.Next(50, 500) * 1000);
                
                result.Add(new DateAmount
                {
                    Date = date,
                    Amount = amount
                });
            }

            return result;
        }
        #endregion
    }
} 