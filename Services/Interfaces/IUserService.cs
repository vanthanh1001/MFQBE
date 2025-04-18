using Microsoft.AspNetCore.Http;
using Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FitnessApp.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);
    }
}