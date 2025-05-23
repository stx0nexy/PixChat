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

public class MessageMetadataService : BaseDataService<ApplicationDbContext>, IMessageMetadataService
{
    private readonly IMessageMetadataRepository _messageMetadataRepository;
    private readonly IMapper _mapper;

    public MessageMetadataService(IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IMessageMetadataRepository messageMetadataRepository,
        IMapper mapper
    ) : base(dbContextWrapper, logger)
    {
        _messageMetadataRepository = messageMetadataRepository;
        _mapper = mapper;
    }

    public async Task<MessageMetadataDto> GetMessageMetadata(int messageId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _messageMetadataRepository.GetMessageMetadataAsync(messageId);
            return _mapper.Map<MessageMetadataDto>(result);
        });
    }

    public async Task<IEnumerable<MessageMetadataDto>> GetSentMessages(int senderId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _messageMetadataRepository.GetSentMessagesAsync(senderId);
            return result.Select(s => _mapper.Map<MessageMetadataDto>(s)).ToList();
        });
    }

    public async Task<IEnumerable<MessageMetadataDto>> GetReceivedMessages(int receiverId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _messageMetadataRepository.GetReceivedMessagesAsync(receiverId);
            return result.Select(s => _mapper.Map<MessageMetadataDto>(s)).ToList();
        });
    }

    public async Task AddMessageMetadata(MessageMetadataDto messageMetadataDto)
    {
        await ExecuteSafeAsync(async () =>
        {
            var messageMetadataEntity = _mapper.Map<MessageMetadata>(messageMetadataDto);
            await _messageMetadataRepository.AddMessageMetadataAsync(messageMetadataEntity);
        });
    }

    public async Task UpdateMessageStatus(int messageId, string status)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _messageMetadataRepository.UpdateMessageStatusAsync(messageId, status);
        });
    }

    public async Task DeleteMessageMetadata(int messageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _messageMetadataRepository.DeleteMessageMetadataAsync(messageId);
        });
        
    }
}