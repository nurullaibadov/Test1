using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Test1.Application.Interfaces.Services;

namespace Test1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public UploadController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost("image")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "general")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { Success = false, Message = "No file uploaded" });

                var imageUrl = await _fileUploadService.UploadImageAsync(file, folder);

                return Ok(new
                {
                    Success = true,
                    Message = "Image uploaded successfully",
                    Data = new { ImageUrl = imageUrl }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("images")]
        [Authorize]
        public async Task<IActionResult> UploadImages(List<IFormFile> files, [FromQuery] string folder = "general")
        {
            try
            {
                if (files == null || files.Count == 0)
                    return BadRequest(new { Success = false, Message = "No files uploaded" });

                var imageUrls = await _fileUploadService.UploadImagesAsync(files, folder);

                return Ok(new
                {
                    Success = true,
                    Message = $"{imageUrls.Count} images uploaded successfully",
                    Data = new { ImageUrls = imageUrls }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpDelete("image")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return BadRequest(new { Success = false, Message = "Image URL is required" });

                var deleted = await _fileUploadService.DeleteImageAsync(imageUrl);

                if (!deleted)
                    return NotFound(new { Success = false, Message = "Image not found" });

                return Ok(new
                {
                    Success = true,
                    Message = "Image deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }

}
