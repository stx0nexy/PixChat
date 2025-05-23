using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;

namespace PixChat.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ImageDto>> GetImageById(int id)
        {
            var image = await _imageService.GetImageById(id);

            if (image == null)
            {
                return NotFound();
            }

            return Ok(image);
        }
        
        [HttpGet("bytes/{id}")]
        public async Task<ActionResult<byte[]>> GetImageBytesById(int id)
        {
            try
            {
                var imageBytes = await _imageService.GetImageBytesByIdAsync(id);
                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetImagesByOwnerId(int ownerId)
        {
            var images = await _imageService.GetImagesByOwnerId(ownerId);
            return Ok(images);
        }

        [HttpPost]
        public async Task<ActionResult> AddImage([FromBody] ImageDto imageDto)
        {
            if (imageDto == null)
            {
                return BadRequest("Invalid image data.");
            }

            await _imageService.AddImage(imageDto);
            return CreatedAtAction(nameof(GetImageById), new { id = imageDto.Id }, imageDto);
        }

        [HttpPut("status/{id}")]
        public async Task<ActionResult> UpdateImageStatus(int id, [FromBody] bool isActive)
        {
            await _imageService.UpdateImageStatus(id, isActive);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteImage(int id)
        {
            await _imageService.DeleteImage(id);
            return NoContent();
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetAllActiveImages()
        {
            var images = await _imageService.GetAllActiveImages();
            return Ok(images);
        }
    }
}
