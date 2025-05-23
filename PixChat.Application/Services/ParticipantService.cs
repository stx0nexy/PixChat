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

public class ParticipantService: BaseDataService<ApplicationDbContext>, IParticipantService
{
    private readonly IChatParticipantRepository _chatParticipantRepository;
    private readonly IMapper _mapper;

    public ParticipantService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IChatParticipantRepository chatParticipantRepository,
        IMapper mapper
    ) : base(dbContextWrapper, logger)
    {
        _chatParticipantRepository = chatParticipantRepository;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<ChatParticipantDto>> GetParticipantsByChatIdAsync(int chatId)
    {
        try
        {
            var participants = await _chatParticipantRepository.GetParticipantsByChatIdAsync(chatId);
            return _mapper.Map<IEnumerable<ChatParticipantDto>>(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting participants by chat ID.");
            throw;
        }
    }

    public async Task AddParticipantAsync(AddParticipantDto dto)
    {
        try
        {
            var participant = _mapper.Map<ChatParticipantEntity>(dto);
            participant.JoinedAt = DateTime.UtcNow;

            await _chatParticipantRepository.AddAsync(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding participant to chat.");
            throw;
        }
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        try
        {
            await _chatParticipantRepository.DeleteAsync(participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing participant from chat.");
            throw;
        }
    }

    public async Task UpdateParticipantAsync(UpdateParticipantDto dto)
    {
        try
        {
            var participant = await _chatParticipantRepository.GetByIdAsync(dto.Id);
            if (participant == null)
            {
                throw new KeyNotFoundException($"Participant with ID {dto.Id} not found.");
            }

            participant.IsAdmin = dto.IsAdmin;
            await _chatParticipantRepository.UpdateAsync(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating participant.");
            throw;
        }
    }
    
}