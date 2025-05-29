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

namespace PixChat.Tests;

public class OfflineMessageServiceTests
{
    private readonly Mock<ILogger<OfflineMessageService>> _mockLogger;
    private readonly Mock<IOfflineMessageRepository> _mockOfflineMessageRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly OfflineMessageService _offlineMessageService;

    public OfflineMessageServiceTests()
    {
        _mockLogger = new Mock<ILogger<OfflineMessageService>>();
        _mockOfflineMessageRepository = new Mock<IOfflineMessageRepository>();
        _mockMapper = new Mock<IMapper>();
        _offlineMessageService = new OfflineMessageService(_mockLogger.Object, _mockOfflineMessageRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task SaveMessageAsync_StegoImage_SavesMessage()
    {
        // Arrange
        var messageDto = new OfflineMessageDto
        {
            Id = "msg1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            ChatId = 1,
            StegoImage = new byte[] { 1, 2, 3 },
            EncryptionKey = "key123",
            MessageLength = 100,
            CreatedAt = DateTime.UtcNow,
            Received = false,
            IsGroup = false
        };
        var messageEntity = new OfflineMessageEntity
        {
            Id = "msg1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            ChatId = 1,
            StegoImage = new byte[] { 1, 2, 3 },
            EncryptionKey = "key123",
            MessageLength = 100,
            CreatedAt = DateTime.UtcNow,
            Received = false,
            IsGroup = false
        };

        _mockMapper.Setup(m => m.Map<OfflineMessageEntity>(It.IsAny<OfflineMessageDto>())).Returns(messageEntity);
        _mockOfflineMessageRepository.Setup(r => r.SaveMessageAsync(It.IsAny<OfflineMessageEntity>())).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.SaveMessageAsync(messageDto);

        // Assert
        _mockMapper.Verify(m => m.Map<OfflineMessageEntity>(messageDto), Times.Once);
        _mockOfflineMessageRepository.Verify(r => r.SaveMessageAsync(messageEntity), Times.Once);
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
    public async Task SaveMessageAsync_File_SavesFileMessage()
    {
        // Arrange
        var fileMessageDto = new OfflineMessageFileDto
        {
            Id = "fileMsg1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            ChatId = 2,
            FileName = "image.jpg",
            FileData = new byte[] { 1, 2, 3 },
            CreatedAt = DateTime.UtcNow,
            Received = false
        };
        var fileMessageEntity = new OfflineMessageFileEntity
        {
            Id = "fileMsg1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            ChatId = 2,
            FileName = "image.jpg",
            FileData = new byte[] { 1, 2, 3 },
            CreatedAt = DateTime.UtcNow,
            Received = false
        };

        _mockMapper.Setup(m => m.Map<OfflineMessageFileEntity>(It.IsAny<OfflineMessageFileDto>())).Returns(fileMessageEntity);
        _mockOfflineMessageRepository.Setup(r => r.SaveMessageAsync(It.IsAny<OfflineMessageFileEntity>())).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.SaveMessageAsync(fileMessageDto);

        // Assert
        _mockMapper.Verify(m => m.Map<OfflineMessageFileEntity>(fileMessageDto), Times.Once);
        _mockOfflineMessageRepository.Verify(r => r.SaveMessageAsync(fileMessageEntity), Times.Once);
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
    public async Task GetPendingMessagesAsync_ReturnsPendingMessages()
    {
        // Arrange
        var receiverId = "receiver1";
        var messageEntities = new List<OfflineMessageEntity>
        {
            new OfflineMessageEntity { Id = "msg1", ReceiverId = receiverId, StegoImage = new byte[] { 1 }, EncryptionKey = "k1", MessageLength = 1 },
            new OfflineMessageEntity { Id = "msg2", ReceiverId = receiverId, StegoImage = new byte[] { 2 }, EncryptionKey = "k2", MessageLength = 2 }
        };
        var messageDtos = new List<OfflineMessageDto>
        {
            new OfflineMessageDto { Id = "msg1", ReceiverId = receiverId, StegoImage = new byte[] { 1 }, EncryptionKey = "k1", MessageLength = 1 },
            new OfflineMessageDto { Id = "msg2", ReceiverId = receiverId, StegoImage = new byte[] { 2 }, EncryptionKey = "k2", MessageLength = 2 }
        };

        _mockOfflineMessageRepository.Setup(r => r.GetPendingMessagesAsync(receiverId)).ReturnsAsync(messageEntities);
        _mockMapper.Setup(m => m.Map<OfflineMessageDto>(messageEntities[0])).Returns(messageDtos[0]);
        _mockMapper.Setup(m => m.Map<OfflineMessageDto>(messageEntities[1])).Returns(messageDtos[1]);


        // Act
        var result = await _offlineMessageService.GetPendingMessagesAsync(receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(messageDtos.Count, result.Count());
        Assert.Contains(messageDtos[0], result);
        Assert.Contains(messageDtos[1], result);

        _mockOfflineMessageRepository.Verify(r => r.GetPendingMessagesAsync(receiverId), Times.Once);
        _mockMapper.Verify(m => m.Map<OfflineMessageDto>(It.IsAny<OfflineMessageEntity>()), Times.Exactly(messageEntities.Count));
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
    public async Task GetPendingFileMessagesAsync_ReturnsPendingFileMessages()
    {
        // Arrange
        var receiverId = "receiver1";
        var fileMessageEntities = new List<OfflineMessageFileEntity>
        {
            new OfflineMessageFileEntity { Id = "fileMsg1", ReceiverId = receiverId, FileName = "doc1.txt", FileData = new byte[]{1,2}},
            new OfflineMessageFileEntity { Id = "fileMsg2", ReceiverId = receiverId, FileName = "doc2.txt", FileData = new byte[]{3,4}}
        };
        var fileMessageDtos = new List<OfflineMessageFileDto>
        {
            new OfflineMessageFileDto { Id = "fileMsg1", ReceiverId = receiverId, FileName = "doc1.txt", FileData = new byte[]{1,2}},
            new OfflineMessageFileDto { Id = "fileMsg2", ReceiverId = receiverId, FileName = "doc2.txt", FileData = new byte[]{3,4}}
        };

        _mockOfflineMessageRepository.Setup(r => r.GetPendingFileMessagesAsync(receiverId)).ReturnsAsync(fileMessageEntities);
        _mockMapper.Setup(m => m.Map<OfflineMessageFileDto>(fileMessageEntities[0])).Returns(fileMessageDtos[0]);
        _mockMapper.Setup(m => m.Map<OfflineMessageFileDto>(fileMessageEntities[1])).Returns(fileMessageDtos[1]);


        // Act
        var result = await _offlineMessageService.GetPendingFileMessagesAsync(receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileMessageDtos.Count, result.Count());
        Assert.Contains(fileMessageDtos[0], result);
        Assert.Contains(fileMessageDtos[1], result);

        _mockOfflineMessageRepository.Verify(r => r.GetPendingFileMessagesAsync(receiverId), Times.Once);
        _mockMapper.Verify(m => m.Map<OfflineMessageFileDto>(It.IsAny<OfflineMessageFileEntity>()), Times.Exactly(fileMessageEntities.Count));
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
        _mockOfflineMessageRepository.Setup(r => r.DeleteMessageAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.DeleteMessageAsync(messageId);

        // Assert
        _mockOfflineMessageRepository.Verify(r => r.DeleteMessageAsync(messageId), Times.Once);
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
    public async Task MarkMessageAsReceivedAsync_MarksMessageAsReceived()
    {
        // Arrange
        var messageId = "msgToMark";
        _mockOfflineMessageRepository.Setup(r => r.MarkMessageAsReceivedAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.MarkMessageAsReceivedAsync(messageId);

        // Assert
        _mockOfflineMessageRepository.Verify(r => r.MarkMessageAsReceivedAsync(messageId), Times.Once);
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