using FluentAssertions;
using PokManager.Application.UseCases.InstanceQuery;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceQuery;

/// <summary>
/// Tests for GetInstanceStatusRequestValidator using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class GetInstanceStatusRequestValidatorTests
{
    private readonly GetInstanceStatusRequestValidator _validator = new();
    private GetInstanceStatusRequest _request = null!;
    private FluentValidation.Results.ValidationResult _validationResult = null!;

    [Fact]
    public void Given_ValidRequest_When_Validated_Then_IsValid()
    {
        // Given
        GivenValidRequest();

        // When
        WhenValidationIsPerformed();

        // Then
        ThenValidationIsSuccessful();
    }

    [Fact]
    public void Given_EmptyInstanceId_When_Validated_Then_IsInvalid()
    {
        // Given
        GivenRequestWithEmptyInstanceId();

        // When
        WhenValidationIsPerformed();

        // Then
        ThenValidationFails();
        ThenErrorContains("Instance ID cannot be empty");
    }

    [Fact]
    public void Given_InstanceIdTooLong_When_Validated_Then_IsInvalid()
    {
        // Given
        GivenRequestWithInstanceIdTooLong();

        // When
        WhenValidationIsPerformed();

        // Then
        ThenValidationFails();
        ThenErrorContains("Instance ID must be maximum 64 characters");
    }

    [Fact]
    public void Given_InstanceIdWithInvalidCharacters_When_Validated_Then_IsInvalid()
    {
        // Given
        GivenRequestWithInvalidInstanceId("test@instance");

        // When
        WhenValidationIsPerformed();

        // Then
        ThenValidationFails();
        ThenErrorContains("Instance ID must contain only alphanumeric characters");
    }

    [Fact]
    public void Given_EmptyCorrelationId_When_Validated_Then_IsInvalid()
    {
        // Given
        GivenRequestWithEmptyCorrelationId();

        // When
        WhenValidationIsPerformed();

        // Then
        ThenValidationFails();
        ThenErrorContains("Correlation ID cannot be empty");
    }

    [Theory]
    [InlineData("valid-instance")]
    [InlineData("valid_instance")]
    [InlineData("valid123")]
    [InlineData("123-valid")]
    [InlineData("a")]
    public void Given_ValidInstanceIdFormat_When_Validated_Then_IsValid(string instanceId)
    {
        // Given
        GivenRequestWithInstanceId(instanceId);

        // When
        WhenValidationIsPerformed();

        // Then
        ThenValidationIsSuccessful();
    }

    // Given steps
    private void GivenValidRequest()
    {
        _request = new GetInstanceStatusRequest("test-instance", Guid.NewGuid().ToString());
    }

    private void GivenRequestWithEmptyInstanceId()
    {
        _request = new GetInstanceStatusRequest("", Guid.NewGuid().ToString());
    }

    private void GivenRequestWithInstanceIdTooLong()
    {
        _request = new GetInstanceStatusRequest(new string('a', 65), Guid.NewGuid().ToString());
    }

    private void GivenRequestWithInvalidInstanceId(string instanceId)
    {
        _request = new GetInstanceStatusRequest(instanceId, Guid.NewGuid().ToString());
    }

    private void GivenRequestWithEmptyCorrelationId()
    {
        _request = new GetInstanceStatusRequest("test-instance", "");
    }

    private void GivenRequestWithInstanceId(string instanceId)
    {
        _request = new GetInstanceStatusRequest(instanceId, Guid.NewGuid().ToString());
    }

    // When steps
    private void WhenValidationIsPerformed()
    {
        _validationResult = _validator.Validate(_request);
    }

    // Then steps
    private void ThenValidationIsSuccessful()
    {
        _validationResult.Should().NotBeNull();
        _validationResult.IsValid.Should().BeTrue();
    }

    private void ThenValidationFails()
    {
        _validationResult.Should().NotBeNull();
        _validationResult.IsValid.Should().BeFalse();
    }

    private void ThenErrorContains(string expectedError)
    {
        _validationResult.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedError));
    }
}
