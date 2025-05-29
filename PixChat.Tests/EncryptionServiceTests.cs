using Xunit;
using Moq;
using System.Text;
using System.Security.Cryptography;
using PixChat.Application.Services;
using Microsoft.Extensions.Logging;

namespace PixChat.Tests;

public class EncryptionServiceTests
{
    private readonly Mock<ILogger<EncryptionService>> _mockLogger;
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _mockLogger = new Mock<ILogger<EncryptionService>>();
        _encryptionService = new EncryptionService(_mockLogger.Object);
    }

    [Fact]
    public async Task EncryptDataAsync_ReturnsEncryptedDataAndKeys()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("This is some test data for encryption.");

        // Act
        var (encryptedData, key, iv) = await _encryptionService.EncryptDataAsync(originalData);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.NotEmpty(encryptedData);
        Assert.NotNull(key);
        Assert.NotEmpty(key);
        Assert.NotNull(iv);
        Assert.NotEmpty(iv);

        Assert.NotEqual(originalData, encryptedData);

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
    public async Task DecryptDataAsync_ReturnsDecryptedData()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("This is some sensitive data to be decrypted.");

        var (encryptedData, key, iv) = await _encryptionService.EncryptDataAsync(originalData);

        // Act
        var decryptedData = await _encryptionService.DecryptDataAsync(encryptedData, key, iv);

        // Assert
        Assert.NotNull(decryptedData);
        Assert.NotEmpty(decryptedData);
        Assert.Equal(originalData, decryptedData);

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
    public void EncryptAESKeyWithRSA_ReturnsEncryptedKey()
    {
        // Arrange
        var aesKey = new byte[32];
        Random.Shared.NextBytes(aesKey);

        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();

        // Act
        var encryptedAesKeyBase64 = _encryptionService.EncryptAESKeyWithRSA(aesKey, publicKeyPem);

        // Assert
        Assert.NotNull(encryptedAesKeyBase64);
        Assert.NotEmpty(encryptedAesKeyBase64);

        var encryptedKeyBytes = Convert.FromBase64String(encryptedAesKeyBase64);
        var decryptedKey = rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);
        Assert.Equal(aesKey, decryptedKey);

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
    public void DecryptAESKeyWithRSA_ReturnsDecryptedKey()
    {
        // Arrange
        var originalAesKey = new byte[32];
        Random.Shared.NextBytes(originalAesKey);

        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();

        var encryptedAesKeyBytes = rsa.Encrypt(originalAesKey, RSAEncryptionPadding.OaepSHA256);
        var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKeyBytes);

        // Act
        var decryptedAesKey = _encryptionService.DecryptAESKeyWithRSA(encryptedAesKeyBase64, privateKeyPem);

        // Assert
        Assert.NotNull(decryptedAesKey);
        Assert.NotEmpty(decryptedAesKey);
        Assert.Equal(originalAesKey, decryptedAesKey);

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
    public void EncryptAESKeyWithRSA_ThrowsArgumentNullException_IfPublicKeyIsEmpty()
    {
        // Arrange
        var aesKey = new byte[32];
        Random.Shared.NextBytes(aesKey);
        string emptyPublicKey = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _encryptionService.EncryptAESKeyWithRSA(aesKey, emptyPublicKey));
        
        Assert.Equal("The public key cannot be empty. (Parameter 'publicKey')", exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}