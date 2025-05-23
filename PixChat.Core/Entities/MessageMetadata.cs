namespace PixChat.Core.Entities;

public class MessageMetadata
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public DateTime SentAt { get; set; }
    public string MessageStatus { get; set; }
    public UserEntity Sender { get; set; }
    public UserEntity Receiver { get; set; }
}