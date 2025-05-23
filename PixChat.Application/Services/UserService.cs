using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Application.Services;

public class UserService : BaseDataService<ApplicationDbContext>, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IKeyService _keyService;
    private readonly IMapper _mapper;

    public UserService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IUserRepository userRepository,
        IKeyService keyService,
        IMapper mapper)
        : base(dbContextWrapper, logger)
    {
        _userRepository = userRepository;
        _keyService = keyService;
        _mapper = mapper;
    }

    public async Task<UserDto> GetByIdAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _userRepository.GetByIdAsync(userId);
            return _mapper.Map<UserDto>(result);
        });
    }

    public async Task<UserDto> GetByUsernameAsync(string username)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _userRepository.GetByUsernameAsync(username);
            return _mapper.Map<UserDto>(result);
        });
    }

    public async Task<UserDto> GetByEmailAsync(string email)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _userRepository.GetByEmailAsync(email);
            return _mapper.Map<UserDto>(result);
        });
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _userRepository.GetAllAsync();
            return result.Select(s => _mapper.Map<UserDto>(s)).ToList();
        });
    }

    public async Task AddAsync(UserDto user)
    {
        await ExecuteSafeAsync(async () =>
        {
            var userEntity = _mapper.Map<UserEntity>(user);
            if (string.IsNullOrEmpty(userEntity.ProfilePictureFileName))
            {
                userEntity.ProfilePictureFileName = "default_profile_picture.png";
            }

            await _userRepository.AddAsync(userEntity);

            var (publicKey, privateKey) = await _keyService.GenerateKeyPairAsync();
            await _keyService.SaveKeysAsync(userEntity.Id, publicKey, privateKey);
        });
    }

    public async Task UpdateAsync(UserDto user)
    {
        await ExecuteSafeAsync(async () =>
        {
            var userEntity = _mapper.Map<UserEntity>(user);
            await _userRepository.UpdateAsync(userEntity);
        });
    }

    public async Task DeleteAsync(int userId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _userRepository.DeleteAsync(userId);
        });
    }

    public async Task<string> GetUserStatusAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await _userRepository.GetUserStatusAsync(userId);
        });
    }

    public async Task UpdateUserStatusAsync(int userId, string status)
    {
        await ExecuteSafeAsync(async () =>
        {
             await _userRepository.UpdateUserStatusAsync(userId, status);
        });
    }
    
    public async Task<UserDto> UploadUserProfilePictureAsync(int userId, Stream imageStream, string imageFileName)
    {
        var fileName = await _userRepository.SaveUserImageAsync(userId, imageStream, imageFileName);

        var userEntity = await _userRepository.GetByIdAsync(userId);
        return _mapper.Map<UserDto>(userEntity);
    }
    
    public async Task<string> GetPublicKeyAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await _userRepository.GetPublicKeyAsync(userId);
        });
    }

    public async Task<string> GetPrivateKeyAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await _userRepository.GetPrivateKeyAsync(userId);
        });
    }
}