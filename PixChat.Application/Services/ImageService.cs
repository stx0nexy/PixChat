using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PixChat.Application.Config;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Application.Services;

public class ImageService : BaseDataService<ApplicationDbContext>, IImageService
{
    private readonly IImageRepository _imageRepository;
    private readonly IMapper _mapper;
    private readonly ChatConfig _config;

    public ImageService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IImageRepository imageRepository,
        IMapper mapper ,
        IOptionsSnapshot<ChatConfig> config
    ) : base(dbContextWrapper, logger)
    {
        _imageRepository = imageRepository;
        _mapper = mapper;
        _config = config.Value;
    }

    public async Task<ImageDto> GetImageById(int imageId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _imageRepository.GetImageByIdAsync(imageId);
            return _mapper.Map<ImageDto>(result);
        });
    }
    
    public async Task<byte[]> GetImageBytesByIdAsync(int imageId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var image = await _imageRepository.GetImageByIdAsync(imageId);
        
            if (image == null || string.IsNullOrEmpty(image.PictureFileName))
            {
                throw new Exception("");
            }

            var imageUrl = $"C:/Users/nykol/RiderProjects/PixChat/Proxy/assets/images/{image.PictureFileName}";

            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                return imageBytes;
            }
        });
    }

    public async Task<IEnumerable<ImageDto>> GetImagesByOwnerId(int ownerId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _imageRepository.GetImagesByOwnerIdAsync(ownerId);
            return result.Select(s => _mapper.Map<ImageDto>(s)).ToList();
        });
    }

    public async Task AddImage(ImageDto imageDto)
    {
        await ExecuteSafeAsync(async () =>
        {
            var imageEntity = _mapper.Map<ImageEntity>(imageDto);
            await _imageRepository.AddImageAsync(imageEntity);
        });
    }

    public async Task UpdateImageStatus(int imageId, bool isActive)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _imageRepository.UpdateImageStatusAsync(imageId, isActive);
        });
    }

    public async Task DeleteImage(int imageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _imageRepository.DeleteImageAsync(imageId);
        });
    }

    public async Task<IEnumerable<ImageDto>> GetAllActiveImages()
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _imageRepository.GetAllActiveImagesAsync();
            return result.Select(s => _mapper.Map<ImageDto>(s)).ToList();
        });
    }
}