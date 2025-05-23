namespace PixChat.Application.Interfaces.Services;

public interface ISteganographyService
{

    byte[] EmbedMessage(byte[] image, string message, string key);


    (string message, string encryptionKey, int messageLength, DateTime timestamp) ExtractFullMessage(byte[] image, string key);


    byte[] GetRandomImage();
}