using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models.DTOs;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterDto userDto)
    {
        var result = await _authService.RegisterAsync(userDto);
        if (!result)
            return BadRequest("Tên người dùng đã tồn tại");

        return Ok("Đăng ký thành công");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto userDto)
    {
        var token = await _authService.LoginAsync(userDto);
        if (token == null)
            return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng");

        return Ok(new 
        { 
            access_token = token,
            token_type = "Bearer",
            message = "Vui lòng copy token và click vào nút Authorize (🔓) ở trên, sau đó nhập: Bearer [space] token"
        });
    }

    [HttpPost("social-login")]
    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto request)
    {
        return BadRequest("Social login not implemented yet");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        return BadRequest("Forgot password not implemented yet");
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPDto request)
    {
        return BadRequest("OTP verification not implemented yet");
    }
} 