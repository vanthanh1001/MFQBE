namespace FitnessApp.API.Models.DTOs;
using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }
}

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    [MinLength(3)]
    public string DisplayName { get; set; }
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
} 