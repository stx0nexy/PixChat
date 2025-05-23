using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class ChatParticipantRepository : IChatParticipantRepository
{
    private readonly ApplicationDbContext _context;

    public ChatParticipantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChatParticipantEntity>> GetParticipantsByChatIdAsync(int chatId)
    {
        return await _context.ChatParticipants
            .Where(p => p.ChatId == chatId)
            .ToListAsync();
    }

    public async Task<ChatParticipantEntity?> GetByIdAsync(int participantId)
    {
        return await _context.ChatParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId);
    }

    public async Task AddAsync(ChatParticipantEntity participant)
    {
        await _context.ChatParticipants.AddAsync(participant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int participantId)
    {
        var participant = await GetByIdAsync(participantId);
        if (participant != null)
        {
            _context.ChatParticipants.Remove(participant);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(ChatParticipantEntity participant)
    {
        _context.ChatParticipants.Update(participant);
        await _context.SaveChangesAsync();
    }
}