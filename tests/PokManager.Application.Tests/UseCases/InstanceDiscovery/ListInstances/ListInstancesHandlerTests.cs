using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.InstanceDiscovery.ListInstances;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceDiscovery.ListInstances;

/// <summary>
/// Tests for ListInstancesHandler using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class ListInstancesHandlerTests
{
    private readonly InMemoryInstanceDiscoveryService _discoveryService = new();
    private readonly FakePokManagerClient _fakeClient = new();
    private ListInstancesHandler _handler = null!;
    private Result<ListInstancesResponse> _result = null!;

    private void InitializeHandler()
    {
        _handler = new ListInstancesHandler(_discoveryService, _fakeClient);
    }

    [Fact]
    public async Task Given_NoInstances_When_List_Then_EmptyList()
    {
        // Given
        GivenNoInstancesExist();

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListIsEmpty();
    }

    [Fact]
    public async Task Given_SingleInstance_When_List_Then_ReturnsOneInstance()
    {
        // Given
        GivenAnInstanceExists("island_main", InstanceState.Running);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListContainsInstances(1);
        ThenTheInstanceIsIncluded("island_main");
    }

    [Fact]
    public async Task Given_MultipleInstances_When_List_Then_ReturnsAll()
    {
        // Given
        GivenAnInstanceExists("island_main", InstanceState.Running);
        GivenAnInstanceExists("desert_backup", InstanceState.Stopped);
        GivenAnInstanceExists("forest_dev", InstanceState.Starting);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListContainsInstances(3);
        ThenTheInstanceIsIncluded("island_main");
        ThenTheInstanceIsIncluded("desert_backup");
        ThenTheInstanceIsIncluded("forest_dev");
    }

    [Fact]
    public async Task Given_InstancesExist_When_List_Then_IncludesAllStates()
    {
        // Given
        GivenAnInstanceExists("inst_created", InstanceState.Created);
        GivenAnInstanceExists("inst_starting", InstanceState.Starting);
        GivenAnInstanceExists("inst_running", InstanceState.Running);
        GivenAnInstanceExists("inst_stopping", InstanceState.Stopping);
        GivenAnInstanceExists("inst_stopped", InstanceState.Stopped);
        GivenAnInstanceExists("inst_restarting", InstanceState.Restarting);
        GivenAnInstanceExists("inst_failed", InstanceState.Failed);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListContainsInstances(7);
        ThenTheInstanceHasState("inst_created", InstanceState.Created);
        ThenTheInstanceHasState("inst_starting", InstanceState.Starting);
        ThenTheInstanceHasState("inst_running", InstanceState.Running);
        ThenTheInstanceHasState("inst_stopping", InstanceState.Stopping);
        ThenTheInstanceHasState("inst_stopped", InstanceState.Stopped);
        ThenTheInstanceHasState("inst_restarting", InstanceState.Restarting);
        ThenTheInstanceHasState("inst_failed", InstanceState.Failed);
    }

    [Fact]
    public async Task Given_InstanceWithDetails_When_List_Then_IncludesHealth()
    {
        // Given
        GivenAnInstanceExistsWithDetails("island_main", InstanceState.Running, ProcessHealth.Healthy);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheInstanceHasHealth("island_main", ProcessHealth.Healthy);
    }

    [Fact]
    public async Task Given_RunningInstance_When_List_Then_IncludesLastStartedAt()
    {
        // Given
        var lastStartedAt = DateTimeOffset.UtcNow.AddHours(-2);
        GivenAnInstanceExistsWithStartTime("island_main", InstanceState.Running, lastStartedAt);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheInstanceHasLastStartedAt("island_main", lastStartedAt);
    }

    [Fact]
    public async Task Given_StoppedInstance_When_List_Then_LastStartedAtIsNull()
    {
        // Given
        GivenAnInstanceExistsWithNoStartTime("island_main", InstanceState.Stopped);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheInstanceHasNoLastStartedAt("island_main");
    }

    [Fact]
    public async Task Given_PokManagerClientFailsForAllInstances_When_List_Then_ReturnsEmptyList()
    {
        // Given
        // Add instance to discovery but don't set it up in client
        // This simulates all instances failing to load details
        _discoveryService.AddInstance("island_main");
        InitializeHandler();

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListIsEmpty(); // All instances were skipped due to failures
    }

    [Fact]
    public async Task Given_InstanceDetailsFails_When_List_Then_SkipsFailedInstance()
    {
        // Given
        GivenAnInstanceExists("island_main", InstanceState.Running);
        GivenAnInstanceExistsButDetailsWillFail("broken_instance");

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListContainsInstances(1);
        ThenTheInstanceIsIncluded("island_main");
        ThenTheInstanceIsNotIncluded("broken_instance");
    }

    [Fact]
    public async Task Given_MixedHealthStates_When_List_Then_IncludesAllHealthStates()
    {
        // Given
        GivenAnInstanceExistsWithDetails("inst_healthy", InstanceState.Running, ProcessHealth.Healthy);
        GivenAnInstanceExistsWithDetails("inst_degraded", InstanceState.Running, ProcessHealth.Degraded);
        GivenAnInstanceExistsWithDetails("inst_unhealthy", InstanceState.Running, ProcessHealth.Unhealthy);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheListContainsInstances(3);
        ThenTheInstanceHasHealth("inst_healthy", ProcessHealth.Healthy);
        ThenTheInstanceHasHealth("inst_degraded", ProcessHealth.Degraded);
        ThenTheInstanceHasHealth("inst_unhealthy", ProcessHealth.Unhealthy);
    }

    [Fact]
    public async Task Given_ValidRequest_When_List_Then_ResponseContainsCorrectInstanceIds()
    {
        // Given
        GivenAnInstanceExists("island_alpha", InstanceState.Running);
        GivenAnInstanceExists("island_beta", InstanceState.Stopped);

        // When
        await WhenListInstancesIsCalled();

        // Then
        ThenTheResultIsSuccess();
        ThenTheInstanceIdMatches("island_alpha", "island_alpha");
        ThenTheInstanceIdMatches("island_beta", "island_beta");
    }

    #region Given Steps

    private void GivenNoInstancesExist()
    {
        _discoveryService.Reset();
        _fakeClient.Reset();
        InitializeHandler();
    }

    private void GivenAnInstanceExists(string instanceId, InstanceState state)
    {
        _discoveryService.AddInstance(instanceId);
        _fakeClient.SetupInstance(instanceId, state);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            state,
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
            state == InstanceState.Running || state == InstanceState.Starting || state == InstanceState.Restarting
                ? DateTimeOffset.UtcNow.AddHours(-1)
                : null,
            state == InstanceState.Stopped ? DateTimeOffset.UtcNow.AddMinutes(-30) : null,
            new Dictionary<string, string>()));
        InitializeHandler();
    }

    private void GivenAnInstanceExistsWithDetails(string instanceId, InstanceState state, ProcessHealth health)
    {
        _discoveryService.AddInstance(instanceId);
        _fakeClient.SetupInstance(instanceId, state);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            state,
            health,
            8211,
            32,
            0,
            "1.0.0",
            TimeSpan.FromHours(1),
            $"/opt/palworld/{instanceId}",
            $"/opt/palworld/{instanceId}/world",
            $"/opt/palworld/{instanceId}/config",
            DateTimeOffset.UtcNow.AddDays(-1),
            state == InstanceState.Running ? DateTimeOffset.UtcNow.AddHours(-1) : null,
            null,
            new Dictionary<string, string>()));
        InitializeHandler();
    }

    private void GivenAnInstanceExistsWithStartTime(string instanceId, InstanceState state, DateTimeOffset lastStartedAt)
    {
        _discoveryService.AddInstance(instanceId);
        _fakeClient.SetupInstance(instanceId, state);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            state,
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
            lastStartedAt,
            null,
            new Dictionary<string, string>()));
        InitializeHandler();
    }

    private void GivenAnInstanceExistsWithNoStartTime(string instanceId, InstanceState state)
    {
        _discoveryService.AddInstance(instanceId);
        _fakeClient.SetupInstance(instanceId, state);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            state,
            ProcessHealth.Healthy,
            8211,
            32,
            0,
            "1.0.0",
            null,
            $"/opt/palworld/{instanceId}",
            $"/opt/palworld/{instanceId}/world",
            $"/opt/palworld/{instanceId}/config",
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            null,
            new Dictionary<string, string>()));
        InitializeHandler();
    }

    private void GivenAnInstanceExistsButDetailsWillFail(string instanceId)
    {
        _discoveryService.AddInstance(instanceId);
        // Don't setup in fake client - this will cause GetInstanceDetailsAsync to fail
        InitializeHandler();
    }

    private void GivenPokManagerClientWillFail(string errorMessage)
    {
        _fakeClient.FailNextOperation(errorMessage);
        InitializeHandler();
    }

    #endregion

    #region When Steps

    private async Task WhenListInstancesIsCalled()
    {
        var request = new ListInstancesRequest();
        _result = await _handler.Handle(request, CancellationToken.None);
    }

    #endregion

    #region Then Steps

    private void ThenTheResultIsSuccess()
    {
        _result.Should().NotBeNull();
        _result.IsSuccess.Should().BeTrue();
    }

    private void ThenTheResultIsFailure()
    {
        _result.Should().NotBeNull();
        _result.IsFailure.Should().BeTrue();
    }

    private void ThenTheErrorContains(string expectedError)
    {
        _result.Error.Should().Contain(expectedError);
    }

    private void ThenTheListIsEmpty()
    {
        _result.Value.Instances.Should().BeEmpty();
    }

    private void ThenTheListContainsInstances(int expectedCount)
    {
        _result.Value.Instances.Should().HaveCount(expectedCount);
    }

    private void ThenTheInstanceIsIncluded(string instanceId)
    {
        _result.Value.Instances.Should().Contain(i => i.Id == instanceId);
    }

    private void ThenTheInstanceIsNotIncluded(string instanceId)
    {
        _result.Value.Instances.Should().NotContain(i => i.Id == instanceId);
    }

    private void ThenTheInstanceHasState(string instanceId, InstanceState expectedState)
    {
        var instance = _result.Value.Instances.FirstOrDefault(i => i.Id == instanceId);
        instance.Should().NotBeNull();
        instance!.State.Should().Be(expectedState);
    }

    private void ThenTheInstanceHasHealth(string instanceId, ProcessHealth expectedHealth)
    {
        var instance = _result.Value.Instances.FirstOrDefault(i => i.Id == instanceId);
        instance.Should().NotBeNull();
        instance!.Health.Should().Be(expectedHealth);
    }

    private void ThenTheInstanceHasLastStartedAt(string instanceId, DateTimeOffset expectedLastStartedAt)
    {
        var instance = _result.Value.Instances.FirstOrDefault(i => i.Id == instanceId);
        instance.Should().NotBeNull();
        instance!.LastStartedAt.Should().NotBeNull();
        instance!.LastStartedAt.Should().BeCloseTo(expectedLastStartedAt, TimeSpan.FromSeconds(1));
    }

    private void ThenTheInstanceHasNoLastStartedAt(string instanceId)
    {
        var instance = _result.Value.Instances.FirstOrDefault(i => i.Id == instanceId);
        instance.Should().NotBeNull();
        instance!.LastStartedAt.Should().BeNull();
    }

    private void ThenTheInstanceIdMatches(string instanceId, string expectedId)
    {
        var instance = _result.Value.Instances.FirstOrDefault(i => i.Id == instanceId);
        instance.Should().NotBeNull();
        instance!.Id.Should().Be(expectedId);
    }

    #endregion
}
