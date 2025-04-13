namespace FitnessApp.API.Services.Implementations;

using FitnessApp.API.Services.Interfaces;
using Firebase.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;

public class FirebaseStorageService : IFirebaseStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly FirebaseStorage _storage;

    public FirebaseStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["Firebase:StorageBucket"];
        _storageClient = StorageClient.Create();
        _storage = new FirebaseStorage(
            configuration["Firebase:StorageBucket"],
            new FirebaseStorageOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(configuration["Firebase:ApiKey"])
            });
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder)
    {
        try
        {
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = $"{folder}/{uniqueFileName}";

            await _storage
                .Child(filePath)
                .PutAsync(fileStream);

            return await _storage.Child(filePath).GetDownloadUrlAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error uploading file: {ex.Message}");
        }
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            await _storage.Child(fileName).DeleteAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting file: {ex.Message}");
        }
    }

    public async Task<string> UploadProfileImageAsync(IFormFile file, string userId)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was provided");

        using var stream = file.OpenReadStream();
        return await UploadFileAsync(stream, file.FileName, $"profile-images/{userId}");
    }
} 