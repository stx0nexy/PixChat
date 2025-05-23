namespace PixChat.Application.DTOs;

public class MessageMetadataDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public DateTime SentAt { get; set; }
    public string MessageStatus { get; set; }
    public UserDto Sender { get; set; }
    public UserDto Receiver { get; set; }
}