using Models.Interfaces;
using Models.Auth;
using Models;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Firebase.Auth;
using Firebase.Auth.Providers;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using FitnessApp.API.Exceptions;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace Services.Implementations
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly FirebaseAuth _adminAuth;
        private readonly FirebaseAuthClient _authClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        // Thời gian timeout mặc định
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(90);
        private readonly HttpClient _httpClient;
        
        // REST API endpoint URL cho Firebase Auth
        private readonly string _signInWithEmailUrl;
        private readonly string _signUpWithEmailUrl;

        public FirebaseAuthService(
            IConfiguration configuration,
            ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            
            try 
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    throw new InvalidOperationException("Firebase has not been initialized");
                }

                _adminAuth = FirebaseAuth.DefaultInstance;
                
                // Tạo HttpClient với timeout dài hơn
                _httpClient = new HttpClient
                {
                    Timeout = _defaultTimeout
                };
                
                // Cấu hình URL cho REST API
                string apiKey = configuration["Firebase:ApiKey"];
                _signInWithEmailUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
                _signUpWithEmailUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";
                
                // Cấu hình Firebase Auth Client (giữ lại để tương thích với phương thức khác)
                try {
                    var config = new FirebaseAuthConfig
                    {
                        ApiKey = apiKey,
                        AuthDomain = configuration["Firebase:AuthDomain"],
                        Providers = new FirebaseAuthProvider[] 
                        { 
                            new EmailProvider()
                        },
                        HttpClient = _httpClient
                    };
                    
                    Console.WriteLine("Initializing Firebase Auth with config:");
                    Console.WriteLine($"ApiKey: {apiKey}");
                    Console.WriteLine($"AuthDomain: {configuration["Firebase:AuthDomain"]}");
                    Console.WriteLine($"StorageBucket: {configuration["Firebase:StorageBucket"]}");
                    Console.WriteLine($"Timeout: {_defaultTimeout.TotalSeconds} seconds");
                    
                    _authClient = new FirebaseAuthClient(config);
                } catch (Exception ex) {
                    Console.WriteLine($"Error initializing Firebase Auth Client: {ex.Message}");
                    // Tiếp tục vì chúng ta sẽ sử dụng REST API
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Firebase Auth: {ex.Message}");
                throw;
            }
        }

        public async Task<AuthResponse> LoginWithEmailPasswordAsync(string email, string password)
        {
            try
            {
                Console.WriteLine($"Attempting login for email: {email}");
                
                // Thêm Referer vào header
                if (_httpClient.DefaultRequestHeaders.Contains("Referer"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Referer");
                }
                _httpClient.DefaultRequestHeaders.Add("Referer", "https://mfquest-b89b0.firebaseapp.com");
                
                // Sử dụng REST API trực tiếp thay vì FirebaseAuthClient
                var loginContent = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };
                
                // Chuyển đổi thành JSON
                var jsonContent = JsonSerializer.Serialize(loginContent);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Gửi request đến Firebase Auth REST API
                Console.WriteLine($"Sending request to Firebase Auth REST API: {_signInWithEmailUrl}");
                HttpResponseMessage response = null;
                
                // Thêm xử lý thử lại
                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        response = await _httpClient.PostAsync(_signInWithEmailUrl, httpContent);
                        break; // Thoát khỏi vòng lặp khi thành công
                    }
                    catch (HttpRequestException ex) when (retry < 2)
                    {
                        Console.WriteLine($"HTTP error on attempt {retry+1}: {ex.Message}");
                        await Task.Delay(500); // Đợi 0.5 giây trước khi thử lại
                    }
                }
                
                if (response == null)
                {
                    throw new FitnessApp.API.Exceptions.ValidationException("Đăng nhập thất bại, vui lòng thử lại sau");
                }
                
                // Xử lý response
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Firebase Auth response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var error = errorResponse.GetProperty("error");
                    var errorMessage = error.GetProperty("message").GetString();
                    Console.WriteLine($"Firebase Auth error: {errorMessage}");
                    
                    // Xử lý các mã lỗi Firebase Auth
                    switch (errorMessage)
                    {
                        case "EMAIL_NOT_FOUND":
                            throw new FitnessApp.API.Exceptions.ValidationException("Email không tồn tại");
                        case "INVALID_PASSWORD":
                            throw new FitnessApp.API.Exceptions.ValidationException("Mật khẩu không đúng");
                        case "USER_DISABLED":
                            throw new FitnessApp.API.Exceptions.ValidationException("Tài khoản bị vô hiệu hóa");
                        default:
                            throw new FitnessApp.API.Exceptions.ValidationException($"Đăng nhập thất bại: {errorMessage}");
                    }
                }
                
                // Phân tích response
                var authResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var localId = authResponse.GetProperty("localId").GetString(); // Firebase UID
                var idToken = authResponse.GetProperty("idToken").GetString();
                
                Console.WriteLine($"Firebase auth successful for UID: {localId}");
                
                // Lấy thông tin user từ Firebase Auth
                var userRecord = await _adminAuth.GetUserAsync(localId);
                Console.WriteLine($"Got user record: {userRecord.Email}");

                // Kiểm tra user trong MySQL
                var dbUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUid == userRecord.Uid);
                
                if (dbUser == null)
                {
                    throw new NotFoundException($"User not found in database");
                }

                return new AuthResponse
                {
                    Token = idToken,
                    User = MapToUserData(userRecord, "password")
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error during login: {ex.Message}");
                throw new FitnessApp.API.Exceptions.ValidationException("Lỗi kết nối đến máy chủ xác thực. Vui lòng kiểm tra kết nối mạng của bạn.");
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Request timed out: {ex.Message}");
                throw new FitnessApp.API.Exceptions.ValidationException("Yêu cầu xác thực hết thời gian. Vui lòng thử lại sau.");
            }
            catch (FitnessApp.API.Exceptions.ValidationException)
            {
                // Truyền qua các lỗi validation
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new FitnessApp.API.Exceptions.ValidationException("Đăng nhập thất bại: " + ex.Message);
            }
        }

        public async Task<AuthResponse> LoginWithGoogleAsync(string idToken)
        {
            try
            {
                // Validate idToken trước
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                
                // Lấy hoặc tạo user trong Firebase
                UserRecord userRecord;
                try
                {
                    userRecord = await _adminAuth.GetUserByEmailAsync(payload.Email);
                }
                catch (FirebaseAdmin.Auth.FirebaseAuthException)
                {
                    // Tạo user mới nếu không tìm thấy
                    var userArgs = new UserRecordArgs
                    {
                        Email = payload.Email,
                        DisplayName = payload.Name,
                        PhotoUrl = payload.Picture,
                        EmailVerified = payload.EmailVerified
                    };
                    userRecord = await _adminAuth.CreateUserAsync(userArgs);
                }
                
                // Tạo custom token từ Firebase UID
                string customToken = await CreateCustomTokenAsync(userRecord.Uid);
                
                // Đổi custom token lấy ID token
                var request = new
                {
                    token = customToken,
                    returnSecureToken = true
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                string apiKey = _configuration["Firebase:ApiKey"];
                string exchangeUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key={apiKey}";
                
                var response = await _httpClient.PostAsync(exchangeUrl, httpContent);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error exchanging custom token: {responseBody}");
                }
                
                // Lấy ID token từ response
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var firebaseIdToken = tokenResponse.GetProperty("idToken").GetString();
                
                // Kiểm tra user trong MySQL
                var dbUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUid == userRecord.Uid);
                
                if (dbUser == null)
                {
                    // Tạo user trong MySQL nếu chưa có
                    var appUser = new Models.User
                    {
                        FirebaseUid = userRecord.Uid,
                        Email = payload.Email,
                        DisplayName = payload.Name,
                        CreatedAt = DateTime.UtcNow,
                        Username = payload.Email
                    };
                    
                    await _dbContext.Users.AddAsync(appUser);
                    await _dbContext.SaveChangesAsync();
                }
                
                return new AuthResponse
                {
                    Token = firebaseIdToken,
                    User = MapToUserData(userRecord, "google")
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Google login failed", ex);
            }
        }

        public async Task<AuthResponse> RegisterWithEmailPasswordAsync(string email, string password, string displayName)
        {
            try
            {
                Console.WriteLine($"Attempting registration for email: {email}");
                
                // Thêm Referer vào header
                if (_httpClient.DefaultRequestHeaders.Contains("Referer"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Referer");
                }
                _httpClient.DefaultRequestHeaders.Add("Referer", "https://mfquest-b89b0.firebaseapp.com");
                
                // Sử dụng REST API trực tiếp thay vì FirebaseAuthClient
                var registerContent = new
                {
                    email,
                    password,
                    displayName,
                    returnSecureToken = true
                };
                
                // Chuyển đổi thành JSON
                var jsonContent = JsonSerializer.Serialize(registerContent);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Gửi request đến Firebase Auth REST API
                Console.WriteLine($"Sending register request to Firebase Auth REST API: {_signUpWithEmailUrl}");
                HttpResponseMessage response = null;
                
                // Thêm xử lý thử lại
                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        response = await _httpClient.PostAsync(_signUpWithEmailUrl, httpContent);
                        break; // Thoát khỏi vòng lặp khi thành công
                    }
                    catch (HttpRequestException ex) when (retry < 2)
                    {
                        Console.WriteLine($"HTTP error on attempt {retry+1}: {ex.Message}");
                        await Task.Delay(500); // Đợi 0.5 giây trước khi thử lại
                    }
                }
                
                if (response == null)
                {
                    throw new FitnessApp.API.Exceptions.ValidationException("Đăng ký thất bại, vui lòng thử lại sau");
                }
                
                // Xử lý response
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Firebase Auth response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var error = errorResponse.GetProperty("error");
                    var errorMessage = error.GetProperty("message").GetString();
                    Console.WriteLine($"Firebase Auth error: {errorMessage}");
                    
                    // Xử lý các mã lỗi Firebase Auth
                    switch (errorMessage)
                    {
                        case "EMAIL_EXISTS":
                            throw new FitnessApp.API.Exceptions.ValidationException("Email đã được sử dụng. Vui lòng chọn email khác.");
                        case "OPERATION_NOT_ALLOWED":
                            throw new FitnessApp.API.Exceptions.ValidationException("Đăng ký bằng email và mật khẩu hiện đang bị tắt.");
                        case "TOO_MANY_ATTEMPTS_TRY_LATER":
                            throw new FitnessApp.API.Exceptions.ValidationException("Quá nhiều lần thử không thành công. Vui lòng thử lại sau.");
                        default:
                            throw new FitnessApp.API.Exceptions.ValidationException($"Đăng ký thất bại: {errorMessage}");
                    }
                }
                
                // Phân tích response
                var authResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var localId = authResponse.GetProperty("localId").GetString(); // Firebase UID
                var idToken = authResponse.GetProperty("idToken").GetString();
                
                Console.WriteLine($"Firebase auth registration successful for UID: {localId}");
                
                // Lấy thông tin user từ Firebase Auth
                var userRecord = await _adminAuth.GetUserAsync(localId);

                // Tạo user trong MySQL
                var appUser = new Models.User
                {
                    FirebaseUid = userRecord.Uid,
                    Email = email,
                    DisplayName = displayName,
                    CreatedAt = DateTime.UtcNow,
                    Username = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                };
                
                await _dbContext.Users.AddAsync(appUser);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Created MySQL user with ID: {appUser.Id}");

                return new AuthResponse
                {
                    Token = idToken,
                    User = MapToUserData(userRecord, "password")
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error during registration: {ex.Message}");
                throw new FitnessApp.API.Exceptions.ValidationException("Lỗi kết nối đến máy chủ xác thực. Vui lòng kiểm tra kết nối mạng của bạn.");
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Request timed out: {ex.Message}");
                throw new FitnessApp.API.Exceptions.ValidationException("Yêu cầu đăng ký hết thời gian. Vui lòng thử lại sau.");
            }
            catch (FitnessApp.API.Exceptions.ValidationException)
            {
                // Truyền qua các lỗi validation
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                throw new FitnessApp.API.Exceptions.ValidationException("Đăng ký thất bại: " + ex.Message);
            }
        }

        public async Task<UserRecord> VerifyGoogleTokenAsync(string idToken)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            try
            {
                return await _adminAuth.GetUserByEmailAsync(payload.Email);
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException)
            {
                var userArgs = new UserRecordArgs
                {
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    PhotoUrl = payload.Picture,
                    EmailVerified = payload.EmailVerified
                };
                return await _adminAuth.CreateUserAsync(userArgs);
            }
        }

        public async Task<string> CreateCustomTokenAsync(string uid)
        {
            return await _adminAuth.CreateCustomTokenAsync(uid);
        }

        public async Task<List<UserData>> GetAllUsersAsync()
        {
            try
            {
                // Lấy danh sách người dùng từ Firebase Admin SDK
                var users = new List<UserData>();
                var pagedEnumerable = _adminAuth.ListUsersAsync(null);
                var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();
                
                while (await responses.MoveNextAsync())
                {
                    var currentPage = responses.Current;
                    foreach (var userRecord in currentPage.Users)
                    {
                        users.Add(MapToUserData(userRecord, "firebase"));
                    }
                }
                
                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all users: {ex.Message}");
                throw new Exception("Failed to get users from Firebase", ex);
            }
        }

        private UserData MapToUserData(UserRecord user, string provider)
        {
            return new UserData
            {
                Uid = user.Uid,
                Email = user.Email,
                DisplayName = user.DisplayName,
                PhotoUrl = user.PhotoUrl,
                Provider = provider
            };
        }
    }
} 