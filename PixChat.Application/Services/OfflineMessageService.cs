using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Infrastructure.Database;

namespace PixChat.Application.Services;

public class OfflineMessageService : IOfflineMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OfflineMessageService> _logger;

    public OfflineMessageService(ApplicationDbContext context, ILogger<OfflineMessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveMessageAsync(OfflineMessageEntity message)
    {
        try
        {
            _logger.LogInformation($"Saving offline message for receiver: {message.ReceiverId}");
            
            await _context.OfflineMessages.AddAsync(message);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Successfully saved offline message with ID: {message.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving offline message for receiver: {message.ReceiverId}");
            throw;
        }
    }
    
    public async Task SaveMessageAsync(OfflineMessageFileEntity fileMessage)
    {
        try
        {
            _logger.LogInformation($"Saving offline file message for receiver: {fileMessage.ReceiverId}");

            await _context.OfflineMessageFiles.AddAsync(fileMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully saved offline file message with ID: {fileMessage.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving offline file message for receiver: {fileMessage.ReceiverId}");
            throw;
        }
    }

    public async Task<IEnumerable<OfflineMessageEntity>> GetPendingMessagesAsync(string receiverId)
    {
        try
        {
            _logger.LogInformation($"Fetching pending messages for receiver: {receiverId}");
            
            var messages = await _context.OfflineMessages
                .Where(m => m.ReceiverId == receiverId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            
            _logger.LogInformation($"Found {messages.Count} pending messages for receiver: {receiverId}");
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching pending messages for receiver: {receiverId}");
            throw;
        }
    }
    
    public async Task<IEnumerable<OfflineMessageFileEntity>> GetPendingFileMessagesAsync(string receiverId)
    {
        try
        {
            _logger.LogInformation($"Fetching pending file messages for receiver: {receiverId}");

            var fileMessages = await _context.OfflineMessageFiles
                .Where(m => m.ReceiverId == receiverId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"Found {fileMessages.Count} pending file messages for receiver: {receiverId}");

            return fileMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching pending file messages for receiver: {receiverId}");
            throw;
        }
    }

    public async Task DeleteMessageAsync(string messageId)
        {
            try
            {
                _logger.LogInformation($"Deleting offline message with ID: {messageId}");

                var deletedMessages = await _context.OfflineMessages
                    .Where(m => m.Id == messageId)
                    .ExecuteDeleteAsync();

                if (deletedMessages > 0)
                {
                    _logger.LogInformation($"Successfully deleted offline message with ID: {messageId}");
                    return;
                }

                var deletedFiles = await _context.OfflineMessageFiles
                    .Where(m => m.Id == messageId)
                    .ExecuteDeleteAsync();

                if (deletedFiles > 0)
                {
                    _logger.LogInformation($"Successfully deleted offline file message with ID: {messageId}");
                }
                else
                {
                    _logger.LogWarning($"Message with ID: {messageId} not found or already deleted in both tables");
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Concurrency conflict while deleting message with ID: {messageId}");

                _context.ChangeTracker.Clear();

                var existsInMessages = await _context.OfflineMessages.AnyAsync(m => m.Id == messageId);
                var existsInFiles = await _context.OfflineMessageFiles.AnyAsync(m => m.Id == messageId);

                if (!existsInMessages && !existsInFiles)
                {
                    _logger.LogInformation($"Message with ID: {messageId} was already deleted");
                    return;
                }

                try
                {
                    if (existsInMessages)
                    {
                        var message = await _context.OfflineMessages.FindAsync(messageId);
                        if (message != null)
                        {
                            _context.OfflineMessages.Remove(message);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Successfully deleted message with ID: {messageId} after resolving concurrency conflict");
                        }
                    }
                    else if (existsInFiles)
                    {
                        var fileMessage = await _context.OfflineMessageFiles.FindAsync(messageId);
                        if (fileMessage != null)
                        {
                            _context.OfflineMessageFiles.Remove(fileMessage);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Successfully deleted file message with ID: {messageId} after resolving concurrency conflict");
                        }
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, $"Failed to delete message with ID: {messageId} after retry");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting offline message with ID: {messageId}");
                throw;
            }
        }

        public async Task MarkMessageAsReceivedAsync(string messageId)
        {
            try
            {
                var message = await _context.OfflineMessages.FindAsync(messageId);
                if (message != null)
                {
                    message.Received = true;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Marked message with ID: {messageId} as received");
                    return;
                }

                var fileMessage = await _context.OfflineMessageFiles.FindAsync(messageId);
                if (fileMessage != null)
                {
                    fileMessage.Received = true;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Marked file message with ID: {messageId} as received");
                }
                else
                {
                    _logger.LogWarning($"Message with ID: {messageId} not found in either table");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message with ID: {messageId} as received");
                throw;
            }
        }
}