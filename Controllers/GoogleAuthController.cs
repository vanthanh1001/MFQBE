using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Models.Interfaces;
using Models.Auth;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly IConfiguration _configuration;

        public GoogleAuthController(
            IFirebaseAuthService firebaseAuthService,
            IConfiguration configuration)
        {
            _firebaseAuthService = firebaseAuthService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] GoogleLoginRequest request)
        {
            try
            {
                // Xác thực với Firebase
                var userRecord = await _firebaseAuthService.VerifyGoogleTokenAsync(request.IdToken);
                var firebaseToken = await _firebaseAuthService.CreateCustomTokenAsync(userRecord.Uid);
                
                // Tạo JWT token cho backend
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userRecord.Uid),
                    new Claim(ClaimTypes.Email, userRecord.Email),
                    new Claim(ClaimTypes.Name, userRecord.DisplayName),
                    new Claim("PhotoUrl", userRecord.PhotoUrl ?? ""),
                    new Claim("Provider", "Google")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var jwtToken = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    firebaseToken = firebaseToken,
                    user = new
                    {
                        id = userRecord.Uid,
                        email = userRecord.Email,
                        displayName = userRecord.DisplayName,
                        photoUrl = userRecord.PhotoUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; }
    }
} 