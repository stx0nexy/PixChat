using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class ChatRepository: IChatRepository
{
    private readonly ApplicationDbContext _context;
    private ILogger<ChatRepository> _logger;

    public ChatRepository(ApplicationDbContext context, ILogger<ChatRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChatEntity?> GetPrivateChatIfExistsAsync(int userId, int contactUserId)
    {
        return await _context.Chats
            .Where(c => !c.IsGroup &&
                        c.Participants.Any(p => p.UserId == userId) &&
                        c.Participants.Any(p => p.UserId == contactUserId))
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<ChatEntity>> GetAllAsync()
    {
        return await _context.Chats
            .Include(c => c.Participants)
            .ToListAsync();
    }

    public async Task<ChatEntity?> GetByIdAsync(int id)
    {
        return await _context.Chats
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> AddAsync(ChatEntity entity)
    {
        await _context.Chats.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(ChatEntity entity)
    {
        _context.Chats.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Chats.FindAsync(id);
        if (entity != null)
        {
            _context.Chats.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<IEnumerable<UserEntity>> GetParticipantsByChatIdAsync(int chatId)
    {
        try
        {
            var participants = await _context.ChatParticipants
                .Where(cp => cp.ChatId == chatId)
                .Include(cp => cp.User)
                .Select(cp => cp.User)
                .ToListAsync();

            if (!participants.Any())
            {
                throw new KeyNotFoundException($"No participants found for chat with ID {chatId}.");
            }

            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving participants for chat ID {ChatId}", chatId);
            throw;
        }
    }
    
    public async Task<List<ChatEntity>> GetChatsByUserIdAsync(int userId)
    {
        return await _context.Set<ChatEntity>()
            .Where(chat => chat.Participants.Any(p => p.UserId == userId))
            .ToListAsync();
    }

}