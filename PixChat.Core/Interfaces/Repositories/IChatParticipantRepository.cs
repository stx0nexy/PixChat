using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IChatParticipantRepository
{
    Task<IEnumerable<ChatParticipantEntity>> GetParticipantsByChatIdAsync(int chatId);
    Task<ChatParticipantEntity?> GetByIdAsync(int participantId);
    Task AddAsync(ChatParticipantEntity participant);
    Task DeleteAsync(int participantId);
    Task UpdateAsync(ChatParticipantEntity participant);
}