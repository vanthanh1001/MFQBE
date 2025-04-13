using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Models.Interfaces;
using Models.Auth;
using FitnessApp.API.Models;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthController> _logger;

        public GoogleAuthController(
            IFirebaseAuthService firebaseAuthService,
            IConfiguration configuration,
            ILogger<GoogleAuthController> logger)
        {
            _firebaseAuthService = firebaseAuthService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] GoogleLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Bắt đầu đăng nhập với Google");
                
                // Xác thực với Google và lấy thông tin người dùng
                var userData = await _firebaseAuthService.VerifyGoogleTokenAsync(request.IdToken);
                
                // Gọi API để đăng nhập/đăng ký với Firebase bằng thông tin Google đã được xác thực
                var authResponse = await _firebaseAuthService.LoginWithGoogleAsync(request.IdToken);
                
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Đăng nhập Google thành công",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Google login error: {ex.Message}");
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; }
    }
} 