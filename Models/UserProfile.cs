namespace FitnessApp.API.Models;

public class UserProfile
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string AvatarUrl { get; set; }
    public DateTime? UpdatedAt { get; set; }
} 