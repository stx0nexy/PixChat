using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Application.Services;

public class ChatService : BaseDataService<ApplicationDbContext>, IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IMapper _mapper;
    private readonly IChatParticipantRepository _participantRepository;

    public ChatService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IChatRepository chatRepository,
        IMapper mapper,
        IChatParticipantRepository participantRepository
    ) : base(dbContextWrapper, logger)
    {
        _chatRepository = chatRepository;
        _mapper = mapper;
        _participantRepository = participantRepository;
    }
    
    public async Task<IEnumerable<ChatDto>> GetAllChatsAsync()
    {
        return await ExecuteSafeAsync(async () =>
        {
            var chats = await _chatRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ChatDto>>(chats);
        });
    }

    public async Task<ChatDto?> GetChatByIdAsync(int id)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var chat = await _chatRepository.GetByIdAsync(id);
            return chat == null ? null : _mapper.Map<ChatDto>(chat);
        });
    }
    
    public async Task<ChatDto?> GetPrivateChatIfExists(int userId, int contactUserId)
    {

        var result = await _chatRepository.GetPrivateChatIfExistsAsync(userId, contactUserId);
        return _mapper.Map<ChatDto?>(result);

    }

    public async Task CreatePrivateChatAsync(int userId, int contactUserId)
    {

        var chat = new ChatEntity
        {
            Name = $"{userId}-{contactUserId}",
            IsGroup = false,
            CreatorId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Participants = new List<ChatParticipantEntity>
            {
                new ChatParticipantEntity { UserId = userId, IsAdmin = true, JoinedAt = DateTime.UtcNow },
                new ChatParticipantEntity { UserId = contactUserId, IsAdmin = false, JoinedAt = DateTime.UtcNow }
            }
        };

        await _chatRepository.AddAsync(chat);
    }
    
    public async Task CreateChatAsync(CreateChatDto dto)
    {

            var chat = _mapper.Map<ChatEntity>(dto);
            chat.CreatedAt = DateTime.UtcNow;
            chat.UpdatedAt = DateTime.UtcNow;
            
            var result = await _chatRepository.AddAsync(chat);

            foreach (var participantId in dto.ParticipantIds)
            {
                await AddParticipantAsync(new AddParticipantDto
                {
                    ChatId = result,
                    UserId = participantId,
                    IsAdmin = participantId == dto.CreatorId
                });
            }

            await AddParticipantAsync(new AddParticipantDto
            {
                ChatId = result,
                UserId = dto.CreatorId,
                IsAdmin = true
            });

    }

    public async Task UpdateChatAsync(UpdateChatDto dto)
    {
        await ExecuteSafeAsync(async () =>
        {
            var chat = await _chatRepository.GetByIdAsync(dto.Id);
            if (chat == null) throw new KeyNotFoundException($"Chat with ID {dto.Id} not found.");

            _mapper.Map(dto, chat);
            chat.UpdatedAt = DateTime.UtcNow;

            await _chatRepository.UpdateAsync(chat);
        });
    }

    public async Task DeleteChatAsync(int id)
    {
        await ExecuteSafeAsync(async () => { await _chatRepository.DeleteAsync(id); });
    }

    public async Task AddParticipantAsync(AddParticipantDto dto)
    {

            var participant = _mapper.Map<ChatParticipantEntity>(dto);
            participant.JoinedAt = DateTime.UtcNow;

            await _participantRepository.AddAsync(participant);
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        await ExecuteSafeAsync(async () => { await _participantRepository.DeleteAsync(participantId); });
    }
    
    public async Task<IEnumerable<UserDto>> GetParticipantsByChatIdAsync(int chatId)
    {
        try
        {
            var participants = await _chatRepository.GetParticipantsByChatIdAsync(chatId);

            var participantDtos = _mapper.Map<IEnumerable<UserDto>>(participants);

            return participantDtos;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "No participants found for chat ID {ChatId}", chatId);
            return Enumerable.Empty<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving participants for chat ID {ChatId}", chatId);
            throw;
        }
    }
    
    public async Task<List<ChatDto>> GetUserChatsAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var chatEntities = await _chatRepository.GetChatsByUserIdAsync(userId);
            return _mapper.Map<List<ChatDto>>(chatEntities);
        });
    }
}