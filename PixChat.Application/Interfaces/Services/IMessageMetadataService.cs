using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IMessageMetadataService
{
    Task<MessageMetadataDto> GetMessageMetadata(int messageId);
    
    Task<IEnumerable<MessageMetadataDto>> GetSentMessages(int senderId);
    
    Task<IEnumerable<MessageMetadataDto>> GetReceivedMessages(int receiverId);
    
    Task AddMessageMetadata(MessageMetadataDto messageMetadataDto);
    
    Task UpdateMessageStatus(int messageId, string status);
    
    Task DeleteMessageMetadata(int messageId);
}