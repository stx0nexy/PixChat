namespace PixChat.Application.DTOs;

public class DecryptAesKeyRequest
{
    public string EncryptedAESKey { get; set; }
    public string PrivateKey { get; set; }
}