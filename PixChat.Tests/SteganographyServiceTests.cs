using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PixChat.Application.Services;
using PixChat.Application.Config;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Linq;
using System;
using System.Security.Cryptography;

namespace PixChat.Tests;

public class SteganographyServiceTests
{
    private readonly Mock<ILogger<SteganographyService>> _mockLogger;
    private readonly Mock<IOptions<ImageConfig>> _mockImageConfig;
    private readonly SteganographyService _steganographyService;

    private const string EndMarker = "|X7K9P2M|";

    public SteganographyServiceTests()
    {
        _mockLogger = new Mock<ILogger<SteganographyService>>();
        _mockImageConfig = new Mock<IOptions<ImageConfig>>();
        _mockImageConfig.Setup(o => o.Value).Returns(new ImageConfig { ImageFolderPath = "TestImages" });

        _steganographyService = new SteganographyService(_mockLogger.Object, _mockImageConfig.Object);
    }

    private byte[] CreateTestImageBytes(int width, int height)
    {
        using (var bitmap = new Bitmap(width, height))
        using (var ms = new MemoryStream())
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.FillRectangle(Brushes.White, 0, 0, width, height);
            }
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    private string GenerateTestKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random(0);
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    [Fact]
    public void EmbedMessage_EmbedsMessageCorrectly()
    {
        // Arrange
        var width = 20;
        var height = 10;
        var originalImageBytes = CreateTestImageBytes(width, height);
        var originalMessage = "Hello World! This is a secret message.";
        var key = GenerateTestKey(10);

        // Act
        var embeddedImageBytes = _steganographyService.EmbedMessage(originalImageBytes, originalMessage, key);

        // Assert
        Assert.NotNull(embeddedImageBytes);
        Assert.True(embeddedImageBytes.Length > originalImageBytes.Length);
        
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public void EmbedMessage_ThrowsArgumentException_IfMessageTooLarge()
    {
        // Arrange
        var width = 10;
        var height = 10;
        var originalImageBytes = CreateTestImageBytes(width, height);
        var largeMessage = new string('A', 1000);
        var key = GenerateTestKey(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _steganographyService.EmbedMessage(originalImageBytes, largeMessage, key));

        Assert.Contains("Message is too large to embed.", exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public void ExtractFullMessage_ExtractsMessageCorrectly()
    {
        // Arrange
        var width = 50;
        var height = 50;
        var originalImageBytes = CreateTestImageBytes(width, height);

        var testMessageContent = Encoding.UTF8.GetBytes("Super secret message");
        var testEncryptionKey = "test_enc_key"; 
        var testMessageLength = testMessageContent.Length;
        var testTimestamp = DateTime.UtcNow;
        var testEncryptedAESKey = "EncryptedAESKeyBase64";
        var testAesIV = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

        string dateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        var fullMessageToEmbed = $"{testMessageLength}|{Convert.ToBase64String(testMessageContent)}|{testTimestamp.ToString(dateTimeFormat)}|{testEncryptedAESKey}|{testAesIV}";
        var key = GenerateTestKey(15);

        var embeddedImageBytes = _steganographyService.EmbedMessage(originalImageBytes, fullMessageToEmbed + EndMarker, key);

        // Act
        var (extractedMessageBytes, extractedEncryptionKeyFromService, extractedMessageLength, extractedTimestamp, extractedEncryptedAESKey, extractedAesIV) = _steganographyService.ExtractFullMessage(embeddedImageBytes, key);

        // Assert
        Assert.NotNull(extractedMessageBytes);
        Assert.Equal(testMessageContent, extractedMessageBytes);
        
        Assert.Null(extractedEncryptionKeyFromService); 

        Assert.Equal(testMessageLength, extractedMessageLength);

        Assert.True(Math.Abs((extractedTimestamp - testTimestamp).TotalMilliseconds) < 1,
                    $"Timestamp mismatch. Expected: {testTimestamp.ToString(dateTimeFormat)}, Actual: {extractedTimestamp.ToString(dateTimeFormat)}");

        Assert.Equal(testEncryptedAESKey, extractedEncryptedAESKey);
        Assert.Equal(Convert.FromBase64String(testAesIV), extractedAesIV);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public void ExtractFullMessage_ThrowsFormatException_IfEndMarkerNotFound()
    {
        // Arrange
        var width = 10;
        var height = 10;
        var originalImageBytes = CreateTestImageBytes(width, height);
        var messageWithoutEndMarker = "TooShort";
        var key = GenerateTestKey(10);

        var embeddedImageBytes = _steganographyService.EmbedMessage(originalImageBytes, messageWithoutEndMarker, key);

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() =>
            _steganographyService.ExtractFullMessage(embeddedImageBytes, key));

        Assert.Contains("End marker not found in image data.", exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex != null && ex.Message.Contains("End marker not found in image data.")),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void ExtractFullMessage_ThrowsFormatException_IfInvalidDataFormatAfterExtraction()
    {
        // Arrange
        var width = 50;
        var height = 50;
        var originalImageBytes = CreateTestImageBytes(width, height);
        var invalidFormattedMessage = "10|Base64Msg|Timestamp" + EndMarker;
        var key = GenerateTestKey(15);

        var embeddedImageBytes = _steganographyService.EmbedMessage(originalImageBytes, invalidFormattedMessage, key);

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() =>
            _steganographyService.ExtractFullMessage(embeddedImageBytes, key));

        Assert.Contains("Invalid steganography data format. Expected 5 parts, got", exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex != null && ex.Message.Contains("Invalid steganography data format.")),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void ExtractFullMessage_ThrowsFormatException_IfInvalidFormat()
    {
        // Arrange
        var width = 50;
        var height = 50;
        var originalImageBytes = CreateTestImageBytes(width, height);
        var invalidFormattedMessage = "10|Base64Msg|Timestamp" + EndMarker;
        var key = GenerateTestKey(15);

        var embeddedImageBytes = _steganographyService.EmbedMessage(originalImageBytes, invalidFormattedMessage, key);

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() =>
            _steganographyService.ExtractFullMessage(embeddedImageBytes, key));

        Assert.StartsWith("Invalid steganography data format. Expected 5 parts, got", exception.Message); 
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }


    [Fact]
    public void GetRandomImage_ReturnsImageBytes()
    {
        // Arrange
        var testImagePath = Path.Combine(_mockImageConfig.Object.Value.ImageFolderPath, "test_image.png");
        var testImageContent = new byte[] { 1, 2, 3, 4, 5 };

        Directory.CreateDirectory(_mockImageConfig.Object.Value.ImageFolderPath);
        File.WriteAllBytes(testImagePath, testImageContent);

        // Act
        var result = _steganographyService.GetRandomImage();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(testImageContent, result);

        File.Delete(testImagePath);
        Directory.Delete(_mockImageConfig.Object.Value.ImageFolderPath);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public void GetRandomImage_ThrowsFileNotFoundException_IfNoImagesFound()
    {
        // Arrange
        var emptyFolderPath = "EmptyTestImages";
        _mockImageConfig.Setup(o => o.Value).Returns(new ImageConfig { ImageFolderPath = emptyFolderPath });
        Directory.CreateDirectory(emptyFolderPath);

        var serviceWithEmptyFolder = new SteganographyService(_mockLogger.Object, _mockImageConfig.Object);

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() =>
            serviceWithEmptyFolder.GetRandomImage());

        Assert.Equal("No images found in the folder.", exception.Message);

        Directory.Delete(emptyFolderPath);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }
}