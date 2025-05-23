using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Infrastructure.Database;

namespace PixChat.Application.Services;

public class KeyService : IKeyService
{
   private readonly ApplicationDbContext _context;
    private readonly ILogger<KeyService> _logger;

    public KeyService(ApplicationDbContext context, ILogger<KeyService> logger)
    {
        _context = context;
        _logger = logger;
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
        try
        {
            var existingKeys = await _context.UserKeys
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
                _context.UserKeys.Add(userKeys);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Keys saved for userId: {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving keys for userId: {userId}");
            throw;
        }
    }

    public async Task<string> GetPublicKeyAsync(int userId)
    {
        try
        {
            var userKeys = await _context.UserKeys
                .FirstOrDefaultAsync(k => k.UserId == userId);

            if (userKeys == null)
            {
                _logger.LogWarning($"Public key not found for userId: {userId}");
                throw new Exception($"Public key not found for user: {userId}");
            }

            return userKeys.PublicKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving public key for userId: {userId}");
            throw;
        }
    }

    public async Task<string> GetPrivateKeyAsync(int userId)
    {
        try
        {
            var userKeys = await _context.UserKeys
                .FirstOrDefaultAsync(k => k.UserId == userId);

            if (userKeys == null)
            {
                _logger.LogWarning($"Private key not found for userId: {userId}");
                throw new Exception($"Private key not found for user: {userId}");
            }

            return userKeys.PrivateKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving private key for userId: {userId}");
            throw;
        }
    }
}