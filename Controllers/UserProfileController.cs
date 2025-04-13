using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FitnessApp.API.Services.Interfaces;
using FitnessApp.API.Models;

namespace FitnessApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IFirebaseStorageService _storageService;
    private readonly IFirestoreService _firestoreService;

    public UserProfileController(
        IFirebaseStorageService storageService,
        IFirestoreService firestoreService)
    {
        _storageService = storageService;
        _firestoreService = firestoreService;
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            // Kiểm tra file type
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("File must be an image.");

            // Lấy user ID từ token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Upload ảnh
            var imageUrl = await _storageService.UploadProfileImageAsync(file, userId);

            // Lưu URL vào Firestore
            await _firestoreService.UpdateDocumentAsync("users", userId, new Dictionary<string, object>
            {
                { "avatarUrl", imageUrl },
                { "updatedAt", DateTime.UtcNow }
            });

            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Lấy current avatar URL từ Firestore
            var user = await _firestoreService.GetDocumentAsync<UserProfile>("users", userId);
            if (!string.IsNullOrEmpty(user?.AvatarUrl))
            {
                await _storageService.DeleteFileAsync(user.AvatarUrl);
                
                // Xóa URL khỏi Firestore
                await _firestoreService.UpdateDocumentAsync("users", userId, new Dictionary<string, object>
                {
                    { "avatarUrl", null },
                    { "updatedAt", DateTime.UtcNow }
                });
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
} 