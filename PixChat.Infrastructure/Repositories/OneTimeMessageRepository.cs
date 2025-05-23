using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class OneTimeMessageRepository : IOneTimeMessageRepository
{
    private readonly ApplicationDbContext _context;

    public OneTimeMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OneTimeMessage?> GetByIdAsync(string id)
    {
        return await _context.OneTimeMessages
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<OneTimeMessage>> GetByReceiverIdAsync(string receiverId)
    {
        return await _context.OneTimeMessages
            .Where(m => m.ReceiverId == receiverId)
            .ToListAsync();
    }

    public async Task<string> AddAsync(OneTimeMessage message)
    {
        await _context.OneTimeMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        return message.Id;
    }

    public async Task UpdateAsync(OneTimeMessage message)
    {
        _context.OneTimeMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var message = await GetByIdAsync(id);
        if (message != null)
        {
            _context.OneTimeMessages.Remove(message);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task MarkOneTimeMessageAsReceivedAsync(string messageId)
    {
        var message = await _context.OneTimeMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Received = true;
            await _context.SaveChangesAsync();
        }
    }
}
