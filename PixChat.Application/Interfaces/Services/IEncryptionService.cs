namespace PixChat.Application.Interfaces.Services;

public interface IEncryptionService
{
    Task<(byte[] encryptedData, byte[] key, byte[] iv)> EncryptDataAsync(byte[] data);
    Task<byte[]> DecryptDataAsync(byte[] encryptedData, byte[] key, byte[] iv);
    string EncryptAESKeyWithRSA(byte[] aesKey, string publicKey);
    byte[] DecryptAESKeyWithRSA(string encryptedAESKey, string privateKey);
}