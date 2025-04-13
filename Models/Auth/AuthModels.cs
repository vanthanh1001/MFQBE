namespace Models.Auth
{
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