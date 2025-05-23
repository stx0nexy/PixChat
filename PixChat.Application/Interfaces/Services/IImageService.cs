
using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IImageService
{
    Task<ImageDto> GetImageById(int imageId);

    Task<byte[]> GetImageBytesByIdAsync(int imageId);
    
    Task<IEnumerable<ImageDto>> GetImagesByOwnerId(int ownerId);
    
    Task AddImage(ImageDto imageDto);
    
    Task UpdateImageStatus(int imageId, bool isActive);
    
    Task DeleteImage(int imageId);
    
    Task<IEnumerable<ImageDto>> GetAllActiveImages();
}