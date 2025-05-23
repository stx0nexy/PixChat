using PixChat.Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PixChat.Application.Services;

public class EncryptionService : IEncryptionService
{
private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    public async Task<(byte[] encryptedData, byte[] key, byte[] iv)> EncryptDataAsync(byte[] data)
    {
        try
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            using var ms = new MemoryStream();
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                await cs.WriteAsync(data, 0, data.Length);
                await cs.FlushFinalBlockAsync();
            }
            return (ms.ToArray(), aes.Key, aes.IV);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data with AES.");
            throw;
        }
    }

    public async Task<byte[]> DecryptDataAsync(byte[] encryptedData, byte[] key, byte[] iv)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream();
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                await cs.WriteAsync(encryptedData, 0, encryptedData.Length);
                await cs.FlushFinalBlockAsync();
            }
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data with AES.");
            throw;
        }
    }

    public string EncryptAESKeyWithRSA(byte[] aesKey, string publicKey)
    {
        try
        {
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentNullException(nameof(publicKey), "Публичный ключ не может быть пустым.");
            }

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            var encryptedKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting AES key with RSA.");
            throw;
        }
    }

    public byte[] DecryptAESKeyWithRSA(string encryptedAESKey, string privateKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            var encryptedKeyBytes = Convert.FromBase64String(encryptedAESKey);
            return rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting AES key with RSA.");
            throw;
        }
    }
}