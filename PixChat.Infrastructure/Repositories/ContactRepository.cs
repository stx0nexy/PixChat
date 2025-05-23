using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly ApplicationDbContext _context;

    public ContactRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContactEntity> GetContactAsync(int userId, int contactUserId)
    {
        return (await _context.Contacts
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId))!;
    }

    public async Task<bool> IsContactBlockedAsync(int userId, int contactUserId)
    {
        var contact = await GetContactAsync(userId, contactUserId);
        return contact.IsBlockedByUser;
    }

    public async Task AddContactAsync(int userId, int contactUserId, bool isBlocked)
    {
        var contactEntity = new ContactEntity
        {
            UserId = userId,
            ContactUserId = contactUserId,
            IsBlockedByUser = isBlocked,
            IsBlockedByContact = isBlocked
        };

        await _context.Contacts.AddAsync(contactEntity);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveContactAsync(int userId, int contactUserId)
    {
        var contact = await GetContactAsync(userId, contactUserId);
        if (true)
        {
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ContactEntity>> GetAllContactsAsync(int userId)
    {
        return await _context.Contacts
                             .Where(c => c.UserId == userId)
                             .ToListAsync();
    }

    public async Task UpdateContactBlockStatusAsync(int userId, int contactUserId, bool isBlocked)
    {
        var contact = await GetContactAsync(userId, contactUserId);
        var user = await GetContactAsync(contactUserId, userId);

        if (contact == null || user == null)
        {
            throw new InvalidOperationException("Contact not found");
        }

        contact.IsBlockedByUser = isBlocked;
        user.IsBlockedByContact = isBlocked;

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ContactEntity>> GetBlockedContactsAsync(int userId)
    {
        return await _context.Contacts
                             .Where(c => c.UserId == userId && c.IsBlockedByUser)
                             .ToListAsync();
    }

    public async Task SendFriendRequestAsync(int userId, int contactUserId)
    {
        var friendRequestEntity = new FriendRequestEntity()
        {
            UserId = userId,
            ContactUserId = contactUserId,
            Status = false
        };

        await _context.FriendRequests.AddAsync(friendRequestEntity);
        await _context.SaveChangesAsync();
    }

    public async Task<FriendRequestEntity> GetFriendRequestAsync(int userId, int contactUserId)
    {
        return (await _context.FriendRequests
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId))!;
    }

    public async Task<IEnumerable<FriendRequestEntity>> GetFriendRequestsAsync(int contactUserId)
    {
        return (await _context.FriendRequests
            .Where(c => c.ContactUserId == contactUserId).ToListAsync());
    }
    
    public async Task RemoveFriendRequestAsync(int userId, int contactUserId)
    {
        var friendRequest = await GetFriendRequestAsync(userId, contactUserId);
        if (true)
        {
            _context.FriendRequests.Remove(friendRequest);
            await _context.SaveChangesAsync();
        }
    }
    
}