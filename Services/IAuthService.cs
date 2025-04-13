public interface IAuthService
{
    Task<bool> RegisterAsync(UserRegisterDto userDto);
    Task<string> LoginAsync(UserLoginDto userDto);
    Task<bool> ValidateTokenAsync(string token);
} 