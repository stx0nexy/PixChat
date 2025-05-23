using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;

namespace PixChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageMetadataService _messageMetadataService;
    private readonly IMapper _mapper;

    public MessagesController(IMessageMetadataService messageMetadataService, IMapper mapper)
    {
        _messageMetadataService = messageMetadataService;
        _mapper = mapper;
    }

    [Authorize]
    [HttpGet("{messageId}")]
    public async Task<IActionResult> GetMessageMetadata(int messageId)
    {
        var messageMetadata = await _messageMetadataService.GetMessageMetadata(messageId);
        if (messageMetadata == null)
            return NotFound();
            
        return Ok(messageMetadata);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddMessageMetadata([FromBody] MessageMetadataDto request)
    {
        var messageMetadataDto = _mapper.Map<MessageMetadataDto>(request);
        await _messageMetadataService.AddMessageMetadata(messageMetadataDto);
        return Ok();
    }

    [Authorize]
    [HttpPut("{messageId}/status")]
    public async Task<IActionResult> UpdateMessageStatus(int messageId, [FromBody] UpdateMessageStatusRequest request)
    {
        await _messageMetadataService.UpdateMessageStatus(messageId, request.Status);
        return Ok();
    }

    [Authorize]
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessageMetadata(int messageId)
    {
        await _messageMetadataService.DeleteMessageMetadata(messageId);
        return Ok();
    }
}