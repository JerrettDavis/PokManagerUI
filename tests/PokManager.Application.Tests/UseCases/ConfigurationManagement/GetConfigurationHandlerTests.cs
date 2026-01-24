using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.ConfigurationManagement;

public class GetConfigurationHandlerTests
{
    private readonly FakePokManagerClient _fakeClient;
    private readonly GetConfigurationHandler _handler;

    public GetConfigurationHandlerTests()
    {
        _fakeClient = new FakePokManagerClient();
        _handler = new GetConfigurationHandler(_fakeClient);
    }

    [Fact]
    public async Task Given_ValidInstanceId_When_GetConfiguration_Then_ReturnsConfiguration()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "My Awesome Server",
            ["ServerPassword"] = "secret123",
            ["MaxPlayers"] = "32",
            ["ServerMap"] = "Palworld/Maps/DefaultMap",
            ["Difficulty"] = "Normal",
            ["DeathPenalty"] = "All"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.SessionName.Should().Be("My Awesome Server");
        result.Value.Configuration.MaxPlayers.Should().Be(32);
        result.Value.Configuration.ServerMap.Should().Be("Palworld/Maps/DefaultMap");
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_GetConfiguration_Then_ReturnsValidationFailure()
    {
        // Arrange
        var request = new GetConfigurationRequest("", Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_GetConfiguration_Then_ReturnsNotFoundFailure()
    {
        // Arrange - Don't set up the instance
        var request = new GetConfigurationRequest("nonexistent", Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("InstanceNotFound");
    }

    [Fact]
    public async Task Given_PokManagerFails_When_GetConfiguration_Then_ReturnsFailure()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);
        _fakeClient.FailNextOperation("Client connection failed");

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Client connection failed");
    }

    [Fact]
    public async Task Given_ValidInstance_When_GetConfiguration_Then_IncludesAllSettings()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Test Server",
            ["ServerPassword"] = "pass123",
            ["MaxPlayers"] = "16",
            ["ServerMap"] = "DefaultMap",
            ["Mods"] = "Mod1,Mod2,Mod3",
            ["CustomSetting1"] = "Value1",
            ["CustomSetting2"] = "Value2"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        var config = result.Value.Configuration;
        config.SessionName.Should().Be("Test Server");
        config.MaxPlayers.Should().Be(16);
        config.ServerMap.Should().Be("DefaultMap");
        config.Mods.Should().NotBeNull();
        config.Mods.Should().Contain("Mod1");
        config.Mods.Should().Contain("Mod2");
        config.Mods.Should().Contain("Mod3");
        config.CustomSettings.Should().ContainKey("CustomSetting1");
        config.CustomSettings["CustomSetting1"].Should().Be("Value1");
        config.CustomSettings.Should().ContainKey("CustomSetting2");
        config.CustomSettings["CustomSetting2"].Should().Be("Value2");
    }

    [Fact]
    public async Task Given_IncludeSecretsTrue_When_GetConfiguration_Then_IncludesServerPassword()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Secure Server",
            ["ServerPassword"] = "supersecret456",
            ["MaxPlayers"] = "32"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.ServerPassword.Should().Be("supersecret456");
    }

    [Fact]
    public async Task Given_IncludeSecretsFalse_When_GetConfiguration_Then_MasksServerPassword()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Secure Server",
            ["ServerPassword"] = "supersecret456",
            ["MaxPlayers"] = "32"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.ServerPassword.Should().Be("********");
    }

    [Fact]
    public async Task Given_ConfigurationWithMods_When_GetConfiguration_Then_ReturnsModsList()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Modded Server",
            ["ServerPassword"] = "pass",
            ["MaxPlayers"] = "32",
            ["Mods"] = "ModA,ModB,ModC"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.Mods.Should().NotBeNull();
        result.Value.Configuration.Mods.Should().BeEquivalentTo(new[] { "ModA", "ModB", "ModC" });
    }

    [Fact]
    public async Task Given_ConfigurationWithCustomSettings_When_GetConfiguration_Then_ReturnsCustomSettings()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Custom Server",
            ["ServerPassword"] = "pass",
            ["MaxPlayers"] = "32",
            ["CustomKey1"] = "CustomValue1",
            ["CustomKey2"] = "CustomValue2"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.CustomSettings.Should().ContainKey("CustomKey1");
        result.Value.Configuration.CustomSettings["CustomKey1"].Should().Be("CustomValue1");
        result.Value.Configuration.CustomSettings.Should().ContainKey("CustomKey2");
        result.Value.Configuration.CustomSettings["CustomKey2"].Should().Be("CustomValue2");
    }

    [Fact]
    public async Task Given_EmptyConfiguration_When_GetConfiguration_Then_ReturnsEmptyConfiguration()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>());

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        var config = result.Value.Configuration;
        config.SessionName.Should().BeEmpty();
        config.ServerPassword.Should().Be("********"); // Even empty configs should mask the password
        config.MaxPlayers.Should().Be(0);
        config.ServerMap.Should().BeEmpty();
        config.Mods.Should().BeEmpty();
        config.CustomSettings.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_DefaultIncludeSecretsValue_When_GetConfiguration_Then_MasksSecrets()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Default Test",
            ["ServerPassword"] = "defaultpass123",
            ["MaxPlayers"] = "32"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString()); // Default IncludeSecrets = false

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.ServerPassword.Should().Be("********");
    }

    [Fact]
    public async Task Given_ModsWithSpaces_When_GetConfiguration_Then_TrimsModNames()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Modded Server",
            ["ServerPassword"] = "pass",
            ["MaxPlayers"] = "32",
            ["Mods"] = " ModA , ModB , ModC "
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.Mods.Should().BeEquivalentTo(new[] { "ModA", "ModB", "ModC" });
    }

    [Fact]
    public async Task Given_InvalidMaxPlayersValue_When_GetConfiguration_Then_ReturnsZero()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstanceWithConfiguration(instanceId, new Dictionary<string, string>
        {
            ["SessionName"] = "Test Server",
            ["ServerPassword"] = "pass",
            ["MaxPlayers"] = "not_a_number"
        });

        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString(), IncludeSecrets: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Configuration.MaxPlayers.Should().Be(0);
    }

    #region Helper Methods

    private void SetupInstance(string instanceId)
    {
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            InstanceState.Running,
            ProcessHealth.Healthy,
            8211,
            32,
            0,
            "1.0.0",
            TimeSpan.FromHours(1),
            $"/opt/palworld/{instanceId}",
            $"/opt/palworld/{instanceId}/world",
            $"/opt/palworld/{instanceId}/config",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            null,
            new Dictionary<string, string>()));
    }

    private void SetupInstanceWithConfiguration(string instanceId, Dictionary<string, string> configuration)
    {
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            InstanceState.Running,
            ProcessHealth.Healthy,
            8211,
            32,
            0,
            "1.0.0",
            TimeSpan.FromHours(1),
            $"/opt/palworld/{instanceId}",
            $"/opt/palworld/{instanceId}/world",
            $"/opt/palworld/{instanceId}/config",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            null,
            configuration));
    }

    #endregion
}
