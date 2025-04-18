using FitnessApp.API.Services.Interfaces;
using Firebase.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace FitnessApp.API.Services.Implementations
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly string _bucketName;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public FirebaseStorageService(IConfiguration configuration)
        {
            _bucketName = configuration["Firebase:StorageBucket"];
            _apiKey = configuration["Firebase:ApiKey"];
            
            Console.WriteLine($"Initializing Firebase Storage with bucket: {_bucketName}");
            
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Longer timeout for file uploads
            };
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder)
        {
            try
            {
                Console.WriteLine($"Uploading file {fileName} to folder {folder}");
                
                // Create a unique file name to avoid conflicts
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = $"{folder}/{uniqueFileName}";

                // Create Firebase Storage instance with proper authentication
                var storage = new FirebaseStorage(
                    _bucketName,
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(_apiKey)
                    });
                
                // Upload the file with progress reporting
                var task = storage
                    .Child(filePath)
                    .PutAsync(fileStream, CancellationToken.None);
                
                // Wait for the upload to complete and get the download URL
                var downloadUrl = await task;
                Console.WriteLine($"File uploaded successfully. Download URL: {downloadUrl}");
                
                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Additional debugging for Firebase Storage errors
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                throw new Exception($"Error uploading file: {ex.Message}", ex);
            }
        }

        // Fixed return type to match interface (Task instead of Task<bool>)
        public async Task DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return;
                }
                
                Console.WriteLine($"Deleting file from URL: {fileUrl}");
                
                // Extract the file path from the URL
                Uri uri = new Uri(fileUrl);
                string filePath = uri.LocalPath;
                
                // Remove the leading /o/ if present
                if (filePath.StartsWith("/o/"))
                {
                    filePath = filePath.Substring(3);
                }
                
                // URL decode the path
                filePath = Uri.UnescapeDataString(filePath);

                // Create Firebase Storage instance
                var storage = new FirebaseStorage(
                    _bucketName,
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(_apiKey)
                    });
                
                // Delete the file
                await storage.Child(filePath).DeleteAsync();
                Console.WriteLine($"File deleted successfully: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                // Don't throw exception for delete failures
            }
        }

        // Added missing method from interface
        public async Task<string> UploadProfileImageAsync(IFormFile file, string userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("No file was provided");
                }

                Console.WriteLine($"Uploading profile image for user {userId}");
                
                using var stream = file.OpenReadStream();
                
                // Upload to a profile images folder with user ID
                string folder = $"profile-images/{userId}";
                
                // Use the existing upload method
                return await UploadFileAsync(stream, file.FileName, folder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading profile image: {ex.Message}");
                throw new Exception($"Error uploading profile image: {ex.Message}", ex);
            }
        }
    }
}