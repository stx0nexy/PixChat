
using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IMessageMetadataRepository
{
    Task<MessageMetadata> GetMessageMetadataAsync(int messageId);
    
    Task<IEnumerable<MessageMetadata>> GetSentMessagesAsync(int senderId);
    
    Task<IEnumerable<MessageMetadata>> GetReceivedMessagesAsync(int receiverId);
    
    Task AddMessageMetadataAsync(MessageMetadata messageMetadata);
    
    Task UpdateMessageStatusAsync(int messageId, string status);
    
    Task DeleteMessageMetadataAsync(int messageId);
}