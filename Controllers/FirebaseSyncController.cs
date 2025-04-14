using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;
using Models.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Models;
using Models.Auth;
using System.Linq;
using Models.DTOs;
using System.Collections.Generic;
using Models.Enums;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirebaseSyncController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly IRepository<User> _userRepository;
        private readonly ILogger<FirebaseSyncController> _logger;

        public FirebaseSyncController(
            ApplicationDbContext dbContext,
            IFirebaseAuthService firebaseAuthService,
            IRepository<User> userRepository,
            ILogger<FirebaseSyncController> logger)
        {
            _dbContext = dbContext;
            _firebaseAuthService = firebaseAuthService;
            _userRepository = userRepository;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("sync-firebase-users")]
        public async Task<IActionResult> SyncFirebaseUsers()
        {
            try
            {
                // Lấy danh sách tất cả người dùng từ Firebase
                var firebaseUsers = await ListAllFirebaseUsersAsync();
                
                // Thống kê
                int synced = 0;
                int skipped = 0;
                int failed = 0;
                var errorMessages = new List<string>();
                
                foreach (var firebaseUser in firebaseUsers)
                {
                    try
                    {
                        // Kiểm tra xem người dùng đã tồn tại trong MySQL chưa
                        var existingUser = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUser.Uid);
                            
                        if (existingUser != null)
                        {
                            // Người dùng đã tồn tại, bỏ qua
                            skipped++;
                            continue;
                        }
                        
                        // Tạo người dùng mới trong MySQL
                        var newUser = new User
                        {
                            FirebaseUid = firebaseUser.Uid,
                            Email = firebaseUser.Email,
                            DisplayName = !string.IsNullOrEmpty(firebaseUser.DisplayName) 
                                ? firebaseUser.DisplayName 
                                : firebaseUser.Email.Split('@')[0],
                            Username = !string.IsNullOrEmpty(firebaseUser.DisplayName) 
                                ? firebaseUser.DisplayName 
                                : firebaseUser.Email.Split('@')[0],
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true,
                            Role = UserRole.User,
                            ProfilePicture = firebaseUser.PhotoUrl,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                        };
                        
                        _dbContext.Users.Add(newUser);
                        await _dbContext.SaveChangesAsync();
                        synced++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        errorMessages.Add($"Lỗi đồng bộ người dùng {firebaseUser.Email}: {ex.Message}");
                    }
                }
                
                return Ok(new
                {
                    Success = true,
                    Message = $"Đồng bộ hoàn tất: {synced} người dùng đã đồng bộ, {skipped} đã bỏ qua, {failed} thất bại",
                    Errors = errorMessages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi khi đồng bộ người dùng từ Firebase",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("sync-user-by-email")]
        public async Task<IActionResult> SyncUserByEmail([FromBody] SyncUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { Success = false, Message = "Email là bắt buộc" });
            }

            try
            {
                // Tìm người dùng trong Firebase bằng email
                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(request.Email);
                }
                catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
                {
                    return NotFound(new { Success = false, Message = $"Không tìm thấy người dùng với email {request.Email} trong Firebase" });
                }

                // Kiểm tra xem người dùng đã tồn tại trong MySQL chưa
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUser.Uid || u.Email == firebaseUser.Email);

                if (existingUser != null)
                {
                    // Cập nhật các thông tin từ Firebase nếu người dùng đã tồn tại
                    existingUser.Username = firebaseUser.DisplayName;
                    existingUser.Email = firebaseUser.Email;
                    existingUser.FirebaseUid = firebaseUser.Uid;
                    existingUser.ProfilePicture = firebaseUser.PhotoUrl;
                    existingUser.LastLoginAt = DateTime.UtcNow;
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    return Ok(new 
                    { 
                        Success = true, 
                        Message = "Người dùng đã tồn tại trong cơ sở dữ liệu", 
                        User = new
                        {
                            existingUser.Id,
                            existingUser.Email,
                            existingUser.Username,
                            existingUser.DisplayName,
                            existingUser.FirebaseUid,
                            existingUser.Role
                        }
                    });
                }

                // Tạo người dùng mới trong MySQL
                var newUser = new User
                {
                    FirebaseUid = firebaseUser.Uid,
                    Email = firebaseUser.Email,
                    DisplayName = !string.IsNullOrEmpty(firebaseUser.DisplayName)
                        ? firebaseUser.DisplayName
                        : firebaseUser.Email.Split('@')[0],
                    Username = !string.IsNullOrEmpty(firebaseUser.DisplayName)
                        ? firebaseUser.DisplayName
                        : firebaseUser.Email.Split('@')[0],
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Role = UserRole.User,
                    ProfilePicture = firebaseUser.PhotoUrl,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                };

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = $"Đã đồng bộ thành công người dùng {request.Email}",
                    User = new
                    {
                        newUser.Id,
                        newUser.Email,
                        newUser.Username,
                        newUser.DisplayName,
                        newUser.FirebaseUid,
                        newUser.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Lỗi khi đồng bộ người dùng {request.Email}",
                    Error = ex.Message
                });
            }
        }

        private async Task<List<UserRecord>> ListAllFirebaseUsersAsync(string? pageToken = null)
        {
            var allUsers = new List<UserRecord>();
            
            try
            {
                // Lấy instance của FirebaseAuth
                var auth = FirebaseAuth.DefaultInstance;
                
                // Sử dụng ListUsersAsync để lấy danh sách người dùng
                var pagedEnumerable = auth.ListUsersAsync(new FirebaseAdmin.Auth.ListUsersOptions { PageToken = pageToken });
                var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();
                
                while (await responses.MoveNextAsync())
                {
                    var currentPage = responses.Current;
                    foreach (var userRecord in currentPage.Users)
                    {
                        allUsers.Add(userRecord);
                    }
                }
                
                return allUsers;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                Console.WriteLine($"Lỗi khi lấy danh sách người dùng Firebase: {ex.Message}");
                return allUsers; // Trả về danh sách rỗng trong trường hợp lỗi
            }
        }

        [HttpPost("user")]
        [Authorize]
        public async Task<IActionResult> SyncUserData([FromBody] UserFirebaseData data)
        {
            try
            {
                // Lấy user ID từ token
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized();
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                // Cập nhật thông tin từ Firebase
                if (!string.IsNullOrEmpty(data.DisplayName))
                {
                    user.DisplayName = data.DisplayName;
                }

                if (!string.IsNullOrEmpty(data.Email) && user.Email != data.Email)
                {
                    // Kiểm tra email đã tồn tại chưa
                    var existingUser = (await _userRepository.FindAsync(u => u.Email == data.Email && u.Id != userId)).FirstOrDefault();
                    if (existingUser != null)
                    {
                        return Conflict($"Email {data.Email} is already in use");
                    }
                    user.Email = data.Email;
                }

                if (!string.IsNullOrEmpty(data.FirebaseUid) && user.FirebaseUid != data.FirebaseUid)
                {
                    user.FirebaseUid = data.FirebaseUid;
                }

                if (!string.IsNullOrEmpty(data.ProfilePicture))
                {
                    user.ProfilePicture = data.ProfilePicture;
                }

                if (!string.IsNullOrEmpty(data.FirstName))
                {
                    user.FirstName = data.FirstName;
                }

                if (!string.IsNullOrEmpty(data.LastName))
                {
                    user.LastName = data.LastName;
                }

                if (!string.IsNullOrEmpty(data.PhoneNumber))
                {
                    user.PhoneNumber = data.PhoneNumber;
                }

                if (data.DateOfBirth.HasValue)
                {
                    user.DateOfBirth = data.DateOfBirth;
                }

                user.IsActive = data.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                // Lưu thay đổi
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                return Ok(new
                {
                    message = "User data synced successfully",
                    user = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        ProfilePicture = user.ProfilePicture,
                        DisplayName = user.DisplayName,
                        CreatedAt = user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user data from Firebase");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("batch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncBatchUserData([FromBody] List<UserFirebaseData> users)
        {
            try
            {
                if (users == null || !users.Any())
                {
                    return BadRequest("No user data provided");
                }

                var syncResults = new List<object>();
                foreach (var userData in users)
                {
                    if (string.IsNullOrEmpty(userData.FirebaseUid))
                    {
                        syncResults.Add(new { status = "Error", message = "Firebase UID is required", data = userData });
                        continue;
                    }

                    // Tìm user theo Firebase UID
                    var user = (await _userRepository.FindAsync(u => u.FirebaseUid == userData.FirebaseUid)).FirstOrDefault();
                    
                    if (user == null)
                    {
                        // Tạo user mới
                        user = new User
                        {
                            FirebaseUid = userData.FirebaseUid,
                            Email = userData.Email,
                            DisplayName = userData.DisplayName ?? userData.Email?.Split('@')[0],
                            Username = userData.Username ?? userData.Email?.Split('@')[0],
                            FirstName = userData.FirstName,
                            LastName = userData.LastName,
                            PhoneNumber = userData.PhoneNumber,
                            ProfilePicture = userData.ProfilePicture,
                            DateOfBirth = userData.DateOfBirth,
                            IsActive = userData.IsActive,
                            CreatedAt = DateTime.UtcNow,
                            Role = Models.Enums.UserRole.User,
                            // Tạo password hash ban đầu
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                        };

                        await _userRepository.AddAsync(user);
                        syncResults.Add(new { status = "Created", userId = user.Id, firebaseUid = user.FirebaseUid });
                    }
                    else
                    {
                        // Cập nhật thông tin
                        if (!string.IsNullOrEmpty(userData.DisplayName))
                        {
                            user.DisplayName = userData.DisplayName;
                        }

                        if (!string.IsNullOrEmpty(userData.Email))
                        {
                            user.Email = userData.Email;
                        }

                        if (!string.IsNullOrEmpty(userData.Username))
                        {
                            user.Username = userData.Username;
                        }

                        if (!string.IsNullOrEmpty(userData.ProfilePicture))
                        {
                            user.ProfilePicture = userData.ProfilePicture;
                        }

                        if (!string.IsNullOrEmpty(userData.FirstName))
                        {
                            user.FirstName = userData.FirstName;
                        }

                        if (!string.IsNullOrEmpty(userData.LastName))
                        {
                            user.LastName = userData.LastName;
                        }

                        if (!string.IsNullOrEmpty(userData.PhoneNumber))
                        {
                            user.PhoneNumber = userData.PhoneNumber;
                        }

                        if (userData.DateOfBirth.HasValue)
                        {
                            user.DateOfBirth = userData.DateOfBirth;
                        }

                        user.IsActive = userData.IsActive;
                        user.UpdatedAt = DateTime.UtcNow;

                        await _userRepository.UpdateAsync(user);
                        syncResults.Add(new { status = "Updated", userId = user.Id, firebaseUid = user.FirebaseUid });
                    }
                }

                await _userRepository.SaveChangesAsync();
                return Ok(new { message = $"Synced {syncResults.Count} users", results = syncResults });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing batch user data from Firebase");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("check/{firebaseUid}")]
        public async Task<IActionResult> CheckUserExists(string firebaseUid)
        {
            try
            {
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return BadRequest("Firebase UID is required");
                }

                var user = (await _userRepository.FindAsync(u => u.FirebaseUid == firebaseUid)).FirstOrDefault();
                
                if (user == null)
                {
                    return NotFound(new { exists = false });
                }

                return Ok(new
                {
                    exists = true,
                    userId = user.Id,
                    username = user.Username,
                    email = user.Email,
                    displayName = user.DisplayName,
                    role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence");
                return StatusCode(500, "Internal server error");
            }
        }

        public class SyncUserRequest
        {
            public string Email { get; set; }
        }

        public class UserFirebaseData
        {
            public string FirebaseUid { get; set; }
            public string Email { get; set; }
            public string DisplayName { get; set; }
            public string Username { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string PhoneNumber { get; set; }
            public string ProfilePicture { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }
} 