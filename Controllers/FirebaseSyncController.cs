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

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirebaseSyncController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IFirebaseAuthService _firebaseAuthService;

        public FirebaseSyncController(
            ApplicationDbContext dbContext,
            IFirebaseAuthService firebaseAuthService)
        {
            _dbContext = dbContext;
            _firebaseAuthService = firebaseAuthService;
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

        private async Task<List<UserRecord>> ListAllFirebaseUsersAsync(string pageToken = null)
        {
            var allUsers = new List<UserRecord>();
            
            try
            {
                // Lấy instance của FirebaseAuth
                var auth = FirebaseAuth.DefaultInstance;
                
                // Sử dụng ListUsersAsync để lấy danh sách người dùng
                var pagedEnumerable = auth.ListUsersAsync(null);
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
    }

    public class SyncUserRequest
    {
        public string Email { get; set; }
    }
} 