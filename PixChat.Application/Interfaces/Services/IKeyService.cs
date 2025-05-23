namespace PixChat.Application.Interfaces.Services;

public interface IKeyService
{
    Task<(string publicKey, string privateKey)> GenerateKeyPairAsync();
    Task SaveKeysAsync(int userId, string publicKey, string privateKey);
    Task<string> GetPublicKeyAsync(int userId);
    Task<string> GetPrivateKeyAsync(int userId);
}