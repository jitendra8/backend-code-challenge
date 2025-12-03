using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageLogic _messageLogic;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageLogic messageLogic, ILogger<MessagesController> logger)
    {
        _messageLogic = messageLogic;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        _logger.LogInformation("Getting all messages for organization {OrganizationId}", organizationId);
        var messages = await _messageLogic.GetAllMessagesAsync(organizationId);
        return Ok(messages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        _logger.LogInformation("Getting message {MessageId} for organization {OrganizationId}", id, organizationId);
        var message = await _messageLogic.GetMessageAsync(organizationId, id);
        
        if (message == null)
        {
            _logger.LogWarning("Message {MessageId} not found for organization {OrganizationId}", id, organizationId);
            return NotFound();
        }

        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult<Message>> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        _logger.LogInformation("Creating message for organization {OrganizationId}", organizationId);
        
        var result = await _messageLogic.CreateMessageAsync(organizationId, request);

        return result switch
        {
            Created<Message> created => CreatedAtAction(nameof(GetById), new { organizationId, id = created.Value.Id }, created.Value),
            ValidationError validationError => BadRequest(new { errors = validationError.Errors }),
            Conflict conflict => Conflict(new { message = conflict.Message }),
            _ => StatusCode(500, "An unexpected error occurred")
        };
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        _logger.LogInformation("Updating message {MessageId} for organization {OrganizationId}", id, organizationId);
        
        var result = await _messageLogic.UpdateMessageAsync(organizationId, id, request);

        return result switch
        {
            Updated => NoContent(),
            NotFound notFound => NotFound(new { message = notFound.Message }),
            ValidationError validationError => BadRequest(new { errors = validationError.Errors }),
            Conflict conflict => Conflict(new { message = conflict.Message }),
            _ => StatusCode(500, "An unexpected error occurred")
        };
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        _logger.LogInformation("Deleting message {MessageId} for organization {OrganizationId}", id, organizationId);
        
        var result = await _messageLogic.DeleteMessageAsync(organizationId, id);

        return result switch
        {
            Deleted => NoContent(),
            NotFound notFound => NotFound(new { message = notFound.Message }),
            ValidationError validationError => BadRequest(new { errors = validationError.Errors }),
            _ => StatusCode(500, "An unexpected error occurred")
        };
    }
}
