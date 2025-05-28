using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Exceptions;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class KeyService : IKeyService
{
    private readonly IUserKeyRepository _userKeyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<KeyService> _logger;

    public KeyService(
        ILogger<KeyService> logger,
        IUserKeyRepository userKeyRepository,
        IMapper mapper
    )
    {
        _logger = logger;
        _userKeyRepository = userKeyRepository;
        _mapper = mapper;
    }

   public async Task<(string publicKey, string privateKey)> GenerateKeyPairAsync()
    {
        try
        {
            var result = await _userKeyRepository.GenerateKeyPairAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating key pair.");
            throw;
        }
    }

    public async Task SaveKeysAsync(int userId, string publicKey, string privateKey)
    {
        try
        {
            await _userKeyRepository.SaveKeysAsync(userId, publicKey, privateKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving keys for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<string?> GetPublicKeyAsync(int userId)
    {
        try
        {
            var result = await _userKeyRepository.GetPublicKeyAsync(userId);
            if (result == null)
            {
                _logger.LogWarning("Public key not found for user {UserId}.", userId);
                throw new BusinessException($"Public key not found for user: {userId}");
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving public key for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<string?> GetPrivateKeyAsync(int userId)
    {
        try
        {
            var result = await _userKeyRepository.GetPrivateKeyAsync(userId);
            if (result == null)
            {
                _logger.LogWarning("Private key not found for user {UserId}.", userId);
                throw new BusinessException($"Private key not found for user: {userId}");
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving private key for user {UserId}.", userId);
            throw;
        }
    }
}