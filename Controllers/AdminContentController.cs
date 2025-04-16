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
    [Route("api/admin/content")]
    [Authorize]
    public class AdminContentController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminContentController> _logger;

        public AdminContentController(
            ApplicationDbContext dbContext,
            ILogger<AdminContentController> logger)
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

        #region Content Review
        [HttpGet("pending-reviews")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<ContentReviewDto>>>> GetPendingReviews(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? contentType = null)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Tạo dữ liệu mẫu cho nội dung đang chờ duyệt
                var pendingReviews = new List<ContentReviewDto>();
                
                // 10 bài tập tự tạo
                for (int i = 1; i <= 10; i++)
                {
                    pendingReviews.Add(new ContentReviewDto
                    {
                        Id = i,
                        ContentType = "Exercise",
                        ContentId = i,
                        ContentName = $"Custom Exercise #{i}",
                        CreatorName = $"user{100 + i}",
                        CreatorId = 100 + i,
                        SubmittedAt = DateTime.UtcNow.AddDays(-i),
                        Status = "Pending",
                        ReviewerNote = null
                    });
                }
                
                // 5 workout plans
                for (int i = 1; i <= 5; i++)
                {
                    pendingReviews.Add(new ContentReviewDto
                    {
                        Id = 10 + i,
                        ContentType = "WorkoutPlan",
                        ContentId = i,
                        ContentName = $"Custom Workout Plan #{i}",
                        CreatorName = $"user{200 + i}",
                        CreatorId = 200 + i,
                        SubmittedAt = DateTime.UtcNow.AddDays(-i),
                        Status = "Pending",
                        ReviewerNote = null
                    });
                }
                
                // 3 challenges
                for (int i = 1; i <= 3; i++)
                {
                    pendingReviews.Add(new ContentReviewDto
                    {
                        Id = 15 + i,
                        ContentType = "Challenge",
                        ContentId = i,
                        ContentName = $"Community Challenge #{i}",
                        CreatorName = $"trainer{100 + i}",
                        CreatorId = 300 + i,
                        SubmittedAt = DateTime.UtcNow.AddDays(-i),
                        Status = "Pending",
                        ReviewerNote = null
                    });
                }
                
                // 2 meal plans
                for (int i = 1; i <= 2; i++)
                {
                    pendingReviews.Add(new ContentReviewDto
                    {
                        Id = 18 + i,
                        ContentType = "MealPlan",
                        ContentId = i,
                        ContentName = $"Custom Diet Plan #{i}",
                        CreatorName = $"trainer{200 + i}",
                        CreatorId = 300 + i,
                        SubmittedAt = DateTime.UtcNow.AddDays(-i),
                        Status = "Pending",
                        ReviewerNote = null
                    });
                }

                // Lọc theo contentType nếu được chỉ định
                if (!string.IsNullOrEmpty(contentType))
                {
                    pendingReviews = pendingReviews.Where(r => r.ContentType == contentType).ToList();
                }

                // Phân trang
                var totalItems = pendingReviews.Count;
                var paginatedItems = pendingReviews
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedResult<ContentReviewDto>
                {
                    Items = paginatedItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<ContentReviewDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách nội dung cần duyệt thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pending reviews: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<ContentReviewDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách nội dung cần duyệt",
                    Data = null
                });
            }
        }

        [HttpGet("reviews")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<ContentReviewDto>>>> GetAllReviews(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? contentType = null,
            [FromQuery] string? status = null)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Tạo dữ liệu mẫu cho tất cả nội dung đã được duyệt hoặc từ chối
                var allReviews = new List<ContentReviewDto>();
                
                // 20 bài đánh giá với trạng thái khác nhau
                for (int i = 1; i <= 20; i++)
                {
                    string reviewStatus = i % 3 == 0 ? "Rejected" : (i % 5 == 0 ? "Pending" : "Approved");
                    string contentTypeValue = i % 4 == 0 ? "MealPlan" : (i % 3 == 0 ? "Challenge" : (i % 2 == 0 ? "WorkoutPlan" : "Exercise"));
                    
                    allReviews.Add(new ContentReviewDto
                    {
                        Id = i,
                        ContentType = contentTypeValue,
                        ContentId = i,
                        ContentName = $"Custom {contentTypeValue} #{i}",
                        CreatorName = $"user{100 + i}",
                        CreatorId = 100 + i,
                        SubmittedAt = DateTime.UtcNow.AddDays(-i),
                        Status = reviewStatus,
                        ReviewerNote = reviewStatus == "Rejected" ? "Không tuân theo quy định cộng đồng" : null
                    });
                }

                // Lọc theo contentType và status nếu được chỉ định
                if (!string.IsNullOrEmpty(contentType))
                {
                    allReviews = allReviews.Where(r => r.ContentType == contentType).ToList();
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    allReviews = allReviews.Where(r => r.Status == status).ToList();
                }

                // Phân trang
                var totalItems = allReviews.Count;
                var paginatedItems = allReviews
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedResult<ContentReviewDto>
                {
                    Items = paginatedItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<ContentReviewDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách đánh giá nội dung thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting reviews: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<ContentReviewDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách đánh giá nội dung",
                    Data = null
                });
            }
        }

        [HttpGet("reviews/{reviewId}")]
        public async Task<ActionResult<ApiResponse<ContentReviewDto>>> GetReviewDetail(int reviewId)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Giả lập việc lấy chi tiết đánh giá từ database
                if (reviewId < 1 || reviewId > 20)
                {
                    return NotFound(new ApiResponse<ContentReviewDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin đánh giá",
                        Data = null
                    });
                }

                string reviewStatus = reviewId % 3 == 0 ? "Rejected" : (reviewId % 5 == 0 ? "Pending" : "Approved");
                string contentTypeValue = reviewId % 4 == 0 ? "MealPlan" : (reviewId % 3 == 0 ? "Challenge" : (reviewId % 2 == 0 ? "WorkoutPlan" : "Exercise"));
                
                var review = new ContentReviewDto
                {
                    Id = reviewId,
                    ContentType = contentTypeValue,
                    ContentId = reviewId,
                    ContentName = $"Custom {contentTypeValue} #{reviewId}",
                    CreatorName = $"user{100 + reviewId}",
                    CreatorId = 100 + reviewId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-reviewId),
                    Status = reviewStatus,
                    ReviewerNote = reviewStatus == "Rejected" ? "Không tuân theo quy định cộng đồng" : null
                };

                return Ok(new ApiResponse<ContentReviewDto>
                {
                    Success = true,
                    Message = "Lấy thông tin đánh giá thành công",
                    Data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting review detail: {ex.Message}");
                return StatusCode(500, new ApiResponse<ContentReviewDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin đánh giá",
                    Data = null
                });
            }
        }

        [HttpPut("reviews/{reviewId}/action")]
        public async Task<ActionResult<ApiResponse<ContentReviewDto>>> ReviewContent(
            int reviewId, 
            [FromBody] ContentReviewActionDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Giả lập việc cập nhật trạng thái đánh giá trong database
                if (reviewId < 1 || reviewId > 20)
                {
                    return NotFound(new ApiResponse<ContentReviewDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin đánh giá",
                        Data = null
                    });
                }

                string contentTypeValue = reviewId % 4 == 0 ? "MealPlan" : (reviewId % 3 == 0 ? "Challenge" : (reviewId % 2 == 0 ? "WorkoutPlan" : "Exercise"));
                string newStatus = request.IsApproved ? "Approved" : "Rejected";
                
                var updatedReview = new ContentReviewDto
                {
                    Id = reviewId,
                    ContentType = contentTypeValue,
                    ContentId = reviewId,
                    ContentName = $"Custom {contentTypeValue} #{reviewId}",
                    CreatorName = $"user{100 + reviewId}",
                    CreatorId = 100 + reviewId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-reviewId),
                    Status = newStatus,
                    ReviewerNote = request.IsApproved ? null : request.RejectionReason
                };

                return Ok(new ApiResponse<ContentReviewDto>
                {
                    Success = true,
                    Message = $"Nội dung đã được {(request.IsApproved ? "phê duyệt" : "từ chối")} thành công",
                    Data = updatedReview
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reviewing content: {ex.Message}");
                return StatusCode(500, new ApiResponse<ContentReviewDto>
                {
                    Success = false,
                    Message = "Lỗi khi xử lý đánh giá nội dung",
                    Data = null
                });
            }
        }
        #endregion

        #region Exercise Management
        [HttpGet("exercises")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<ExerciseResponseDto>>>> GetExercises(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Lấy danh sách bài tập từ database
                var exercises = await _dbContext.Exercises
                    .Include(e => e.CreatedBy)
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new ExerciseResponseDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Description = e.Description,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        RestTime = e.RestTime,
                        CreatedById = e.CreatedById,
                        CreatedByUsername = e.CreatedBy.DisplayName
                    })
                    .ToListAsync();

                var totalCount = await _dbContext.Exercises.CountAsync();

                var result = new PaginatedResult<ExerciseResponseDto>
                {
                    Items = exercises,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<ExerciseResponseDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách bài tập thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting exercises: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<ExerciseResponseDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách bài tập",
                    Data = null
                });
            }
        }
        #endregion

        #region Challenge Management
        [HttpGet("challenges")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<ChallengeDto>>>> GetChallenges(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Lấy danh sách thách thức từ database
                var challenges = await _dbContext.Challenges
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ChallengeDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        ParticipantCount = _dbContext.UserChallenges.Count(uc => uc.ChallengeId == c.Id)
                    })
                    .ToListAsync();

                var totalCount = await _dbContext.Challenges.CountAsync();

                var result = new PaginatedResult<ChallengeDto>
                {
                    Items = challenges,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<ChallengeDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách thách thức thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting challenges: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<ChallengeDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách thách thức",
                    Data = null
                });
            }
        }
        #endregion

        #region User Activity Analytics
        [HttpGet("user-activity")]
        public async Task<ActionResult<ApiResponse<UserActivityStatisticsDto>>> GetUserActivityStatistics()
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Tạo dữ liệu thống kê hoạt động người dùng mẫu
                var userActivity = new UserActivityStatisticsDto
                {
                    DailyActiveUsers = 256,
                    WeeklyActiveUsers = 892,
                    MonthlyActiveUsers = 1435,
                    LoginsByDate = GenerateActivityByDate(),
                    WorkoutSessionsByDate = GenerateActivityByDate(0.7),
                    MostPopularActivities = new List<ActivityCount>
                    {
                        new ActivityCount { ActivityName = "Tập cardio", Count = 345 },
                        new ActivityCount { ActivityName = "Tập cơ chân", Count = 289 },
                        new ActivityCount { ActivityName = "Tập đẩy tay", Count = 254 },
                        new ActivityCount { ActivityName = "Tập lưng", Count = 231 },
                        new ActivityCount { ActivityName = "Tập bụng", Count = 186 }
                    }
                };

                return Ok(new ApiResponse<UserActivityStatisticsDto>
                {
                    Success = true,
                    Message = "Lấy thống kê hoạt động người dùng thành công",
                    Data = userActivity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user activity statistics: {ex.Message}");
                return StatusCode(500, new ApiResponse<UserActivityStatisticsDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thống kê hoạt động người dùng",
                    Data = null
                });
            }
        }
        #endregion

        #region Helper Methods
        private List<DateCount> GenerateActivityByDate(double multiplier = 1.0)
        {
            var result = new List<DateCount>();
            var today = DateTime.UtcNow.Date;
            var random = new Random();

            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = (int)(random.Next(50, 300) * multiplier);
                
                result.Add(new DateCount
                {
                    Date = date,
                    Count = count
                });
            }

            return result;
        }
        #endregion
    }
} 