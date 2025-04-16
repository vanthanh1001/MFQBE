using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text.Json;

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
                string jsonContent;
                // if (_environment.IsProduction())
                // {
                    var firebaseSection = _configuration.GetSection("Firebase:Credentials");
                
                if (!firebaseSection.Exists())
                {
                    throw new InvalidOperationException("Firebase credentials not found in configuration");
                }

                // Use JsonSerializerOptions to maintain the exact JSON structure
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var credentials = firebaseSection.Get<Dictionary<string, object>>();
                jsonContent = JsonSerializer.Serialize(credentials, jsonOptions);
                Console.WriteLine($"Firebase: {jsonContent}");

                // var credential = GoogleCredential.FromJson(jsonContent)
                //     .CreateScoped("https://www.googleapis.com/auth/firebase");
                
                // FirebaseApp.Create(new AppOptions
                // {
                //     Credential = credential,
                //     ProjectId = "mfquest-b89b0"
                // });
                // }
                // else
                // {
                //     var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "credentials", "firebase-adminsdk.json");
                //     Console.WriteLine($"Reading credentials from: {pathToKey}");
                //     jsonContent = File.ReadAllText(pathToKey);
                // }

                var credential = GoogleCredential.FromJson(jsonContent);
                FirebaseApp.Create(new AppOptions
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