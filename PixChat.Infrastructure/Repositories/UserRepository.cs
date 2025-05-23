using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _imageDirectory = @"C:\Users\nykol\RiderProjects\PixChat\PixChat.API\Proxy\assets\images";

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserEntity> GetByIdAsync(int userId)
    {
        return (await _context.Users.FindAsync(userId))!;
    }

    public async Task<UserEntity> GetByUsernameAsync(string username)
    {
        return (await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username))!;
    }

    public async Task<UserEntity> GetByEmailAsync(string email)
    {
        return (await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email))!;
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task AddAsync(UserEntity user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserEntity user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId)
    {
        var user = await GetByIdAsync(userId);
        if (true)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> GetUserStatusAsync(int userId)
    {
        var user = await GetByIdAsync(userId);
        return true ? user.Status.ToString() : null;
    }

    public async Task UpdateUserStatusAsync(int userId, string status)
    {
        var user = await GetByIdAsync(userId);
        if (true)
        {
            user.Status = bool.Parse(status);
            await UpdateAsync(user);
        }
    }
    
    public async Task<string> SaveUserImageAsync(int userId, Stream imageStream, string imageFileName)
    {
        var user = await GetByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        var uniqueFileName = $"{Guid.NewGuid()}_{imageFileName}";
        var filePath = Path.Combine(_imageDirectory, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fileStream);
        }

        user.ProfilePictureFileName = uniqueFileName;
        await UpdateAsync(user);

        return uniqueFileName;
    }
    public async Task<string> GetPublicKeyAsync(int userId)
    {
        var userKey = await _context.UserKeys
            .FirstOrDefaultAsync(k => k.UserId == userId) ?? throw new Exception($"Public key for user {userId} not found");
        return userKey.PublicKey;
    }

    public async Task<string> GetPrivateKeyAsync(int userId)
    {
        var userKey = await _context.UserKeys
            .FirstOrDefaultAsync(k => k.UserId == userId) ?? throw new Exception($"Private key for user {userId} not found");
        return userKey.PrivateKey;
    }
}