using AutoMapper;
using Microsoft.Extensions.Options;
using PixChat.Application.Config;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;

namespace PixChat.Application.Mappings;

public class ImagePictureResolver : IMemberValueResolver<ImageEntity, ImageDto, string, object>
{
    private readonly ChatConfig _config;

    public ImagePictureResolver(IOptionsSnapshot<ChatConfig> config)
    {
        _config = config.Value;
    }

    public object Resolve(ImageEntity source, ImageDto destination, string sourceMember, object destMember, ResolutionContext context)
    {
        return $"{_config.CdnHost}/{_config.ImgUrl}/{sourceMember}";
    }
}