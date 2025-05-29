using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using PixChat.API.Controllers;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Exceptions;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.Json;
using PixChat.Application.DTOs;
using DecryptAesKeyRequest = PixChat.API.Controllers.DecryptAesKeyRequest;
using DecryptDataRequest = PixChat.API.Controllers.DecryptDataRequest;

namespace PixChat.Tests;

public class KeysControllerTests
{
    private readonly Mock<IKeyService> _mockKeyService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly KeysController _keysController;

    public KeysControllerTests()
    {
        _mockKeyService = new Mock<IKeyService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _keysController = new KeysController(_mockKeyService.Object);
    }

    [Fact]
    public async Task GenerateKeys_ReturnsPublicAndPrivateKeys()
    {
        // Arrange
        var userId = 1;
        var publicKey = "MOCK_PUBLIC_KEY";
        var privateKey = "MOCK_PRIVATE_KEY";
        _mockKeyService.Setup(s => s.GenerateKeyPairAsync()).ReturnsAsync((publicKey, privateKey));
        _mockKeyService.Setup(s => s.SaveKeysAsync(userId, publicKey, privateKey)).Returns(Task.CompletedTask);

        // Act
        var result = await _keysController.GenerateKeys(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var jsonString = JsonSerializer.Serialize(okResult.Value);
        var responseDto = JsonSerializer.Deserialize<KeysResponseDto>(jsonString);

        Assert.NotNull(responseDto);
        Assert.Equal(publicKey, responseDto.PublicKey);
        Assert.Equal(privateKey, responseDto.PrivateKey);
        _mockKeyService.Verify(s => s.GenerateKeyPairAsync(), Times.Once);
        _mockKeyService.Verify(s => s.SaveKeysAsync(userId, publicKey, privateKey), Times.Once);
    }

    [Fact]
    public async Task GetKeys_KeysExist_ReturnsKeys()
    {
        // Arrange
        var userId = 1;
        var publicKey = "MOCK_PUBLIC_KEY_EXISTING";
        var privateKey = "MOCK_PRIVATE_KEY_EXISTING";
        _mockKeyService.Setup(s => s.GetPublicKeyAsync(userId)).ReturnsAsync(publicKey);
        _mockKeyService.Setup(s => s.GetPrivateKeyAsync(userId)).ReturnsAsync(privateKey);

        // Act
        var result = await _keysController.GetKeys(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var jsonString = JsonSerializer.Serialize(okResult.Value);
        var responseDto = JsonSerializer.Deserialize<KeysResponseDto>(jsonString);

        Assert.NotNull(responseDto);
        Assert.Equal(publicKey, responseDto.PublicKey);
        Assert.Equal(privateKey, responseDto.PrivateKey);
        _mockKeyService.Verify(s => s.GetPublicKeyAsync(userId), Times.Once);
        _mockKeyService.Verify(s => s.GetPrivateKeyAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetKeys_KeysDoNotExist_ReturnsBadRequest()
    {
        // Arrange
        var userId = 99;
        _mockKeyService.Setup(s => s.GetPublicKeyAsync(userId)).ThrowsAsync(new BusinessException("Public key not found."));

        // Act
        var result = await _keysController.GetKeys(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var jsonString = JsonSerializer.Serialize(badRequestResult.Value);
        var errorResponseDto = JsonSerializer.Deserialize<ErrorResponseDto>(jsonString);

        Assert.NotNull(errorResponseDto);
        Assert.Contains("Error retrieving keys", errorResponseDto.message);
        _mockKeyService.Verify(s => s.GetPublicKeyAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DecryptAesKey_ValidRequest_ReturnsDecryptedKey()
    {
        // Arrange
        using var rsa = RSA.Create();
        var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();

        var aesKey = new byte[32];
        new Random().NextBytes(aesKey);
        var encryptedAesKeyBytes = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
        var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKeyBytes);

        var request = new DecryptAesKeyRequest()
        {
            EncryptedAESKey = encryptedAesKeyBase64,
            PrivateKey = privateKeyPem
        };

        // Act
        var result = await _keysController.DecryptAesKey(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDecryptedKeyBase64 = Assert.IsType<string>(okResult.Value);
        var returnedDecryptedKeyBytes = Convert.FromBase64String(returnedDecryptedKeyBase64);

        Assert.Equal(aesKey, returnedDecryptedKeyBytes);
    }

    [Fact]
    public async Task DecryptData_ValidRequest_ReturnsDecryptedData()
    {
        // Arrange
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        var dataToEncrypt = Encoding.UTF8.GetBytes("Hello, world!");
        
        byte[] encryptedDataBytes;
        using (var ms = new MemoryStream())
        {
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                cs.FlushFinalBlock();
            }
            encryptedDataBytes = ms.ToArray();
        }

        var request = new DecryptDataRequest()
        {
            EncryptedData = Convert.ToBase64String(encryptedDataBytes),
            Key = Convert.ToBase64String(aes.Key),
            IV = Convert.ToBase64String(aes.IV)
        };

        // Act
        var result = await _keysController.DecryptData(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDecryptedData = Assert.IsType<string>(okResult.Value);
        var finalDecryptedBytes = Convert.FromBase64String(returnedDecryptedData);
        Assert.Equal(dataToEncrypt, finalDecryptedBytes);
    }

    [Fact]
    public async Task DecryptMessage_ValidRequest_ReturnsDecryptedString()
    {
        // Arrange
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        var messageToEncrypt = "This is a secret message.";
        var messageBytes = Encoding.UTF8.GetBytes(messageToEncrypt);

        byte[] encryptedMessageBytes;
        using (var ms = new MemoryStream())
        {
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(messageBytes, 0, messageBytes.Length);
                cs.FlushFinalBlock();
            }
            encryptedMessageBytes = ms.ToArray();
        }

        var request = new DecryptDataRequest()
        {
            EncryptedData = Convert.ToBase64String(encryptedMessageBytes),
            Key = Convert.ToBase64String(aes.Key),
            IV = Convert.ToBase64String(aes.IV)
        };

        // Act
        var result = await _keysController.DecryptMessage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDecryptedString = Assert.IsType<string>(okResult.Value);
        Assert.Equal(messageToEncrypt, returnedDecryptedString);
    }
}