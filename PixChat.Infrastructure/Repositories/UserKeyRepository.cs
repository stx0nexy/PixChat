using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class UserKeyRepository : BaseDataService, IUserKeyRepository
{
    public UserKeyRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<UserKeyRepository> logger) : base(dbContextWrapper, logger)
    {
    }

    public async Task<(string publicKey, string privateKey)> GenerateKeyPairAsync()
    {
        try
        {
            using (RSA rsa = RSA.Create(2048))
            {
                string publicKeyPem = ConvertToPem(rsa.ExportSubjectPublicKeyInfo(), "PUBLIC KEY");
                string privateKeyPem = ConvertToPem(rsa.ExportPkcs8PrivateKey(), "PRIVATE KEY");

                _logger.LogInformation("Generated new RSA key pair in PEM format.");
                return (publicKeyPem, privateKeyPem);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RSA key pair.");
            throw;
        }
    }

    private string ConvertToPem(byte[] keyBytes, string keyType)
    {
        string base64 = Convert.ToBase64String(keyBytes);
        string header = $"-----BEGIN {keyType}-----";
        string footer = $"-----END {keyType}-----";
        string wrappedBase64 = string.Join("\n", base64.Chunk(64).Select(chunk => new string(chunk)));
        return $"{header}\n{wrappedBase64}\n{footer}";
    }

    public async Task SaveKeysAsync(int userId, string publicKey, string privateKey)
    {
        await ExecuteSafeAsync(async () =>
        {
            var existingKeys = await Context.UserKeys
                .FirstOrDefaultAsync(k => k.UserId == userId);

            if (existingKeys != null)
            {
                existingKeys.PublicKey = publicKey;
                existingKeys.PrivateKey = privateKey;
            }
            else
            {
                var userKeys = new UserKeyEntity
                {
                    UserId = userId,
                    PublicKey = publicKey,
                    PrivateKey = privateKey
                };
                Context.UserKeys.Add(userKeys);
            }

            await Context.SaveChangesAsync();
            _logger.LogInformation($"Keys saved for userId: {userId}");
        });
    }

    public async Task<string?> GetPublicKeyAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var userKeys = await Context.UserKeys
                .FirstOrDefaultAsync(k => k.UserId == userId);

            if (userKeys == null)
            {
                _logger.LogWarning($"Public key not found for userId: {userId}");
                return null;
            }

            return userKeys.PublicKey;
        });
    }

    public async Task<string?> GetPrivateKeyAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var userKeys = await Context.UserKeys
                .FirstOrDefaultAsync(k => k.UserId == userId);

            if (userKeys == null)
            {
                _logger.LogWarning($"Private key not found for userId: {userId}");
                return null;
            }

            return userKeys.PrivateKey;
        });
    }
}