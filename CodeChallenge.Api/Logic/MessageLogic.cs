using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;

namespace CodeChallenge.Api.Logic;

public class MessageLogic : IMessageLogic
{
    private readonly IMessageRepository _repository;

    public MessageLogic(IMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> CreateMessageAsync(Guid organizationId, CreateMessageRequest request)
    {
        // Validate the request
        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
        {
            return new ValidationError(validationErrors);
        }

        // Check for duplicate title
        var existingMessage = await _repository.GetByTitleAsync(organizationId, request.Title);
        if (existingMessage != null)
        {
            return new Conflict($"A message with title '{request.Title}' already exists in this organization");
        }

        // Create the message
        var message = new Message
        {
            OrganizationId = organizationId,
            Title = request.Title,
            Content = request.Content,
            IsActive = true
        };

        var createdMessage = await _repository.CreateAsync(message);
        return new Created<Message>(createdMessage);
    }

    public async Task<Result> UpdateMessageAsync(Guid organizationId, Guid id, UpdateMessageRequest request)
    {
        // Validate the request
        var validationErrors = ValidateUpdateRequest(request);
        if (validationErrors.Count > 0)
        {
            return new ValidationError(validationErrors);
        }

        // Check if message exists
        var existingMessage = await _repository.GetByIdAsync(organizationId, id);
        if (existingMessage == null)
        {
            return new NotFound($"Message with id '{id}' not found");
        }

        // Can only update active messages
        if (!existingMessage.IsActive)
        {
            return new ValidationError(new Dictionary<string, string[]>
            {
                { "IsActive", new[] { "Cannot update inactive messages" } }
            });
        }

        // Check for duplicate title (excluding current message)
        var duplicateMessage = await _repository.GetByTitleAsync(organizationId, request.Title);
        if (duplicateMessage != null && duplicateMessage.Id != id)
        {
            return new Conflict($"A message with title '{request.Title}' already exists in this organization");
        }

        // Update the message
        existingMessage.Title = request.Title;
        existingMessage.Content = request.Content;
        existingMessage.IsActive = request.IsActive;
        // UpdatedAt is set automatically in the repository

        var updatedMessage = await _repository.UpdateAsync(existingMessage);
        if (updatedMessage == null)
        {
            return new NotFound($"Message with id '{id}' not found");
        }

        return new Updated();
    }

    public async Task<Result> DeleteMessageAsync(Guid organizationId, Guid id)
    {
        // Check if message exists
        var existingMessage = await _repository.GetByIdAsync(organizationId, id);
        if (existingMessage == null)
        {
            return new NotFound($"Message with id '{id}' not found");
        }

        // Can only delete active messages
        if (!existingMessage.IsActive)
        {
            return new ValidationError(new Dictionary<string, string[]>
            {
                { "IsActive", new[] { "Cannot delete inactive messages" } }
            });
        }

        var deleted = await _repository.DeleteAsync(organizationId, id);
        if (!deleted)
        {
            return new NotFound($"Message with id '{id}' not found");
        }

        return new Deleted();
    }

    public async Task<Message?> GetMessageAsync(Guid organizationId, Guid id)
    {
        return await _repository.GetByIdAsync(organizationId, id);
    }

    public async Task<IEnumerable<Message>> GetAllMessagesAsync(Guid organizationId)
    {
        return await _repository.GetAllByOrganizationAsync(organizationId);
    }

    private Dictionary<string, string[]> ValidateCreateRequest(CreateMessageRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        // Validate Title
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title", new[] { "Title is required" });
        }
        else if (request.Title.Length < 3 || request.Title.Length > 200)
        {
            errors.Add("Title", new[] { "Title must be between 3 and 200 characters" });
        }

        // Validate Content
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors.Add("Content", new[] { "Content is required" });
        }
        else if (request.Content.Length < 10 || request.Content.Length > 1000)
        {
            errors.Add("Content", new[] { "Content must be between 10 and 1000 characters" });
        }

        return errors;
    }

    private Dictionary<string, string[]> ValidateUpdateRequest(UpdateMessageRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        // Validate Title
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title", new[] { "Title is required" });
        }
        else if (request.Title.Length < 3 || request.Title.Length > 200)
        {
            errors.Add("Title", new[] { "Title must be between 3 and 200 characters" });
        }

        // Validate Content
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors.Add("Content", new[] { "Content is required" });
        }
        else if (request.Content.Length < 10 || request.Content.Length > 1000)
        {
            errors.Add("Content", new[] { "Content must be between 10 and 1000 characters" });
        }

        return errors;
    }
}