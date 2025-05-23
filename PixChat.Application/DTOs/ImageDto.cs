namespace PixChat.Application.DTOs;

public class ImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; }
    public int OwnerId { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsActive { get; set; }
    public UserDto Owner { get; set; }
}