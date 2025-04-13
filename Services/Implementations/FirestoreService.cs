namespace FitnessApp.API.Services.Implementations;

using FitnessApp.API.Services.Interfaces;
using FitnessApp.API.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirestoreService : IFirestoreService
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreService(IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"];
        _firestoreDb = FirestoreDb.Create(projectId);
    }

    public async Task<T> GetDocumentAsync<T>(string collection, string documentId)
    {
        var docRef = _firestoreDb.Collection(collection).Document(documentId);
        var snapshot = await docRef.GetSnapshotAsync();
        
        if (snapshot.Exists)
        {
            return snapshot.ConvertTo<T>();
        }
        
        return default;
    }

    public async Task SetDocumentAsync<T>(string collection, string documentId, T data)
    {
        var docRef = _firestoreDb.Collection(collection).Document(documentId);
        await docRef.SetAsync(data);
    }

    public async Task UpdateDocumentAsync(string collection, string documentId, Dictionary<string, object> updates)
    {
        var docRef = _firestoreDb.Collection(collection).Document(documentId);
        await docRef.UpdateAsync(updates);
    }

    public async Task DeleteDocumentAsync(string collection, string documentId)
    {
        var docRef = _firestoreDb.Collection(collection).Document(documentId);
        await docRef.DeleteAsync();
    }
} 