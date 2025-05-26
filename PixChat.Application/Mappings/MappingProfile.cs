using AutoMapper;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;

namespace PixChat.Application.Mappings;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<ContactEntity, ContactDto>();
        CreateMap<ImageEntity, ImageDto>()
            .ForMember("ImageUrl", opt
                => opt.MapFrom<ImagePictureResolver, string>(c => c.PictureFileName));
        CreateMap<MessageMetadata, MessageMetadataDto>();
        CreateMap<UserEntity, UserDto>()
            .ForMember("ProfilePictureUrl", opt
                => opt.MapFrom<UserPictureResolver, string>(c => c.ProfilePictureFileName));
        CreateMap<UserDto, UserEntity>()
            .ForMember(dest => dest.ProfilePictureFileName, opt => opt.Ignore());
        CreateMap<ChatEntity, ChatDto>()
            .ForMember(dest => dest.ParticipantIds, opt => opt.MapFrom(src => src.Participants.Select(p => p.UserId)));

        CreateMap<CreateChatDto, ChatEntity>();

        CreateMap<UpdateChatDto, ChatEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()); // Не обновляем ID

        CreateMap<ChatParticipantEntity, ChatParticipantDto>();
        CreateMap<AddParticipantDto, ChatParticipantEntity>();

        CreateMap<OneTimeMessage, OneTimeMessageDto>();
        CreateMap<OneTimeMessageDto, OneTimeMessage>();
    }
}