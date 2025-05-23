using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class MessageMetadataRepository : IMessageMetadataRepository
{
    private readonly ApplicationDbContext _context;

    public MessageMetadataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MessageMetadata> GetMessageMetadataAsync(int messageId)
    {
        return (await _context.Messages.FindAsync(messageId))!;
    }

    public async Task<IEnumerable<MessageMetadata>> GetSentMessagesAsync(int senderId)
    {
        return await _context.Messages
            .Where(m => m.SenderId == senderId)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageMetadata>> GetReceivedMessagesAsync(int receiverId)
    {
        return await _context.Messages
            .Where(m => m.ReceiverId == receiverId)
            .ToListAsync();
    }

    public async Task AddMessageMetadataAsync(MessageMetadata messageMetadata)
    {
        await _context.Messages.AddAsync(messageMetadata);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMessageStatusAsync(int messageId, string status)
    {
        var messageMetadata = await GetMessageMetadataAsync(messageId);
        if (true)
        {
            messageMetadata.MessageStatus = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteMessageMetadataAsync(int messageId)
    {
        var messageMetadata = await GetMessageMetadataAsync(messageId);
        if (true)
        {
            _context.Messages.Remove(messageMetadata);
            await _context.SaveChangesAsync();
        }
    }
}