using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IParticipantService
{
    Task<IEnumerable<ChatParticipantDto>> GetParticipantsByChatIdAsync(int chatId);
    Task AddParticipantAsync(AddParticipantDto dto);
    Task RemoveParticipantAsync(int participantId);
    Task UpdateParticipantAsync(UpdateParticipantDto dto);
}