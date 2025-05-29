namespace PixChat.Application.DTOs;

public class DecryptDataRequest
{
    public string EncryptedData { get; set; }
    public string Key { get; set; }
    public string IV { get; set; }
}