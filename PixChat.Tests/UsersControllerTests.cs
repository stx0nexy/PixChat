using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PixChat.API.Controllers;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;
using PixChat.Core.Entities;
using PixChat.Core.Exceptions;
using System.IO;
using System.Text.Json;

namespace PixChat.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<AutoMapper.IMapper> _mockMapper;
    private readonly Mock<ISteganographyService> _mockSteganographyService;
    private readonly Mock<IKeyService> _mockKeyService;
    private readonly Mock<IOfflineMessageService> _mockOfflineMessageService;
    private readonly Mock<IOneTimeMessageService> _mockOneTimeMessageService;
    private readonly UsersController _usersController;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockMapper = new Mock<AutoMapper.IMapper>();
        _mockSteganographyService = new Mock<ISteganographyService>();
        _mockKeyService = new Mock<IKeyService>();
        _mockOfflineMessageService = new Mock<IOfflineMessageService>();
        _mockOneTimeMessageService = new Mock<IOneTimeMessageService>();

        _usersController = new UsersController(
            _mockUserService.Object,
            _mockMapper.Object,
            _mockSteganographyService.Object,
            _mockKeyService.Object,
            _mockOfflineMessageService.Object,
            _mockOneTimeMessageService.Object
        );
    }

    [Fact]
    public async Task GetUserById_UserExists_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var userDto = new UserDto { Id = userId, Username = "TestUser" };
        _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(userDto);

        // Act
        var result = await _usersController.GetUserById(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, returnedUser.Id);
        _mockUserService.Verify(s => s.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserById_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = 99;
        _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync((UserDto)null);

        // Act
        var result = await _usersController.GetUserById(userId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockUserService.Verify(s => s.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmail_UserExists_ReturnsUser()
    {
        // Arrange
        var userEmail = "test@example.com";
        var userDto = new UserDto { Email = userEmail, Username = "TestUser" };
        _mockUserService.Setup(s => s.GetByEmailAsync(userEmail)).ReturnsAsync(userDto);

        // Act
        var result = await _usersController.GetUserByEmail(userEmail);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userEmail, returnedUser.Email);
        _mockUserService.Verify(s => s.GetByEmailAsync(userEmail), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmail_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userEmail = "nonexistent@example.com";
        _mockUserService.Setup(s => s.GetByEmailAsync(userEmail)).ReturnsAsync((UserDto)null);

        // Act
        var result = await _usersController.GetUserByEmail(userEmail);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockUserService.Verify(s => s.GetByEmailAsync(userEmail), Times.Once);
    }

    [Fact]
    public async Task AddUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userDto = new UserDto { Email = "new@example.com", Username = "newuser" };
        _mockUserService.Setup(s => s.AddAsync(userDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.AddUser(userDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserService.Verify(s => s.AddAsync(userDto), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var userDto = new UserDto { Id = userId, Email = "updated@example.com" };
        _mockUserService.Setup(s => s.UpdateAsync(userDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.UpdateUser(userId, userDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserService.Verify(s => s.UpdateAsync(userDto), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_UserExists_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        _mockUserService.Setup(s => s.DeleteAsync(userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.DeleteUser(userId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserService.Verify(s => s.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_UserDoesNotExist_ReturnsOk()
    {
        // Arrange
        var userId = 99;
        _mockUserService.Setup(s => s.DeleteAsync(userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.DeleteUser(userId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserService.Verify(s => s.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ReceiveMessage_ValidRequest_ReturnsDecryptedMessage()
    {
        // Arrange
        var userId = 1;
        var base64Image = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var encryptedKey = "mock_encrypted_key";
        var decryptedMessageBytes = new byte[] { 72, 101, 108, 108, 111 }; // "Hello"
        var messageLength = 5;
        var timestamp = DateTime.UtcNow;
        var encryptedAesKey = "mock_encrypted_aes_key";
        var aesIv = new byte[] { 1, 2, 3 };

        _mockSteganographyService.Setup(s => s.ExtractFullMessage(
                It.IsAny<byte[]>(),
                It.IsAny<string>()))
            .Returns((decryptedMessageBytes, null, messageLength, timestamp, encryptedAesKey, aesIv));

        var request = new MessageRequest
        {
            Base64Image = base64Image,
            EncryptedKey = encryptedKey
        };

        // Act
        var result = await _usersController.ReceiveMessage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var jsonString = JsonSerializer.Serialize(okResult.Value);
        var responseDto = JsonSerializer.Deserialize<ReceiveMessageResponseDto>(jsonString);

        Assert.NotNull(responseDto);
        Assert.Equal(decryptedMessageBytes, responseDto.message);
        Assert.Equal(messageLength, responseDto.messageLength);
        Assert.Equal(timestamp, responseDto.timestamp);
        Assert.Equal(encryptedAesKey, responseDto.encryptedAesKey);
        Assert.Equal(aesIv, responseDto.aesIv);

        _mockSteganographyService.Verify(s => s.ExtractFullMessage(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
    }


    [Fact]
    public async Task UploadUserProfilePicture_ValidImage_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = 1;
        var fileName = "test.png";
        var fileContent = "dummy image data";
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.FileName).Returns(fileName);
        mockFormFile.Setup(f => f.Length).Returns(memoryStream.Length);
        mockFormFile.Setup(f => f.OpenReadStream()).Returns(memoryStream);

        var updatedUserDto = new UserDto { Id = userId, ProfilePictureUrl = "http://cdn.com/assets/images/new_image.png" };
        _mockUserService.Setup(s => s.UploadUserProfilePictureAsync(userId, It.IsAny<Stream>(), fileName)).ReturnsAsync(updatedUserDto);

        // Act
        var result = await _usersController.UploadUserProfilePicture(userId, mockFormFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(updatedUserDto.Id, returnedUser.Id);
        Assert.Equal(updatedUserDto.ProfilePictureUrl, returnedUser.ProfilePictureUrl);
        _mockUserService.Verify(s => s.UploadUserProfilePictureAsync(userId, It.IsAny<Stream>(), fileName), Times.Once);
    }

    [Fact]
    public async Task ConfirmMessageReceived_ValidRequest_ReturnsOk()
    {
        // Arrange
        var messageId = "some-message-id";
        var request = new MessageConfirmRequest { MessageId = messageId };
        _mockOfflineMessageService.Setup(s => s.MarkMessageAsReceivedAsync(messageId)).Returns(Task.CompletedTask);
        _mockOfflineMessageService.Setup(s => s.DeleteMessageAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.ConfirmMessageReceived(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockOfflineMessageService.Verify(s => s.MarkMessageAsReceivedAsync(messageId), Times.Once);
        _mockOfflineMessageService.Verify(s => s.DeleteMessageAsync(messageId), Times.Once);
    }

    [Fact]
    public async Task ConfirmOneTimeMessageReceived_ValidRequest_ReturnsOk()
    {
        // Arrange
        var messageId = "some-onetime-message-id";
        var request = new MessageConfirmRequest { MessageId = messageId };
        _mockOneTimeMessageService.Setup(s => s.MarkOneTimeMessageAsReceivedAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.ConfirmOneTimeMessageReceived(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockOneTimeMessageService.Verify(s => s.MarkOneTimeMessageAsReceivedAsync(messageId), Times.Once);
    }
}