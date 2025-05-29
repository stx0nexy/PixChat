using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Core.Exceptions;
using AutoMapper;

namespace PixChat.Tests;

public class KeyServiceTests
{
    private readonly Mock<ILogger<KeyService>> _mockLogger;
    private readonly Mock<IUserKeyRepository> _mockUserKeyRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly KeyService _keyService;

    public KeyServiceTests()
    {
        _mockLogger = new Mock<ILogger<KeyService>>();
        _mockUserKeyRepository = new Mock<IUserKeyRepository>();
        _mockMapper = new Mock<IMapper>();
        _keyService = new KeyService(_mockLogger.Object, _mockUserKeyRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GenerateKeyPairAsync_ReturnsNewKeyPair()
    {
        // Arrange
        var expectedPublicKey = "test_public_key";
        var expectedPrivateKey = "test_private_key";
        _mockUserKeyRepository.Setup(r => r.GenerateKeyPairAsync()).ReturnsAsync((expectedPublicKey, expectedPrivateKey));

        // Act
        var (publicKey, privateKey) = await _keyService.GenerateKeyPairAsync();

        // Assert
        Assert.Equal(expectedPublicKey, publicKey);
        Assert.Equal(expectedPrivateKey, privateKey);
        _mockUserKeyRepository.Verify(r => r.GenerateKeyPairAsync(), Times.Once);
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
    public async Task SaveKeysAsync_SavesKeys()
    {
        // Arrange
        var userId = 1;
        var publicKey = "test_public_key";
        var privateKey = "test_private_key";
        _mockUserKeyRepository.Setup(r => r.SaveKeysAsync(userId, publicKey, privateKey)).Returns(Task.CompletedTask);

        // Act
        await _keyService.SaveKeysAsync(userId, publicKey, privateKey);

        // Assert
        _mockUserKeyRepository.Verify(r => r.SaveKeysAsync(userId, publicKey, privateKey), Times.Once);
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
    public async Task GetPublicKeyAsync_KeyExists_ReturnsPublicKey()
    {
        // Arrange
        var userId = 1;
        var expectedPublicKey = "existing_public_key";
        _mockUserKeyRepository.Setup(r => r.GetPublicKeyAsync(userId)).ReturnsAsync(expectedPublicKey);

        // Act
        var publicKey = await _keyService.GetPublicKeyAsync(userId);

        // Assert
        Assert.Equal(expectedPublicKey, publicKey);
        _mockUserKeyRepository.Verify(r => r.GetPublicKeyAsync(userId), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
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
    public async Task GetPublicKeyAsync_KeyDoesNotExist_ThrowsBusinessException()
    {
        // Arrange
        var userId = 1;
        _mockUserKeyRepository.Setup(r => r.GetPublicKeyAsync(userId)).ReturnsAsync((string)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _keyService.GetPublicKeyAsync(userId));
        Assert.Equal($"Public key not found for user: {userId}", exception.Message);

        _mockUserKeyRepository.Verify(r => r.GetPublicKeyAsync(userId), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
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
    public async Task GetPrivateKeyAsync_KeyExists_ReturnsPrivateKey()
    {
        // Arrange
        var userId = 1;
        var expectedPrivateKey = "existing_private_key";
        _mockUserKeyRepository.Setup(r => r.GetPrivateKeyAsync(userId)).ReturnsAsync(expectedPrivateKey);

        // Act
        var privateKey = await _keyService.GetPrivateKeyAsync(userId);

        // Assert
        Assert.Equal(expectedPrivateKey, privateKey);
        _mockUserKeyRepository.Verify(r => r.GetPrivateKeyAsync(userId), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
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
    public async Task GetPrivateKeyAsync_KeyDoesNotExist_ThrowsBusinessException()
    {
        // Arrange
        var userId = 1;
        _mockUserKeyRepository.Setup(r => r.GetPrivateKeyAsync(userId)).ReturnsAsync((string)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _keyService.GetPrivateKeyAsync(userId));
        Assert.Equal($"Private key not found for user: {userId}", exception.Message);

        _mockUserKeyRepository.Verify(r => r.GetPrivateKeyAsync(userId), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
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