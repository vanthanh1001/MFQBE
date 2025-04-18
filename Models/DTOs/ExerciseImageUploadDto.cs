using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Models.DTOs
{
    public class ExerciseImageUploadDto
    {
        public int ExerciseId { get; set; }
        public IFormFile ThumbnailImage { get; set; }
        public List<IFormFile> DetailImages { get; set; }
    }
}