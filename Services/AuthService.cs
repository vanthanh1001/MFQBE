using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Models;
using Models.DTOs;
using Models.Enums;
using Models.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public AuthService(
        IRepository<User> userRepository, 
        IConfiguration configuration,
        IFirebaseAuthService firebaseAuthService)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _firebaseAuthService = firebaseAuthService;
    }

    public async Task<bool> RegisterAsync(UserRegisterDto userDto)
    {
        try
        {
            var existingUser = (await _userRepository.FindAsync(u => 
                u.Username == userDto.Username || 
                u.Email == userDto.Email
            )).FirstOrDefault();

            if (existingUser != null)
            {
                throw new ValidationException(existingUser.Email == userDto.Email 
                    ? "Email already exists" 
                    : "Username already exists");
            }

            // Register with Firebase first
            var firebaseResponse = await _firebaseAuthService.RegisterWithEmailPasswordAsync(
                userDto.Email,
                userDto.Password,
                $"{userDto.FirstName} {userDto.LastName}"
            );

            // If Firebase registration successful, create local user
            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                PhoneNumber = userDto.PhoneNumber,
                DateOfBirth = userDto.DateOfBirth,
                Role = UserRole.User,
                FirebaseUid = firebaseResponse.User.Uid,
                CreatedExercises = new List<Exercise>(),
                WorkoutPlans = new List<WorkoutPlan>()
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            throw new ValidationException("Registration failed. Please try again.");
        }
    }

    public async Task<string> LoginAsync(UserLoginDto userDto)
    {
        try
        {
            // Login with Firebase first
            var firebaseResponse = await _firebaseAuthService.LoginWithEmailPasswordAsync(
                userDto.Username, // Assuming username is email
                userDto.Password
            );

            // Find user in local database by Firebase UID
            var user = (await _userRepository.FindAsync(u => 
                u.FirebaseUid == firebaseResponse.User.Uid || 
                u.Email == firebaseResponse.User.Email
            )).FirstOrDefault();

            if (user == null)
            {
                // Create user in local database if not exists
                user = new User
                {
                    Username = firebaseResponse.User.Email,
                    Email = firebaseResponse.User.Email,
                    FirebaseUid = firebaseResponse.User.Uid,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                    Role = UserRole.User,
                    CreatedExercises = new List<Exercise>(),
                    WorkoutPlans = new List<WorkoutPlan>()
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            return firebaseResponse.Token; // Return Firebase token
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
throw new ValidationException("Invalid username or password");
        }
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"]
            }, out SecurityToken validatedToken);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}