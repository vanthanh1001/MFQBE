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
using System.ComponentModel.DataAnnotations;
using FitnessApp.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext dbContext,
            ILogger<AdminController> logger)
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

        #region User Management API
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<AdminUserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] UserRole? roleFilter = null,
            [FromQuery] bool? isActiveFilter = null)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Xây dựng query với các bộ lọc
                var query = _dbContext.Users.AsQueryable();

                // Áp dụng các bộ lọc
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(u => 
                        u.Username.ToLower().Contains(searchTerm) || 
                        u.Email.ToLower().Contains(searchTerm) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchTerm))
                    );
                }

                if (roleFilter.HasValue)
                {
                    query = query.Where(u => u.Role == roleFilter.Value);
                }

                if (isActiveFilter.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActiveFilter.Value);
                }

                // Đếm tổng số bản ghi phù hợp với điều kiện lọc
                var totalCount = await query.CountAsync();

                // Phân trang
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AdminUserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        DisplayName = u.DisplayName,
                        Role = u.Role,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        PhoneNumber = u.PhoneNumber,
                        ProfilePicture = u.ProfilePicture,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .ToListAsync();

                var result = new PaginatedResult<AdminUserDto>
                {
                    Items = users,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new ApiResponse<PaginatedResult<AdminUserDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách người dùng thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResult<AdminUserDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách người dùng",
                    Data = null
                });
            }
        }

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUserDetail(int userId)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDetailDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    });
                }

                // Tạo DTO cơ bản từ thông tin người dùng
                var userDetail = new UserDetailDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    UpdatedAt = user.UpdatedAt,
                    RecentActivities = new List<UserActivityDto>(),
                    Statistics = new UserStatisticsDto(),
                    Subscriptions = new List<SubscriptionDto>(),
                    RecentPayments = new List<PaymentDto>()
                };

                // Lấy thống kê của người dùng (workout, challenges, v.v.)
                var workoutCount = await _dbContext.WorkoutSessions
                    .CountAsync(ws => ws.UserId == userId);
                
                var challengeCount = await _dbContext.UserChallenges
                    .CountAsync(uc => uc.UserId == userId);

                userDetail.Statistics = new UserStatisticsDto
                {
                    TotalWorkouts = workoutCount,
                    CompletedChallenges = challengeCount,
                    CurrentStreak = 0, // Cần tính toán thêm
                    TotalPoints = await _dbContext.UserChallenges
                        .Where(uc => uc.UserId == userId)
                        .SumAsync(uc => uc.Points),
                    CurrentLevel = "Beginner" // Giả định, cần logic chi tiết hơn
                };

                // Trong môi trường thực tế, bạn sẽ lấy các hoạt động gần đây, đăng ký và thanh toán từ database
                // Ở đây, chúng ta tạo dữ liệu giả định cho demo

                return Ok(new ApiResponse<UserDetailDto>
                {
                    Success = true,
                    Message = "Lấy thông tin chi tiết người dùng thành công",
                    Data = userDetail
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user detail: {ex.Message}");
                return StatusCode(500, new ApiResponse<UserDetailDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin chi tiết người dùng",
                    Data = null
                });
            }
        }

        [HttpPost("admins")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> CreateAdmin([FromBody] CreateAdminDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                // Kiểm tra email đã tồn tại chưa
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<AdminUserDto>
                    {
                        Success = false,
                        Message = "Email đã được sử dụng",
                        Data = null
                    });
                }

                // Tạo mã thông báo ngẫu nhiên cho FirebaseUid 
                // (Trong thực tế, bạn sẽ đăng ký người dùng thông qua Firebase Auth)
                var tempFirebaseUid = Guid.NewGuid().ToString();

                // Tạo hash password (trong thực tế sẽ sử dụng Firebase hoặc hệ thống xác thực riêng)
                string passwordHash = HashPassword(request.Password);

                // Tạo người dùng mới với vai trò Admin
                var newAdmin = new User
                {
                    Email = request.Email,
                    DisplayName = request.DisplayName,
                    Username = request.Email, // Sử dụng email làm username mặc định
                    PasswordHash = passwordHash,
                    Role = UserRole.Admin,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    FirebaseUid = tempFirebaseUid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _dbContext.Users.Add(newAdmin);
                await _dbContext.SaveChangesAsync();

                var adminDto = new AdminUserDto
                {
                    Id = newAdmin.Id,
                    Username = newAdmin.Username,
                    Email = newAdmin.Email,
                    DisplayName = newAdmin.DisplayName,
                    Role = newAdmin.Role,
                    FirstName = newAdmin.FirstName,
                    LastName = newAdmin.LastName,
                    PhoneNumber = newAdmin.PhoneNumber,
                    ProfilePicture = newAdmin.ProfilePicture,
                    IsActive = newAdmin.IsActive,
                    CreatedAt = newAdmin.CreatedAt,
                    LastLoginAt = null,
                    UpdatedAt = newAdmin.UpdatedAt
                };

                return CreatedAtAction(nameof(GetUserDetail), new { userId = newAdmin.Id }, new ApiResponse<AdminUserDto>
                {
                    Success = true,
                    Message = "Tạo tài khoản admin mới thành công",
                    Data = adminDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating admin: {ex.Message}");
                return StatusCode(500, new ApiResponse<AdminUserDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo tài khoản admin",
                    Data = null
                });
            }
        }

        [HttpPut("users/{userId}/password")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> ResetUserPassword(
            int userId, 
            [FromBody] ResetUserPasswordDto request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<AdminUserDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    });
                }

                // Trong thực tế, bạn sẽ reset password thông qua Firebase Auth API
                // Ở đây, chúng ta chỉ mô phỏng bằng cách cập nhật hash password
                user.PasswordHash = HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                var userDto = new AdminUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(new ApiResponse<AdminUserDto>
                {
                    Success = true,
                    Message = "Đặt lại mật khẩu người dùng thành công",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resetting user password: {ex.Message}");
                return StatusCode(500, new ApiResponse<AdminUserDto>
                {
                    Success = false,
                    Message = "Lỗi khi đặt lại mật khẩu người dùng",
                    Data = null
                });
            }
        }

        [HttpPut("users/{userId}/role")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> ChangeUserRole(
            int userId, 
            [FromBody] ChangeRoleRequest request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<AdminUserDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    });
                }

                // Cập nhật vai trò người dùng
                user.Role = request.NewRole;
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                var userDto = new AdminUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(new ApiResponse<AdminUserDto>
                {
                    Success = true,
                    Message = "Cập nhật vai trò người dùng thành công",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing user role: {ex.Message}");
                return StatusCode(500, new ApiResponse<AdminUserDto>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật vai trò người dùng",
                    Data = null
                });
            }
        }

        [HttpPut("users/{userId}/status")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> ChangeUserStatus(
            int userId, 
            [FromBody] ChangeStatusRequest request)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<AdminUserDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    });
                }

                // Cập nhật trạng thái người dùng
                user.IsActive = request.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                var userDto = new AdminUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(new ApiResponse<AdminUserDto>
                {
                    Success = true,
                    Message = $"Đã {(request.IsActive ? "kích hoạt" : "vô hiệu hóa")} tài khoản người dùng",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing user status: {ex.Message}");
                return StatusCode(500, new ApiResponse<AdminUserDto>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật trạng thái người dùng",
                    Data = null
                });
            }
        }

        [HttpDelete("users/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int userId)
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng",
                        Data = false
                    });
                }

                // Thực hiện soft delete - chỉ đánh dấu tài khoản là không hoạt động
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Đã xóa người dùng {user.Username} thành công",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting user: {ex.Message}");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Lỗi khi xóa người dùng",
                    Data = false
                });
            }
        }
        #endregion

        #region Statistics API
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<AdminStatisticsDto>>> GetStatistics()
        {
            if (!await IsCurrentUserAdmin())
            {
                return Forbid();
            }

            try
            {
                var statistics = new AdminStatisticsDto
                {
                    TotalUsers = await _dbContext.Users.CountAsync(),
                    ActiveUsers = await _dbContext.Users.CountAsync(u => u.IsActive),
                    TotalTrainers = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Trainer),
                    TotalAdmins = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Admin),
                    TotalExercises = await _dbContext.Exercises.CountAsync(),
                    TotalWorkoutPlans = await _dbContext.WorkoutPlans.CountAsync(),
                    TotalWorkoutSessions = await _dbContext.WorkoutSessions.CountAsync(),
                    TotalChallenges = await _dbContext.Challenges.CountAsync(),
                    NewUsersLast30Days = await _dbContext.Users
                        .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
                    UserRegistrationsByDate = await _dbContext.Users
                        .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                        .GroupBy(u => u.CreatedAt.Date)
                        .Select(g => new DateCount
                        {
                            Date = g.Key,
                            Count = g.Count()
                        })
                        .ToListAsync()
                };

                return Ok(new ApiResponse<AdminStatisticsDto>
                {
                    Success = true,
                    Message = "Lấy thống kê thành công",
                    Data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting statistics: {ex.Message}");
                return StatusCode(500, new ApiResponse<AdminStatisticsDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thống kê",
                    Data = null
                });
            }
        }
        #endregion

        #region Helper Methods
        private string HashPassword(string password)
        {
            // Đây chỉ là một phương thức hash password đơn giản, trong thực tế sẽ sử dụng phương thức bảo mật hơn
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion
    }
}
