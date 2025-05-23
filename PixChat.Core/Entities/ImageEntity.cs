namespace PixChat.Core.Entities;

public class ImageEntity
{
    public int Id { get; set; }
    public string PictureFileName { get; set; }
    public int OwnerId { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsActive { get; set; }
    public UserEntity Owner { get; set; }
}