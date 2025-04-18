using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DTOs;
using FitnessApp.API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Enums;

namespace FitnessApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExerciseImagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirebaseStorageService _storageService;
        private readonly IUserService _userService;

        public ExerciseImagesController(
            ApplicationDbContext context,
            IFirebaseStorageService storageService,
            IUserService userService)
        {
            _context = context;
            _storageService = storageService;
            _userService = userService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadExerciseImages([FromForm] ExerciseImageUploadDto model)
        {
            try
            {
                // Get current user
                var currentUser = await _userService.GetCurrentUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Kiểm tra quyền - chỉ Trainer và Admin mới được phép upload
                if (currentUser.Role != UserRole.Trainer && currentUser.Role != UserRole.Admin)
                {
                    return Forbid("Bạn không có quyền upload ảnh bài tập. Chỉ PT mới có thể thực hiện chức năng này.");
                }

                // Find the exercise
                var exercise = await _context.Exercises
                    .FirstOrDefaultAsync(e => e.Id == model.ExerciseId);

                if (exercise == null)
                {
                    return NotFound($"Exercise with ID {model.ExerciseId} not found");
                }

                // Check if user is authorized to modify this exercise
                // Admin có thể sửa tất cả, Trainer chỉ sửa được bài tập của mình
                bool isAdmin = currentUser.Role == UserRole.Admin;
                bool isOwner = exercise.CreatedById == currentUser.Id;
                
                if (!isAdmin && !isOwner)
                {
                    return Forbid("Bạn không có quyền sửa bài tập này");
                }

                // Đảm bảo DetailImageUrls được khởi tạo
                if (exercise.DetailImageUrls == null)
                {
                    exercise.DetailImageUrls = new List<string>();
                }

                // Upload thumbnail image if provided
                if (model.ThumbnailImage != null)
                {
                    var thumbnailUrl = await _storageService.UploadFileAsync(
                        model.ThumbnailImage.OpenReadStream(),
                        model.ThumbnailImage.FileName,
                        $"exercises/{model.ExerciseId}/thumbnail");

                    // Delete old thumbnail if exists
                    if (!string.IsNullOrEmpty(exercise.ThumbnailImageUrl))
                    {
                        await _storageService.DeleteFileAsync(exercise.ThumbnailImageUrl);
                    }

                    exercise.ThumbnailImageUrl = thumbnailUrl;
                }

                // Upload detail images if provided
                if (model.DetailImages != null && model.DetailImages.Count > 0)
                {
                    var detailUrls = new List<string>();

                    // Delete old detail images if they exist
                    if (exercise.DetailImageUrls != null && exercise.DetailImageUrls.Count > 0)
                    {
                        foreach (var url in exercise.DetailImageUrls)
                        {
                            await _storageService.DeleteFileAsync(url);
                        }
                    }

                    // Upload new detail images
                    foreach (var image in model.DetailImages)
                    {
                        var imageUrl = await _storageService.UploadFileAsync(
                            image.OpenReadStream(),
                            image.FileName,
                            $"exercises/{model.ExerciseId}/details");

                        detailUrls.Add(imageUrl);
                    }

                    exercise.DetailImageUrls = detailUrls;
                }

                exercise.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Exercise images uploaded successfully",
                    exercise = new
                    {
                        id = exercise.Id,
                        name = exercise.Name,
                        thumbnailUrl = exercise.ThumbnailImageUrl,
                        detailUrls = exercise.DetailImageUrls
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetExerciseImages(int id)
        {
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise == null)
            {
                return NotFound($"Exercise with ID {id} not found");
            }

            return Ok(new
            {
                thumbnailUrl = exercise.ThumbnailImageUrl,
                detailUrls = exercise.DetailImageUrls
            });
        }
    }
}