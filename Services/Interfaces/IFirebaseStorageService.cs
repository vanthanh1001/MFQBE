namespace FitnessApp.API.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

public interface IFirebaseStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder);
    Task DeleteFileAsync(string fileUrl);
    Task<string> UploadProfileImageAsync(IFormFile file, string userId);
} 