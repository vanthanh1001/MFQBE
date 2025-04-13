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
                if (_environment.IsProduction())
                {
                    // Sử dụng biến môi trường trong môi trường sản xuất (Azure)
                    Console.WriteLine("Production environment detected. Using environment variables for Firebase credentials.");
                    var firebaseCredentialsJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                    
                    if (string.IsNullOrEmpty(firebaseCredentialsJson))
                    {
                        throw new InvalidOperationException("FIREBASE_CREDENTIALS environment variable is not set. Firebase initialization failed.");
                    }
                    
                    var credential = GoogleCredential.FromJson(firebaseCredentialsJson);
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = credential
                    });
                    
                    Console.WriteLine("Firebase initialized successfully using environment variable");
                }
                else
                {
                    // Sử dụng file cục bộ trong môi trường phát triển
                    var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "credentials", "firebase-adminsdk.json");
                    
                    Console.WriteLine($"Development environment detected.");
                    Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                    Console.WriteLine($"Looking for credentials file at: {pathToKey}");
                    
                    if (!File.Exists(pathToKey))
                    {
                        throw new FileNotFoundException($"Firebase credentials file not found at {pathToKey}. Please create the file based on the example.json template.");
                    }

                    Console.WriteLine("Found credentials file, initializing Firebase...");
                    var credential = GoogleCredential.FromFile(pathToKey);
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = credential
                    });
                    
                    Console.WriteLine("Firebase initialized successfully using local file");
                }
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