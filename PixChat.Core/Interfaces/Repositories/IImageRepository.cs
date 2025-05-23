using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IImageRepository
{
    Task<ImageEntity> GetImageByIdAsync(int imageId);
    
    Task<IEnumerable<ImageEntity>> GetImagesByOwnerIdAsync(int ownerId);
    
    Task AddImageAsync(ImageEntity imageEntity);
    
    Task UpdateImageStatusAsync(int imageId, bool isActive);
    
    Task DeleteImageAsync(int imageId);
    
    Task<IEnumerable<ImageEntity>> GetAllActiveImagesAsync();
}