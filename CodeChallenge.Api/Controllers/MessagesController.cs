using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _repository;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageRepository repository, ILogger<MessagesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        _logger.LogInformation("Getting all messages for organization {OrganizationId}", organizationId);
        var messages = await _repository.GetAllByOrganizationAsync(organizationId).ConfigureAwait(false);
        return Ok(messages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        _logger.LogInformation("Getting message {MessageId} for organization {OrganizationId}", id, organizationId);
        var message = await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);

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

        var message = new Message
        {
            OrganizationId = organizationId,
            Title = request.Title,
            Content = request.Content,
            IsActive = true
        };

        var createdMessage = await _repository.CreateAsync(message).ConfigureAwait(false);

        _logger.LogInformation("Message {MessageId} created successfully", createdMessage.Id);
        return CreatedAtAction(nameof(GetById), new { organizationId, id = createdMessage.Id }, createdMessage);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        _logger.LogInformation("Updating message {MessageId} for organization {OrganizationId}", id, organizationId);

        var existingMessage = await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);
        if (existingMessage == null)
        {
            _logger.LogWarning("Message {MessageId} not found for organization {OrganizationId}", id, organizationId);
            return NotFound();
        }

        existingMessage.Title = request.Title;
        existingMessage.Content = request.Content;
        existingMessage.IsActive = request.IsActive;

        var updatedMessage = await _repository.UpdateAsync(existingMessage).ConfigureAwait(false);
        if (updatedMessage == null)
        {
            _logger.LogError("Failed to update message {MessageId}", id);
            return StatusCode(500, "Failed to update message");
        }

        _logger.LogInformation("Message {MessageId} updated successfully", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        _logger.LogInformation("Deleting message {MessageId} for organization {OrganizationId}", id, organizationId);

        var deleted = await _repository.DeleteAsync(organizationId, id).ConfigureAwait(false);
        if (!deleted)
        {
            _logger.LogWarning("Message {MessageId} not found for organization {OrganizationId}", id, organizationId);
            return NotFound();
        }

        _logger.LogInformation("Message {MessageId} deleted successfully", id);
        return NoContent();
    }
}
