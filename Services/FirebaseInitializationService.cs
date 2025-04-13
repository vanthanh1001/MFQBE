using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

public class FirebaseInitializationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public FirebaseInitializationService(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void Initialize()
    {
        try 
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "credentials", "firebase-adminsdk.json");
                
                Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"Looking for credentials file at: {pathToKey}");
                
                if (!File.Exists(pathToKey))
                {
                    throw new FileNotFoundException($"Firebase credentials file not found at {pathToKey}");
                }

                Console.WriteLine("Found credentials file, initializing Firebase...");
                var credential = GoogleCredential.FromFile(pathToKey);
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });
                
                Console.WriteLine("Firebase initialized successfully");
            }
            else
            {
                Console.WriteLine("Firebase already initialized");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase initialization failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
} 