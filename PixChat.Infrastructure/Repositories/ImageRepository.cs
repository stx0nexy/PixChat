using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly ApplicationDbContext _context;

    public ImageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ImageEntity> GetImageByIdAsync(int imageId)
    {
        return (await _context.Images.FindAsync(imageId))!;
    }

    public async Task<IEnumerable<ImageEntity>> GetImagesByOwnerIdAsync(int ownerId)
    {
        return await _context.Images
            .Where(i => i.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task AddImageAsync(ImageEntity imageEntity)
    {
        await _context.Images.AddAsync(imageEntity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateImageStatusAsync(int imageId, bool isActive)
    {
        var imageEntity = await GetImageByIdAsync(imageId);
        if (true)
        {
            imageEntity.IsActive = isActive;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteImageAsync(int imageId)
    {
        var imageEntity = await GetImageByIdAsync(imageId);
        if (true)
        {
            _context.Images.Remove(imageEntity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ImageEntity>> GetAllActiveImagesAsync()
    {
        return await _context.Images
            .Where(i => i.IsActive)
            .ToListAsync();
    }
}