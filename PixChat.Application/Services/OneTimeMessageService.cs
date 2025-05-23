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

public class OneTimeMessageService : BaseDataService<ApplicationDbContext>, IOneTimeMessageService
{
    private readonly IOneTimeMessageRepository _oneTimeMessageRepository;
    private readonly IMapper _mapper;

    public OneTimeMessageService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IOneTimeMessageRepository oneTimeMessageRepository,
        IMapper mapper
    ) : base(dbContextWrapper, logger)
    {
        _oneTimeMessageRepository = oneTimeMessageRepository;
        _mapper = mapper;
    }

    public async Task<OneTimeMessageDto?> GetMessageByIdAsync(string id)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _oneTimeMessageRepository.GetByIdAsync(id);
            return _mapper.Map<OneTimeMessageDto>(result);
        });
    }

    public async Task<IEnumerable<OneTimeMessageDto>> GetMessagesByReceiverIdAsync(string receiverId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _oneTimeMessageRepository.GetByReceiverIdAsync(receiverId);
            return result.Select(s => _mapper.Map<OneTimeMessageDto>(s)).ToList();
        });
    }

    public async Task<string> SendMessageAsync( string? senderId, string receiverId,
        int chatId, byte[] stegoImage, string encryptionKey, int messageLength, DateTime createdAt, bool received)

        {
            return await ExecuteSafeAsync(async () =>
            {
                var messageAdd = new OneTimeMessage()
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    ChatId = chatId,
                    StegoImage = stegoImage,
                    EncryptionKey = encryptionKey,
                    MessageLength = messageLength,
                    CreatedAt = createdAt,
                    Received = received

                }; 
                return await _oneTimeMessageRepository.AddAsync(messageAdd);
            });
        }

    public async Task DeleteMessageAsync(string id)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _oneTimeMessageRepository.DeleteAsync(id);
        
        });
    }
    
    public async Task MarkOneTimeMessageAsReceivedAsync(string messageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _oneTimeMessageRepository.MarkOneTimeMessageAsReceivedAsync(messageId);
        
        });
    }
}