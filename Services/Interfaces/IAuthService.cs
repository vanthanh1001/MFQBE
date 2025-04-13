using System.Threading.Tasks;
using FirebaseAdmin.Auth;

namespace Services.Interfaces
{
    public interface IFirebaseAuthService
    {
        Task<AuthResponse> LoginWithEmailPasswordAsync(string email, string password);
        Task<AuthResponse> LoginWithGoogleAsync(string idToken);
        Task<AuthResponse> RegisterWithEmailPasswordAsync(string email, string password, string displayName);
        Task<UserRecord> VerifyGoogleTokenAsync(string idToken);
        Task<string> CreateCustomTokenAsync(string uid);
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public UserData User { get; set; }
    }

    public class UserData
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PhotoUrl { get; set; }
        public string Provider { get; set; }
    }
} 