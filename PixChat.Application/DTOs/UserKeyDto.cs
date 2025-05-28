namespace PixChat.Application.DTOs;

public class UserKeyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
}