using FitnessApp.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FitnessApp.API.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                var firebaseUid = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return null;
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
                
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current user: {ex.Message}");
                return null;
            }
        }
    }
}