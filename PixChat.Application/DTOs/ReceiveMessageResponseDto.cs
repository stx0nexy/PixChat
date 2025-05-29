namespace PixChat.Application.DTOs;

public class ReceiveMessageResponseDto
{
    public byte[] message { get; set; }
    public int messageLength { get; set; }
    public DateTime timestamp { get; set; }
    public string encryptedAesKey { get; set; }
    public byte[] aesIv { get; set; }
}