using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using FluentAssertions;
using Moq;

namespace CodeChallenge.Tests;
public class MessageLogicTests
{
    private readonly Mock<IMessageRepository> _mockRepository;
    private readonly MessageLogic _messageLogic;
    private readonly Guid _organizationId;

    public MessageLogicTests()
    {
        _mockRepository = new Mock<IMessageRepository>();
        _messageLogic = new MessageLogic(_mockRepository.Object);
        _organizationId = Guid.NewGuid();
    }

    #region CreateMessageAsync Tests

    [Fact]
    public async Task CreateMessageAsync_WithValidRequest_ReturnsCreatedWithMessage()
    {
        // Arrange
        var request = new CreateMessageRequest
        {
            Title = "Test Message",
            Content = "This is a test message content that is long enough."
        };

        _mockRepository
            .Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
            .ReturnsAsync((Message?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) =>
            {
                m.Id = Guid.NewGuid();
                m.CreatedAt = DateTime.UtcNow;
                return m;
            });

        // Act
        var result = await _messageLogic.CreateMessageAsync(_organizationId, request);

        // Assert
        result.Should().BeOfType<Created<Message>>();
        var createdResult = (Created<Message>)result;
        createdResult.Value.Should().NotBeNull();
        createdResult.Value.Title.Should().Be(request.Title);
        createdResult.Value.Content.Should().Be(request.Content);
        createdResult.Value.OrganizationId.Should().Be(_organizationId);
        createdResult.Value.IsActive.Should().BeTrue();

        _mockRepository.Verify(r => r.GetByTitleAsync(_organizationId, request.Title), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Message>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_WithDuplicateTitle_ReturnsConflict()
    {
        // Arrange
        var request = new CreateMessageRequest
        {
            Title = "Duplicate Title",
            Content = "This is a test message content."
        };

        var existingMessage = new Message
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            Title = "Duplicate Title",
            Content = "Existing content",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _messageLogic.CreateMessageAsync(_organizationId, request);

        // Assert
        result.Should().BeOfType<Conflict>();
        var conflictResult = (Conflict)result;
        conflictResult.Message.Should().Contain("already exists");

        _mockRepository.Verify(r => r.GetByTitleAsync(_organizationId, request.Title), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Message>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Valid content with more than ten characters")]
    [InlineData("AB", "Valid content with more than ten characters")]
    [InlineData(null, "Valid content with more than ten characters")]
    public async Task CreateMessageAsync_WithInvalidTitle_ReturnsValidationError(string title, string content)
    {
        // Arrange
        var request = new CreateMessageRequest
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _messageLogic.CreateMessageAsync(_organizationId, request);

        // Assert
        result.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result;
        validationError.Errors.Should().ContainKey("Title");

        _mockRepository.Verify(r => r.GetByTitleAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Message>()), Times.Never);
    }

    [Theory]
    [InlineData("Valid Title", "Short")]
    [InlineData("Valid Title", "")]
    [InlineData("Valid Title", null)]
    public async Task CreateMessageAsync_WithInvalidContentLength_ReturnsValidationError(string title, string content)
    {
        // Arrange
        var request = new CreateMessageRequest
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _messageLogic.CreateMessageAsync(_organizationId, request);

        // Assert
        result.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result;
        validationError.Errors.Should().ContainKey("Content");
        validationError.Errors["Content"].Should().Contain(e => e.Contains("between 10 and 1000 characters") || e.Contains("required"));

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task CreateMessageAsync_WithContentTooLong_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateMessageRequest
        {
            Title = "Valid Title",
            Content = new string('A', 1001) // 1001 characters
        };

        // Act
        var result = await _messageLogic.CreateMessageAsync(_organizationId, request);

        // Assert
        result.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result;
        validationError.Errors.Should().ContainKey("Content");

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Message>()), Times.Never);
    }

    #endregion

    #region UpdateMessageAsync Tests

    [Fact]
    public async Task UpdateMessageAsync_WithValidRequest_ReturnsUpdated()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = "Updated Title",
            Content = "Updated content with sufficient length.",
            IsActive = true
        };

        var existingMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Original Title",
            Content = "Original content.",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(existingMessage);

        _mockRepository
            .Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
            .ReturnsAsync((Message?)null);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageLogic.UpdateMessageAsync(_organizationId, messageId, request);

        // Assert
        result.Should().BeOfType<Updated>();

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Message>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMessageAsync_WithNonExistentMessage_ReturnsNotFound()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = "Updated Title",
            Content = "Updated content with sufficient length.",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _messageLogic.UpdateMessageAsync(_organizationId, messageId, request);

