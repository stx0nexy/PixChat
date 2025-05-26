using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using PixChat.Application.Interfaces.Services;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PixChat.Application.Services;

public class SteganographyService : ISteganographyService
{
    private readonly ILogger<SteganographyService> _logger;
    private static readonly string ImageFolderPath = @"C:\Users\nykol\RiderProjects\PixChat\PixChat.API\Proxy\assets\images";
    private const string EndMarker = "|X7K9P2M|";

    public SteganographyService(ILogger<SteganographyService> logger)
    {
        _logger = logger;
    }

    public byte[] EmbedMessage(byte[] image, string fullMessage, string key)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage);
        string messageBits = string.Join("", messageBytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

        using (var memoryStream = new MemoryStream(image))
        using (var bitmap = new Bitmap(memoryStream))
        {
            int availableBits = bitmap.Width * bitmap.Height * 3; 
            if (messageBits.Length > availableBits)
            {
                throw new ArgumentException($"Message is too large to embed. Message bits: {messageBits.Length}, Available bits: {availableBits}");
            }

            var pixelIndices = GenerateRandomPixelIndices(bitmap.Width, bitmap.Height, messageBits.Length, key);

            int bitIndex = 0;
            foreach (var (x, y, channel) in pixelIndices)
            {
                Color pixel = bitmap.GetPixel(x, y);
                int r = pixel.R;
                int g = pixel.G;
                int b = pixel.B;

                if (channel == 0) // R
                    r = (r & 0xFE) | (messageBits[bitIndex] == '1' ? 1 : 0);
                else if (channel == 1) // G
                    g = (g & 0xFE) | (messageBits[bitIndex] == '1' ? 1 : 0);
                else // B
                    b = (b & 0xFE) | (messageBits[bitIndex] == '1' ? 1 : 0);

                bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                bitIndex++;
            }

            using (var outputStream = new MemoryStream())
            {
                bitmap.Save(outputStream, ImageFormat.Png);
                return outputStream.ToArray();
            }
        }
    }

    public (byte[] message, string encryptionKey, int messageLength, DateTime timestamp, string encryptedAESKey, byte[] aesIV) ExtractFullMessage(byte[] image, string key)
    {
        using (var memoryStream = new MemoryStream(image))
        using (var bitmap = new Bitmap(memoryStream))
        {
            int totalBits = bitmap.Width * bitmap.Height * 3;
            var pixelIndices = GenerateRandomPixelIndices(bitmap.Width, bitmap.Height, totalBits, key);

            StringBuilder messageBits = new StringBuilder();
            byte[] markerBytes = Encoding.UTF8.GetBytes(EndMarker);
            int markerBitsLength = markerBytes.Length * 8;
            int bitsRead = 0;

            foreach (var (x, y, channel) in pixelIndices)
            {
                Color pixel = bitmap.GetPixel(x, y);
                int bit = channel == 0 ? pixel.R & 1 : channel == 1 ? pixel.G & 1 : pixel.B & 1;
                messageBits.Append(bit);
                bitsRead++;

                if (bitsRead >= markerBitsLength)
                {
                    string currentBits = messageBits.ToString(bitsRead - markerBitsLength, markerBitsLength);
                    List<byte> currentBytes = new List<byte>();
                    for (int i = 0; i < currentBits.Length; i += 8)
                    {
                        if (i + 8 > currentBits.Length) break;
                        string byteString = currentBits.Substring(i, 8);
                        currentBytes.Add(Convert.ToByte(byteString, 2));
                    }
                    string currentString = Encoding.UTF8.GetString(currentBytes.ToArray());
                    if (currentString == EndMarker)
                    {
                        break;
                    }
                }
            }

            if (bitsRead < markerBitsLength)
            {
                _logger.LogError("End marker not found in image data.");
                throw new FormatException("End marker not found in image data.");
            }

            List<byte> messageBytes = new List<byte>();
            for (int i = 0; i < messageBits.Length - markerBitsLength; i += 8)
            {
                if (i + 8 > messageBits.Length - markerBitsLength) break;
                string byteString = messageBits.ToString(i, 8);
                messageBytes.Add(Convert.ToByte(byteString, 2));
            }

            string fullMessage = Encoding.UTF8.GetString(messageBytes.ToArray());
            _logger.LogInformation($"Extracted full message: {fullMessage}");
            string[] parts = fullMessage.Split('|');

            if (parts.Length != 5)
            {
                _logger.LogError($"Invalid steganography data format. Expected 5 parts, got {parts.Length}. Full message: '{fullMessage}'");
                throw new FormatException($"Invalid steganography data format. Expected 3 parts, got {parts.Length}.");
            }

            
            int messageLength = int.Parse(parts[0]);
            byte[] message = Convert.FromBase64String(parts[1]);
            DateTime timestamp;
            string encryptedAESKey = parts[3];
            byte[] aesIV = Convert.FromBase64String(parts[4]);
            
            try
            {
                timestamp = DateTime.Parse(parts[2]);
            }
            catch (FormatException ex)
            {
                _logger.LogError($"Failed to parse timestamp: '{parts[2]}'. Full message: '{fullMessage}'", ex);
                throw;
            }

            if (messageLength != message.Length)
            {
                _logger.LogWarning($"Message length mismatch: expected {messageLength}, got {message.Length}");
            }

            return (message, null, messageLength, timestamp, encryptedAESKey, aesIV);
        }
    }

    public byte[] GetRandomImage()
    {
        var files = Directory.GetFiles(ImageFolderPath, "*.png");
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No images found in the folder.");
        }
        return File.ReadAllBytes(files[new Random().Next(files.Length)]);
    }

    private List<(int x, int y, int channel)> GenerateRandomPixelIndices(int width, int height, int messageBitLength, string key)
    {
        var random = new Random(GenerateSeedFromKey(key));
        HashSet<(int x, int y, int channel)> indices = new HashSet<(int x, int y, int channel)>();
        int totalAvailableBits = width * height * 3;

        if (messageBitLength > totalAvailableBits)
        {
            throw new ArgumentException($"Message bits ({messageBitLength}) exceed available pixel bits ({totalAvailableBits}).");
        }

        while (indices.Count < messageBitLength)
        {
            int x = random.Next(0, width);
            int y = random.Next(0, height);
            int channel = random.Next(0, 3);
            indices.Add((x, y, channel));
        }

        return indices.ToList();
    }

    private int GenerateSeedFromKey(string key)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }
}