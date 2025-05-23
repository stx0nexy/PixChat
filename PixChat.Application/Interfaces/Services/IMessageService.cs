namespace PixChat.Application.Interfaces.Services;

public interface IMessageService
{
    Task SendMessageAsync(string message, string recipientConnectionId);
    Task ReceiveMessageAsync(byte[] image);
}