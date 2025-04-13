using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Models.Auth;

namespace Models.Interfaces
{
    public interface IFirebaseAuthService
    {
        Task<AuthResponse> LoginWithEmailPasswordAsync(string email, string password);
        Task<AuthResponse> LoginWithGoogleAsync(string idToken);
        Task<AuthResponse> RegisterWithEmailPasswordAsync(string email, string password, string displayName);
        Task<UserData> VerifyGoogleTokenAsync(string idToken);
        Task<string> CreateCustomTokenAsync(string uid);
    }
} 