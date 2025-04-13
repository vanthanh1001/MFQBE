using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Models.Auth;
using Models;
using Models.Interfaces;
using FirebaseAdmin.Auth;
using System.Net.Http;
using Models.DTOs;
using FitnessApp.API.Models;
using FitnessApp.API.Models.DTOs;

namespace Controllers;

/*
* HƯỚNG DẪN CÁCH CẤU HÌNH API KEY TRONG FIREBASE CONSOLE
* =====================================================
*
* 1. Vào Google Cloud Console (không phải Firebase Console):
*    - URL: https://console.cloud.google.com/apis/credentials
*    - Đảm bảo bạn đã chọn đúng project của mình
*
* 2. Trong phần "API Keys", tìm và click vào API key được sử dụng bởi Firebase (thường là API key đầu tiên)
*
* 3. Trong trang cài đặt API key:
*    - Trong phần "Application restrictions" chọn "Websites"
*    - Trong phần "Website restrictions", click vào "ADD AN ITEM"
*    - Thêm các trang web sau:
*      * https://mfquest-b89b0.firebaseapp.com/*
*      * http://localhost:5000/*  (cho môi trường phát triển)
*      * https://localhost:5001/* (cho môi trường phát triển)
*
* 4. Nhấn "SAVE" để lưu cài đặt
*
* 5. Nếu ở môi trường phát triển và không muốn cấu hình:
*    - Có thể tạm thời chọn "None" trong phần "Application restrictions"
*    - LƯU Ý: Đây chỉ là giải pháp tạm thời, không nên sử dụng trong môi trường production
*/

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Models.Interfaces.IFirebaseAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext;

    public AuthController(
        Models.Interfaces.IFirebaseAuthService authService, 
        IConfiguration configuration,
        ILogger<AuthController> logger,
        ApplicationDbContext dbContext)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
        
        _httpClient = new HttpClient();
        // Thiết lập Referer header để Firebase chấp nhận request
        try
        {
            var authDomain = _configuration["Firebase:AuthDomain"];
            _httpClient.DefaultRequestHeaders.Referrer = new Uri($"https://{authDomain}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting Referrer: {ex.Message}");
        }

        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<Models.Auth.AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation($"Starting registration for email: {request.Email}");
            
            // Sử dụng Firebase Auth Rest API trực tiếp
            string apiKey = _configuration["Firebase:ApiKey"];
            string signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";
            
            var registerData = new
            {
                email = request.Email,
                password = request.Password,
                displayName = request.DisplayName,
                returnSecureToken = true
            };
            
            var jsonContent = JsonSerializer.Serialize(registerData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _logger.LogInformation($"Sending registration request to: {signUpUrl}");
            
            // Gửi yêu cầu đến Firebase Auth
            var response = await _httpClient.PostAsync(signUpUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Registration response: {response.StatusCode}, Body: {responseBody}");
            
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new ApiResponse<Models.Auth.AuthResponse>
                {
                    Success = false,
                    Message = $"Đăng ký thất bại: {responseBody}",
                    Data = null
                });
            }
            
            // Chuyển tiếp đến service để xử lý
            var authResponse = await _authService.RegisterWithEmailPasswordAsync(
                request.Email, 
                request.Password,
                request.DisplayName
            );

            return Ok(new ApiResponse<Models.Auth.AuthResponse>
            {
                Success = true,
                Message = "Registration successful",
                Data = authResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Registration error: {ex.Message}, StackTrace: {ex.StackTrace}");
            return BadRequest(new ApiResponse<Models.Auth.AuthResponse>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<Models.Auth.AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation($"Starting login for email: {request.Email}");
            
            // Sử dụng Firebase Auth Rest API trực tiếp
            string apiKey = _configuration["Firebase:ApiKey"];
            string signInUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
            
            var loginData = new
            {
                email = request.Email,
                password = request.Password,
                returnSecureToken = true
            };
            
            var jsonContent = JsonSerializer.Serialize(loginData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _logger.LogInformation($"Sending login request to: {signInUrl}");
            
            // Gửi yêu cầu đến Firebase Auth
            var response = await _httpClient.PostAsync(signInUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Login response: {response.StatusCode}, Body: {responseBody}");
            
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new ApiResponse<Models.Auth.AuthResponse>
                {
                    Success = false,
                    Message = $"Đăng nhập thất bại: {responseBody}",
                    Data = null
                });
            }
            
            // Chuyển tiếp đến service để xử lý
            var authResponse = await _authService.LoginWithEmailPasswordAsync(
                request.Email,
                request.Password
            );

            return Ok(new ApiResponse<Models.Auth.AuthResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = authResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login error: {ex.Message}, StackTrace: {ex.StackTrace}");
            return BadRequest(new ApiResponse<Models.Auth.AuthResponse>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    [HttpPost("google")]
    public async Task<ActionResult<ApiResponse<Models.Auth.AuthResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var response = await _authService.LoginWithGoogleAsync(request.IdToken);
            return Ok(new ApiResponse<Models.Auth.AuthResponse>
            {
                Success = true,
                Message = "Google login successful",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<Models.Auth.AuthResponse>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    [HttpPost("sync-firebase-users")]
    public async Task<IActionResult> SyncFirebaseUsers()
    {
        try
        {
            // Sử dụng phương thức trực tiếp từ Firebase Admin
            var firebaseAuth = FirebaseAuth.DefaultInstance;
            var listUsersPage = firebaseAuth.ListUsersAsync(null);
            int createdCount = 0;
            
            // Xử lý người dùng từ Firebase
            var enumerator = listUsersPage.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                var firebaseUser = enumerator.Current;
                
                // Kiểm tra xem user đã tồn tại trong SQL Server chưa
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUser.Uid);
                
                if (existingUser == null)
                {
                    // Tạo user mới trong SQL Server
                    var appUser = new Models.User
                    {
                        FirebaseUid = firebaseUser.Uid,
                        Email = firebaseUser.Email,
                        DisplayName = firebaseUser.DisplayName ?? firebaseUser.Email,
                        CreatedAt = DateTime.UtcNow,
                        Username = firebaseUser.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Mật khẩu tạm thời
                        IsActive = true
                    };
                    
                    await _dbContext.Users.AddAsync(appUser);
                    createdCount++;
                }
            }
            
            await _dbContext.SaveChangesAsync();
            return Ok(new ApiResponse<string> 
            { 
                Success = true, 
                Message = $"Đồng bộ thành công {createdCount} người dùng từ Firebase vào SQL Server" 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string> 
            { 
                Success = false, 
                Message = $"Lỗi khi đồng bộ: {ex.Message}" 
            });
        }
    }
} 