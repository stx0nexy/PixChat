using PixChat.Core.Entities;

namespace PixChat.Application.Interfaces.Services;

public interface IOfflineMessageService
{
    Task SaveMessageAsync(OfflineMessageEntity message);
    Task SaveMessageAsync(OfflineMessageFileEntity fileMessage);

    Task<IEnumerable<OfflineMessageEntity>> GetPendingMessagesAsync(string receiverId);
    Task<IEnumerable<OfflineMessageFileEntity>> GetPendingFileMessagesAsync(string receiverId);
    Task DeleteMessageAsync(string messageId);
    Task MarkMessageAsReceivedAsync(string messageId);
}