        // Assert
        result.Should().BeOfType<NotFound>();
        var notFoundResult = (NotFound)result;
        notFoundResult.Message.Should().Contain(messageId.ToString());

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMessageAsync_WithInactiveMessage_ReturnsValidationError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = "Updated Title",
            Content = "Updated content with sufficient length.",
            IsActive = true
        };

        var existingMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Original Title",
            Content = "Original content.",
            IsActive = false // Inactive message
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _messageLogic.UpdateMessageAsync(_organizationId, messageId, request);

        // Assert
        result.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result;
        validationError.Errors.Should().ContainKey("IsActive");
        validationError.Errors["IsActive"].Should().Contain(e => e.Contains("Cannot update inactive messages"));

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMessageAsync_WithDuplicateTitle_ReturnsConflict()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var otherMessageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = "Duplicate Title",
            Content = "Updated content with sufficient length.",
            IsActive = true
        };

        var existingMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Original Title",
            Content = "Original content.",
            IsActive = true
        };

        var duplicateMessage = new Message
        {
            Id = otherMessageId,
            OrganizationId = _organizationId,
            Title = "Duplicate Title",
            Content = "Other content.",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(existingMessage);

        _mockRepository
            .Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
            .ReturnsAsync(duplicateMessage);

        // Act
        var result = await _messageLogic.UpdateMessageAsync(_organizationId, messageId, request);

        // Assert
        result.Should().BeOfType<Conflict>();
        var conflictResult = (Conflict)result;
        conflictResult.Message.Should().Contain("already exists");

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Message>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Valid content with more than ten characters")]
    [InlineData("AB", "Valid content with more than ten characters")]
    public async Task UpdateMessageAsync_WithInvalidTitle_ReturnsValidationError(string title, string content)
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = title,
            Content = content,
            IsActive = true
        };

        // Act
        var result = await _messageLogic.UpdateMessageAsync(_organizationId, messageId, request);

        // Assert
        result.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result;
        validationError.Errors.Should().ContainKey("Title");

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region DeleteMessageAsync Tests

    [Fact]
    public async Task DeleteMessageAsync_WithValidMessage_ReturnsDeleted()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Message to Delete",
            Content = "Content to delete.",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(existingMessage);

        _mockRepository
            .Setup(r => r.DeleteAsync(_organizationId, messageId))
            .ReturnsAsync(true);

        // Act
        var result = await _messageLogic.DeleteMessageAsync(_organizationId, messageId);

        // Assert
        result.Should().BeOfType<Deleted>();

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(_organizationId, messageId), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_WithNonExistentMessage_ReturnsNotFound()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _messageLogic.DeleteMessageAsync(_organizationId, messageId);

        // Assert
        result.Should().BeOfType<NotFound>();
        var notFoundResult = (NotFound)result;
        notFoundResult.Message.Should().Contain(messageId.ToString());

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_WithInactiveMessage_ReturnsValidationError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Inactive Message",
            Content = "Inactive content.",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _messageLogic.DeleteMessageAsync(_organizationId, messageId);

        // Assert
        result.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result;
        validationError.Errors.Should().ContainKey("IsActive");
        validationError.Errors["IsActive"].Should().Contain(e => e.Contains("Cannot delete inactive messages"));

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenRepositoryDeleteFails_ReturnsNotFound()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Message to Delete",
            Content = "Content to delete.",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(existingMessage);

        _mockRepository
            .Setup(r => r.DeleteAsync(_organizationId, messageId))
            .ReturnsAsync(false);

        // Act
        var result = await _messageLogic.DeleteMessageAsync(_organizationId, messageId);

        // Assert
        result.Should().BeOfType<NotFound>();

        _mockRepository.Verify(r => r.DeleteAsync(_organizationId, messageId), Times.Once);
    }

    #endregion

    #region GetMessageAsync Tests

    [Fact]
    public async Task GetMessageAsync_WithExistingMessage_ReturnsMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var expectedMessage = new Message
        {
            Id = messageId,
            OrganizationId = _organizationId,
            Title = "Test Message",
            Content = "Test content.",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _messageLogic.GetMessageAsync(_organizationId, messageId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedMessage);

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
    }

    [Fact]
    public async Task GetMessageAsync_WithNonExistentMessage_ReturnsNull()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByIdAsync(_organizationId, messageId))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _messageLogic.GetMessageAsync(_organizationId, messageId);

        // Assert
        result.Should().BeNull();

        _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, messageId), Times.Once);
    }

    #endregion

    #region GetAllMessagesAsync Tests

    [Fact]
    public async Task GetAllMessagesAsync_ReturnsAllMessagesForOrganization()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Id = Guid.NewGuid(), OrganizationId = _organizationId, Title = "Message 1", Content = "Content 1", IsActive = true },
            new Message { Id = Guid.NewGuid(), OrganizationId = _organizationId, Title = "Message 2", Content = "Content 2", IsActive = false }
        };

        _mockRepository
            .Setup(r => r.GetAllByOrganizationAsync(_organizationId))
            .ReturnsAsync(messages);

        // Act
        var result = await _messageLogic.GetAllMessagesAsync(_organizationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(messages);

        _mockRepository.Verify(r => r.GetAllByOrganizationAsync(_organizationId), Times.Once);
    }

    [Fact]
    public async Task GetAllMessagesAsync_WithNoMessages_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllByOrganizationAsync(_organizationId))
            .ReturnsAsync(new List<Message>());

        // Act
        var result = await _messageLogic.GetAllMessagesAsync(_organizationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockRepository.Verify(r => r.GetAllByOrganizationAsync(_organizationId), Times.Once);
    }

    #endregion
}