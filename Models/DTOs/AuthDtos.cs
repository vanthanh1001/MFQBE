using System.ComponentModel.DataAnnotations;

namespace Models.DTOs
{
    public class SocialLoginDto
    {
        [Required]
        public string Provider { get; set; } // "Facebook", "Google", "Apple"
        [Required]
        public string AccessToken { get; set; }
    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class VerifyOTPDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string OTPCode { get; set; }
    }
} 