using Moq;
using Microsoft.Extensions.Logging;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;
using AutoMapper;
using PixChat.Core.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Data;
using PixChat.Application.Interfaces.Services;

namespace PixChat.Tests;

public class UserServiceTests
{
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IKeyService> _mockKeyService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockKeyService = new Mock<IKeyService>();
        _mockMapper = new Mock<IMapper>();
        _userService = new UserService(_mockLogger.Object, _mockUserRepository.Object, _mockKeyService.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var userEntity = new UserEntity { Id = userId, Username = "testuser" };
        var userDto = new UserDto { Id = userId, Username = "testuser" };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(userEntity);
        _mockMapper.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userDto.Id, result.Id);
        Assert.Equal(userDto.Username, result.Username);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(userEntity), Times.Once);
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
    public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserEntity)null);
        _mockMapper.Setup(m => m.Map<UserDto>(It.Is<UserEntity>(u => u == null))).Returns((UserDto)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.Null(result);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(It.Is<UserEntity>(u => u == null)), Times.Once);
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
    public async Task GetByUsernameAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var username = "existinguser";
        var userEntity = new UserEntity { Id = 1, Username = username, Email = "test@example.com" };
        var userDto = new UserDto { Id = 1, Username = username, Email = "test@example.com" };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(userEntity);
        _mockMapper.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);

        // Act
        var result = await _userService.GetByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userDto.Username, result.Username);
        _mockUserRepository.Verify(r => r.GetByUsernameAsync(username), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(userEntity), Times.Once);
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
    public async Task GetByEmailAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((UserEntity)null);
        _mockMapper.Setup(m => m.Map<UserDto>(It.Is<UserEntity>(u => u == null))).Returns((UserDto)null);

        // Act
        var result = await _userService.GetByEmailAsync(email);

        // Assert
        Assert.Null(result);
        _mockUserRepository.Verify(r => r.GetByEmailAsync(email), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(It.Is<UserEntity>(u => u == null)), Times.Once);
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
    public async Task UserExistsByEmailAsync_UserExists_ReturnsTrue()
    {
        // Arrange
        var email = "existing@example.com";
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(new UserEntity { Id = 1, Email = email });

        // Act
        var result = await _userService.UserExistsByEmailAsync(email);

        // Assert
        Assert.True(result);
        _mockUserRepository.Verify(r => r.GetByEmailAsync(email), Times.Once);
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
    public async Task UserExistsByEmailAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((UserEntity)null);

        // Act
        var result = await _userService.UserExistsByEmailAsync(email);

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(r => r.GetByEmailAsync(email), Times.Once);
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
    public async Task AddAsync_ValidUser_AddsUserAndGeneratesKeys()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 0,
            Username = "newuser",
            Email = "new@example.com",
            ProfilePictureUrl = null
        };
        var userEntityAfterMapping = new UserEntity
        {
            Id = 0,
            Username = "newuser",
            Email = "new@example.com",
            ProfilePictureFileName = null
        };
        var userEntityAfterAdd = new UserEntity
        {
            Id = 100,
            Username = "newuser",
            Email = "new@example.com",
            ProfilePictureFileName = "default_profile_picture.png"
        };
        var publicKey = "PUBLIC_KEY_GENERATED";
        var privateKey = "PRIVATE_KEY_GENERATED";

        _mockMapper.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntityAfterMapping);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<UserEntity>()))
                           .Callback<UserEntity>(u => u.Id = userEntityAfterAdd.Id)
                           .Returns(Task.CompletedTask);
        _mockKeyService.Setup(s => s.GenerateKeyPairAsync()).ReturnsAsync((publicKey, privateKey));
        _mockKeyService.Setup(s => s.SaveKeysAsync(userEntityAfterAdd.Id, publicKey, privateKey)).Returns(Task.CompletedTask);


        // Act
        await _userService.AddAsync(userDto);

        // Assert
        _mockMapper.Verify(m => m.Map<UserEntity>(userDto), Times.Once);
        _mockUserRepository.Verify(r => r.AddAsync(It.Is<UserEntity>(u =>
            u.Username == userDto.Username &&
            u.Email == userDto.Email &&
            u.ProfilePictureFileName == "default_profile_picture.png"
        )), Times.Once);
        _mockKeyService.Verify(s => s.GenerateKeyPairAsync(), Times.Once);
        _mockKeyService.Verify(s => s.SaveKeysAsync(userEntityAfterAdd.Id, publicKey, privateKey), Times.Once);
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
    public async Task UpdateAsync_ValidUser_UpdatesUser()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            Username = "updateduser",
            Email = "updated@example.com",
            ProfilePictureUrl = "new_pic.png"
        };
        var userEntity = new UserEntity
        {
            Id = 1,
            Username = "updateduser",
            Email = "updated@example.com",
            ProfilePictureFileName = "new_pic.png"
        };

        _mockMapper.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>())).Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateAsync(userDto);

        // Assert
        _mockMapper.Verify(m => m.Map<UserEntity>(userDto), Times.Once);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.Is<UserEntity>(u =>
            u.Id == userDto.Id &&
            u.Username == userDto.Username &&
            u.Email == userDto.Email
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
    public async Task DeleteAsync_ExistingUser_DeletesUser()
    {
        // Arrange
        var userId = 1;
        _mockUserRepository.Setup(r => r.DeleteAsync(userId)).Returns(Task.CompletedTask);

        // Act
        await _userService.DeleteAsync(userId);

        // Assert
        _mockUserRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
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
    public async Task UploadUserProfilePictureAsync_ValidImage_ReturnsUpdatedUserDto()
    {
        // Arrange
        var userId = 1;
        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var imageFileName = "profile.jpg";
        var savedFileName = "saved_profile_1.jpg";
        var updatedUserEntity = new UserEntity { Id = userId, Username = "testuser", ProfilePictureFileName = savedFileName };
        var updatedUserDto = new UserDto { Id = userId, Username = "testuser", ProfilePictureUrl = savedFileName };

        _mockUserRepository.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ReturnsAsync(savedFileName);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(updatedUserEntity);
        _mockMapper.Setup(m => m.Map<UserDto>(updatedUserEntity)).Returns(updatedUserDto);

        // Act
        var result = await _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedUserDto.Id, result.Id);
        Assert.Equal(updatedUserDto.ProfilePictureUrl, result.ProfilePictureUrl);
        _mockUserRepository.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(updatedUserEntity), Times.Once);
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
    public async Task UploadUserProfilePictureAsync_UserNotFound_ThrowsBusinessException()
    {
        // Arrange
        var userId = 1;
        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var imageFileName = "profile.jpg";

        _mockUserRepository.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ReturnsAsync((string)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName));
        Assert.Equal($"Failed to upload profile picture for user {userId}. User not found or file save error.", exception.Message);

        _mockUserRepository.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _mockUserRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockMapper.Verify(m => m.Map<UserDto>(It.IsAny<UserEntity>()), Times.Never);
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
                It.Is<Exception>(ex => ex != null && ex is BusinessException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UploadUserProfilePictureAsync_UserNotFoundAfterSave_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = 1;
        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var imageFileName = "profile.jpg";
        var savedFileName = "saved_profile_1.jpg";

        _mockUserRepository.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ReturnsAsync(savedFileName);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserEntity)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName));
        Assert.Equal($"User {userId} not found after image upload process.", exception.Message);

        _mockUserRepository.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(It.IsAny<UserEntity>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Exactly(2)); 
    }
    
    [Fact]
    public async Task GetByIdAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 1;
        var expectedException = new InvalidOperationException("Simulated database error");
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.GetByIdAsync(userId));
        Assert.Equal(expectedException, thrownException);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while fetching user with ID: {userId}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetByUsernameAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var username = "nonexistentuser";
        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync((UserEntity)null);
        _mockMapper.Setup(m => m.Map<UserDto>(It.Is<UserEntity>(u => u == null))).Returns((UserDto)null);

        // Act
        var result = await _userService.GetByUsernameAsync(username);

        // Assert
        Assert.Null(result);
        _mockUserRepository.Verify(r => r.GetByUsernameAsync(username), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(It.Is<UserEntity>(u => u == null)), Times.Once);
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
    public async Task GetByUsernameAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var username = "testuser";
        var expectedException = new InvalidOperationException("DB error during username lookup");
        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.GetByUsernameAsync(username));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while fetching user with username: {username}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var email = "existing@example.com";
        var userEntity = new UserEntity { Id = 1, Username = "testuser", Email = email };
        var userDto = new UserDto { Id = 1, Username = "testuser", Email = email };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(userEntity);
        _mockMapper.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);

        // Act
        var result = await _userService.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userDto.Email, result.Email);
        _mockUserRepository.Verify(r => r.GetByEmailAsync(email), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(userEntity), Times.Once);
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
    public async Task GetByEmailAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var email = "test@example.com";
        var expectedException = new InvalidOperationException("Network error during email lookup");
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.GetByEmailAsync(email));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while fetching user with email: {email}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UserExistsByEmailAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var email = "test@example.com";
        var expectedException = new Exception("Database connection lost during email check");
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => _userService.UserExistsByEmailAsync(email));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while checking if user exists by email: {email}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var userEntities = new List<UserEntity>
        {
            new UserEntity { Id = 1, Username = "user1" },
            new UserEntity { Id = 2, Username = "user2" }
        };
        var userDtos = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "user1" },
            new UserDto { Id = 2, Username = "user2" }
        };

        _mockUserRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(userEntities);
        _mockMapper.Setup(m => m.Map<UserDto>(userEntities[0])).Returns(userDtos[0]);
        _mockMapper.Setup(m => m.Map<UserDto>(userEntities[1])).Returns(userDtos[1]);

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userDtos.Count, result.Count());
        Assert.Contains(userDtos[0], result);
        Assert.Contains(userDtos[1], result);
        _mockUserRepository.Verify(r => r.GetAllAsync(), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(It.IsAny<UserEntity>()), Times.Exactly(userEntities.Count));
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
    public async Task GetAllAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var expectedException = new TimeoutException("Database timeout on GetAll");
        _mockUserRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<TimeoutException>(() => _userService.GetAllAsync());

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error occurred while fetching all users.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_UserWithProfilePicture_AddsUser()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 0,
            Username = "userwithpic",
            Email = "pic@example.com",
            ProfilePictureUrl = "custom_pic.png"
        };
        var userEntityAfterMapping = new UserEntity
        {
            Id = 0,
            Username = "userwithpic",
            Email = "pic@example.com",
            ProfilePictureFileName = "custom_pic.png"
        };
        var userEntityAfterAdd = new UserEntity
        {
            Id = 101,
            Username = "userwithpic",
            Email = "pic@example.com",
            ProfilePictureFileName = "custom_pic.png"
        };
        var publicKey = "PUB_KEY_CUSTOM";
        var privateKey = "PRIV_KEY_CUSTOM";

        _mockMapper.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntityAfterMapping);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<UserEntity>()))
                           .Callback<UserEntity>(u => u.Id = userEntityAfterAdd.Id)
                           .Returns(Task.CompletedTask);
        _mockKeyService.Setup(s => s.GenerateKeyPairAsync()).ReturnsAsync((publicKey, privateKey));
        _mockKeyService.Setup(s => s.SaveKeysAsync(userEntityAfterAdd.Id, publicKey, privateKey)).Returns(Task.CompletedTask);

        // Act
        await _userService.AddAsync(userDto);

        // Assert
        _mockUserRepository.Verify(r => r.AddAsync(It.Is<UserEntity>(u =>
            u.ProfilePictureFileName == "custom_pic.png"
        )), Times.Once);
        _mockKeyService.Verify(s => s.GenerateKeyPairAsync(), Times.Once);
        _mockKeyService.Verify(s => s.SaveKeysAsync(userEntityAfterAdd.Id, publicKey, privateKey), Times.Once);
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
    public async Task AddAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userDto = new UserDto { Username = "erroruser", Email = "error@example.com" };
        var userEntity = new UserEntity { Username = "erroruser", Email = "error@example.com" };
        var expectedException = new InvalidOperationException("Failed to add user to DB");

        _mockMapper.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<UserEntity>())).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.AddAsync(userDto));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while adding user with username: {userDto.Username}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockKeyService.Verify(s => s.GenerateKeyPairAsync(), Times.Never);
        _mockKeyService.Verify(s => s.SaveKeysAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_KeyPairGenerationThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userDto = new UserDto { Username = "erroruserkeys", Email = "error_keys@example.com" };
        var userEntity = new UserEntity { Id = 100, Username = "erroruserkeys", Email = "error_keys@example.com" };
        var expectedException = new Exception("Key generation failed");

        _mockMapper.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<UserEntity>())).Returns(Task.CompletedTask);
        _mockKeyService.Setup(s => s.GenerateKeyPairAsync()).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => _userService.AddAsync(userDto));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while adding user with username: {userDto.Username}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<UserEntity>()), Times.Once);
        _mockKeyService.Verify(s => s.GenerateKeyPairAsync(), Times.Once);
        _mockKeyService.Verify(s => s.SaveKeysAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never); // Не должен быть вызван
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userDto = new UserDto { Id = 1, Username = "updatefail" };
        var userEntity = new UserEntity { Id = 1, Username = "updatefail" };
        var expectedException = new DataException("Update failed in DB");

        _mockMapper.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>())).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<DataException>(() => _userService.UpdateAsync(userDto));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while updating user with ID: {userDto.Id}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 1;
        var expectedException = new InvalidOperationException("Failed to delete user from DB");
        _mockUserRepository.Setup(r => r.DeleteAsync(userId)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.DeleteAsync(userId));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while deleting user with ID: {userId}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserStatusAsync_ReturnsStatus()
    {
        // Arrange
        var userId = 1;
        var expectedStatus = "Online";
        _mockUserRepository.Setup(r => r.GetUserStatusAsync(userId)).ReturnsAsync(expectedStatus);

        // Act
        var result = await _userService.GetUserStatusAsync(userId);

        // Assert
        Assert.Equal(expectedStatus, result);
        _mockUserRepository.Verify(r => r.GetUserStatusAsync(userId), Times.Once);
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
    public async Task GetUserStatusAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 1;
        var expectedException = new Exception("Status fetch failed");
        _mockUserRepository.Setup(r => r.GetUserStatusAsync(userId)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => _userService.GetUserStatusAsync(userId));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while fetching user status for user ID: {userId}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_UpdatesStatus()
    {
        // Arrange
        var userId = 1;
        var newStatus = "Offline";
        _mockUserRepository.Setup(r => r.UpdateUserStatusAsync(userId, newStatus)).Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateUserStatusAsync(userId, newStatus);

        // Assert
        _mockUserRepository.Verify(r => r.UpdateUserStatusAsync(userId, newStatus), Times.Once);
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
    public async Task UpdateUserStatusAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 1;
        var newStatus = "Busy";
        var expectedException = new Exception("Status update failed");
        _mockUserRepository.Setup(r => r.UpdateUserStatusAsync(userId, newStatus)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => _userService.UpdateUserStatusAsync(userId, newStatus));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while updating status for user ID: {userId}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetPublicKeyAsync_ReturnsPublicKey()
    {
        // Arrange
        var userId = 1;
        var expectedPublicKey = "PUBLIC_KEY_123";
        _mockKeyService.Setup(s => s.GetPublicKeyAsync(userId)).ReturnsAsync(expectedPublicKey);

        // Act
        var result = await _userService.GetPublicKeyAsync(userId);

        // Assert
        Assert.Equal(expectedPublicKey, result);
        _mockKeyService.Verify(s => s.GetPublicKeyAsync(userId), Times.Once);
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
    public async Task GetPublicKeyAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 1;
        var expectedException = new BusinessException("Public key not found in KeyService");
        _mockKeyService.Setup(s => s.GetPublicKeyAsync(userId)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<BusinessException>(() => _userService.GetPublicKeyAsync(userId));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while fetching public key for user ID: {userId}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetPrivateKeyAsync_ReturnsPrivateKey()
    {
        // Arrange
        var userId = 1;
        var expectedPrivateKey = "PRIVATE_KEY_456";
        _mockKeyService.Setup(s => s.GetPrivateKeyAsync(userId)).ReturnsAsync(expectedPrivateKey);

        // Act
        var result = await _userService.GetPrivateKeyAsync(userId);

        // Assert
        Assert.Equal(expectedPrivateKey, result);
        _mockKeyService.Verify(s => s.GetPrivateKeyAsync(userId), Times.Once);
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
    public async Task GetPrivateKeyAsync_ThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = 1;
        var expectedException = new BusinessException("Private key not found in KeyService");
        _mockKeyService.Setup(s => s.GetPrivateKeyAsync(userId)).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<BusinessException>(() => _userService.GetPrivateKeyAsync(userId));

        Assert.Equal(expectedException, thrownException);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error occurred while fetching private key for user ID: {userId}.")),
                It.Is<Exception>(ex => ex == expectedException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}