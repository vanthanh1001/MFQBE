namespace FitnessApp.API.Services.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFirestoreService
{
    Task<T> GetDocumentAsync<T>(string collection, string documentId);
    Task SetDocumentAsync<T>(string collection, string documentId, T data);
    Task UpdateDocumentAsync(string collection, string documentId, Dictionary<string, object> updates);
    Task DeleteDocumentAsync(string collection, string documentId);
} 