using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;

namespace CodeChallenge.Api.Logic;

public class MessageLogic : IMessageLogic
{
    private readonly IMessageRepository _repository;

    // Validation constants
    private static class ValidationConstants
    {
        public const int TitleMinLength = 3;
        public const int TitleMaxLength = 200;
        public const int ContentMinLength = 10;
        public const int ContentMaxLength = 1000;
    }

    // Field names for validation errors
    private static class FieldNames
    {
        public const string Title = nameof(Title);
        public const string Content = nameof(Content);
        public const string IsActive = nameof(IsActive);
    }

    // Error messages
    private static class ErrorMessages
    {
        public const string TitleRequired = "Title is required";
        public static readonly string TitleLength = $"Title must be between {ValidationConstants.TitleMinLength} and {ValidationConstants.TitleMaxLength} characters";
        public const string ContentRequired = "Content is required";
        public static readonly string ContentLength = $"Content must be between {ValidationConstants.ContentMinLength} and {ValidationConstants.ContentMaxLength} characters";
        public const string CannotUpdateInactive = "Cannot update inactive messages";
        public const string CannotDeleteInactive = "Cannot delete inactive messages";
        public const string TitleAlreadyExists = "A message with title '{0}' already exists in this organization";
        public const string MessageNotFound = "Message with id '{0}' not found";
    }

    public MessageLogic(IMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> CreateMessageAsync(Guid organizationId, CreateMessageRequest request)
    {
        var validationErrors = ValidateMessageRequest(request.Title, request.Content);
        if (validationErrors.Count > 0)
        {
            return new ValidationError(validationErrors);
        }

        var existingMessage = await _repository.GetByTitleAsync(organizationId, request.Title);
        if (existingMessage != null)
        {
            return new Conflict(string.Format(ErrorMessages.TitleAlreadyExists, request.Title));
        }

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
        var validationErrors = ValidateMessageRequest(request.Title, request.Content);
        if (validationErrors.Count > 0)
        {
            return new ValidationError(validationErrors);
        }

        var existingMessage = await _repository.GetByIdAsync(organizationId, id);
        if (existingMessage == null)
        {
            return new NotFound(string.Format(ErrorMessages.MessageNotFound, id));
        }

        if (!existingMessage.IsActive)
        {
            return CreateIsActiveValidationError(ErrorMessages.CannotUpdateInactive);
        }

        var duplicateMessage = await _repository.GetByTitleAsync(organizationId, request.Title);
        if (duplicateMessage != null && duplicateMessage.Id != id)
        {
            return new Conflict(string.Format(ErrorMessages.TitleAlreadyExists, request.Title));
        }

        existingMessage.Title = request.Title;
        existingMessage.Content = request.Content;
        existingMessage.IsActive = request.IsActive;

        await _repository.UpdateAsync(existingMessage);
        return new Updated();
    }

    public async Task<Result> DeleteMessageAsync(Guid organizationId, Guid id)
    {
        var existingMessage = await _repository.GetByIdAsync(organizationId, id);
        if (existingMessage == null)
        {
            return new NotFound(string.Format(ErrorMessages.MessageNotFound, id));
        }

        if (!existingMessage.IsActive)
        {
            return CreateIsActiveValidationError(ErrorMessages.CannotDeleteInactive);
        }

        await _repository.DeleteAsync(organizationId, id);
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

    /// <summary>
    /// Unified validation for both Create and Update requests
    /// </summary>
    private Dictionary<string, string[]> ValidateMessageRequest(string title, string content)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateTitle(title, errors);
        ValidateContent(content, errors);

        return errors;
    }

    private static void ValidateTitle(string title, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(FieldNames.Title, [ErrorMessages.TitleRequired]);
        }
        else if (title.Length < ValidationConstants.TitleMinLength || title.Length > ValidationConstants.TitleMaxLength)
        {
            errors.Add(FieldNames.Title, [ErrorMessages.TitleLength]);
        }
    }

    private static void ValidateContent(string content, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add(FieldNames.Content, [ErrorMessages.ContentRequired]);
        }
        else if (content.Length < ValidationConstants.ContentMinLength || content.Length > ValidationConstants.ContentMaxLength)
        {
            errors.Add(FieldNames.Content, [ErrorMessages.ContentLength]);
        }
    }

    private static ValidationError CreateIsActiveValidationError(string message)
    {
        return new ValidationError(new Dictionary<string, string[]>
        {
            { FieldNames.IsActive, [message] }
        });
    }
}