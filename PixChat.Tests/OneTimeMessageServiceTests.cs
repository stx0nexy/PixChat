using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text;

namespace PixChat.Tests;

public class OneTimeMessageServiceTests
{
    private readonly Mock<ILogger<OneTimeMessageService>> _mockLogger;
    private readonly Mock<IOneTimeMessageRepository> _mockOneTimeMessageRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly OneTimeMessageService _oneTimeMessageService;

    public OneTimeMessageServiceTests()
    {
        _mockLogger = new Mock<ILogger<OneTimeMessageService>>();
        _mockOneTimeMessageRepository = new Mock<IOneTimeMessageRepository>();
        _mockMapper = new Mock<IMapper>();
        _oneTimeMessageService = new OneTimeMessageService(_mockLogger.Object, _mockOneTimeMessageRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetMessageByIdAsync_MessageExists_ReturnsMessage()
    {
        // Arrange
        var messageId = "msg123";
        var messageEntity = new OneTimeMessage {
            Id = messageId,
            SenderId = "senderId",
            ReceiverId = "receiverId",
            ChatId = 1,
            StegoImage = new byte[] { 1, 2, 3 },
            EncryptionKey = "key",
            MessageLength = 50,
            CreatedAt = DateTime.UtcNow,
            Received = true,
            Read = false
        };
       
        var messageDto = new OneTimeMessageDto {
            Id = messageId,
            SenderId = "senderId",
            ReceiverId = "receiverId",
            ChatId = 1,
            StegoImage = new byte[] { 1, 2, 3 },
            EncryptionKey = "key",
            MessageLength = 50,
            CreatedAt = DateTime.UtcNow,
            Received = true,
            Read = false
        };

        _mockOneTimeMessageRepository.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(messageEntity);
        _mockMapper.Setup(m => m.Map<OneTimeMessageDto>(messageEntity)).Returns(messageDto);

        // Act
        var result = await _oneTimeMessageService.GetMessageByIdAsync(messageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(messageDto.Id, result.Id);
        Assert.Equal(messageDto.StegoImage, result.StegoImage);
        Assert.Equal(messageDto.EncryptionKey, result.EncryptionKey);
        Assert.Equal(messageDto.MessageLength, result.MessageLength);

        _mockOneTimeMessageRepository.Verify(r => r.GetByIdAsync(messageId), Times.Once);
        _mockMapper.Verify(m => m.Map<OneTimeMessageDto>(messageEntity), Times.Once);
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
    public async Task GetMessageByIdAsync_MessageDoesNotExist_ReturnsNull()
    {
        // Arrange
        var messageId = "nonExistentMsg";
        _mockOneTimeMessageRepository.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync((OneTimeMessage)null);
        _mockMapper.Setup(m => m.Map<OneTimeMessageDto>(It.Is<OneTimeMessage>(e => e == null))).Returns((OneTimeMessageDto)null);

        // Act
        var result = await _oneTimeMessageService.GetMessageByIdAsync(messageId);

        // Assert
        Assert.Null(result);
        _mockOneTimeMessageRepository.Verify(r => r.GetByIdAsync(messageId), Times.Once);
        _mockMapper.Verify(m => m.Map<OneTimeMessageDto>(It.Is<OneTimeMessage>(e => e == null)), Times.Once);
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
    public async Task GetMessagesByReceiverIdAsync_ReturnsMessages()
    {
        // Arrange
        var receiverId = "receiver1";
        var messageEntities = new List<OneTimeMessage>
        {
            new OneTimeMessage { Id = "otm1", ReceiverId = receiverId, StegoImage = new byte[] { 1 }, EncryptionKey = "k1", MessageLength = 10, Received = false, Read = false },
            new OneTimeMessage { Id = "otm2", ReceiverId = receiverId, StegoImage = new byte[] { 2 }, EncryptionKey = "k2", MessageLength = 20, Received = false, Read = false }
        };
        var messageDtos = new List<OneTimeMessageDto>
        {
            new OneTimeMessageDto { Id = "otm1", ReceiverId = receiverId, StegoImage = new byte[] { 1 }, EncryptionKey = "k1", MessageLength = 10, Received = false, Read = false },
            new OneTimeMessageDto { Id = "otm2", ReceiverId = receiverId, StegoImage = new byte[] { 2 }, EncryptionKey = "k2", MessageLength = 20, Received = false, Read = false }
        };

        _mockOneTimeMessageRepository.Setup(r => r.GetByReceiverIdAsync(receiverId)).ReturnsAsync(messageEntities);
        _mockMapper.Setup(m => m.Map<OneTimeMessageDto>(messageEntities[0])).Returns(messageDtos[0]);
        _mockMapper.Setup(m => m.Map<OneTimeMessageDto>(messageEntities[1])).Returns(messageDtos[1]);


        // Act
        var result = await _oneTimeMessageService.GetMessagesByReceiverIdAsync(receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(messageDtos.Count, result.Count());
        Assert.Contains(messageDtos[0], result);
        Assert.Contains(messageDtos[1], result);

        _mockOneTimeMessageRepository.Verify(r => r.GetByReceiverIdAsync(receiverId), Times.Once);
        _mockMapper.Verify(m => m.Map<OneTimeMessageDto>(It.IsAny<OneTimeMessage>()), Times.Exactly(messageEntities.Count));
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
    public async Task SendMessageAsync_SendsMessage()
    {
        // Arrange
        var senderId = "sender1";
        var receiverId = "receiver1";
        var chatId = 123;
        var stegoImage = Encoding.UTF8.GetBytes("stegoImageData");
        var encryptionKey = "encryptedKey";
        var messageLength = 10;
        var createdAt = DateTime.UtcNow;
        var received = false;
        var expectedMessageId = "newMsgId";

        _mockOneTimeMessageRepository.Setup(r => r.AddAsync(It.IsAny<OneTimeMessage>())).ReturnsAsync(expectedMessageId);

        // Act
        var messageId = await _oneTimeMessageService.SendMessageAsync(
            senderId, receiverId, chatId, stegoImage, encryptionKey, messageLength, createdAt, received);

        // Assert
        Assert.Equal(expectedMessageId, messageId);
        _mockOneTimeMessageRepository.Verify(r => r.AddAsync(It.Is<OneTimeMessage>(m =>
            m.SenderId == senderId &&
            m.ReceiverId == receiverId &&
            m.ChatId == chatId &&
            m.StegoImage == stegoImage &&
            m.EncryptionKey == encryptionKey &&
            m.MessageLength == messageLength &&
            m.CreatedAt == createdAt &&
            m.Received == received &&
            m.Read == false
        )), Times.Once);
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
    public async Task DeleteMessageAsync_DeletesMessage()
    {
        // Arrange
        var messageId = "msgToDelete";
        _mockOneTimeMessageRepository.Setup(r => r.DeleteAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _oneTimeMessageService.DeleteMessageAsync(messageId);

        // Assert
        _mockOneTimeMessageRepository.Verify(r => r.DeleteAsync(messageId), Times.Once);
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
    public async Task MarkOneTimeMessageAsReceivedAsync_MarksMessageAsReceived()
    {
        // Arrange
        var messageId = "msgToMark";
        _mockOneTimeMessageRepository.Setup(r => r.MarkOneTimeMessageAsReceivedAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _oneTimeMessageService.MarkOneTimeMessageAsReceivedAsync(messageId);

        // Assert
        _mockOneTimeMessageRepository.Verify(r => r.MarkOneTimeMessageAsReceivedAsync(messageId), Times.Once);
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