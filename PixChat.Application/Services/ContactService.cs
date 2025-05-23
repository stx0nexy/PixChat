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

public class ContactService : BaseDataService<ApplicationDbContext>, IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;

    public ContactService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService<ApplicationDbContext>> logger,
        IContactRepository contactRepository,
        IChatService chatService,
        IMapper mapper
    ) : base(dbContextWrapper, logger)
    {
        _contactRepository = contactRepository;
        _chatService = chatService;
        _mapper = mapper;
    }

    public async Task AddContact(int userId, int contactUserId, bool isBlocked)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _contactRepository.AddContactAsync(userId, contactUserId, isBlocked);
            
            var chat = await _chatService.GetPrivateChatIfExists(userId, contactUserId);
            if (chat == null)
            {
                await _chatService.CreatePrivateChatAsync(userId, contactUserId);
            }
        });
    }


    public async Task<ContactDto> GetContact(int userId, int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _contactRepository.GetContactAsync(userId, contactUserId);
            return _mapper.Map<ContactDto>(result);
        });
    }

    public async Task<bool> IsContactBlocked(int userId, int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _contactRepository.IsContactBlockedAsync(userId, contactUserId);
            return result;
        });
    }

    public async Task RemoveContact(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _contactRepository.RemoveContactAsync(userId, contactUserId);
        });
    }

    public async Task<IEnumerable<ContactDto>> GetAllContacts(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _contactRepository.GetAllContactsAsync(userId);
            return result.Select(s => _mapper.Map<ContactDto>(s)).ToList();
        });
    }

    public async Task UpdateContactBlockStatus(int userId, int contactUserId, bool isBlocked)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _contactRepository.UpdateContactBlockStatusAsync(userId, contactUserId, isBlocked);
        });
    }

    public async Task<IEnumerable<ContactDto>> GetBlockedContacts(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result = await _contactRepository.GetBlockedContactsAsync(userId);
            return result.Select(s => _mapper.Map<ContactDto>(s)).ToList();
        });
    }
    
    public async Task ConfirmFriendRequest(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _contactRepository.RemoveFriendRequestAsync(userId, contactUserId);
            await _contactRepository.AddContactAsync(userId, contactUserId, false);
            await _contactRepository.AddContactAsync(contactUserId, userId, false);
            var chat = await _chatService.GetPrivateChatIfExists(userId, contactUserId);
            if (chat == null)
            {
                await _chatService.CreatePrivateChatAsync(userId, contactUserId);
            }
        });
    }
    
    public async Task RejectFriendRequest(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _contactRepository.RemoveFriendRequestAsync(userId, contactUserId);
        });
    }

    public async Task SendFriendRequest(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            await _contactRepository.SendFriendRequestAsync(userId, contactUserId);
        });
    }

    public async Task<IEnumerable<FriendRequestEntity>> GetFriendRequests (int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var result =  await _contactRepository.GetFriendRequestsAsync(contactUserId);
            return result;
        });
    }
}