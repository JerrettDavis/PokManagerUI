using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Entities;

public class Instance(string instanceId, string sessionName, string mapName, int maxPlayers)
{
    public string InstanceId { get; private set; } = instanceId;
    public string SessionName { get; private set; } = sessionName;
    public string MapName { get; private set; } = mapName;
    public int MaxPlayers { get; private set; } = maxPlayers;
    public InstanceState State { get; set; } = InstanceState.Created;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastStartedAt { get; private set; }
    public string? ContainerId { get; set; }

    // State transition rules
    private static readonly Dictionary<InstanceState, HashSet<InstanceState>> s_allowedTransitions = new()
    {
        [InstanceState.Created] = new() { InstanceState.Starting },
        [InstanceState.Stopped] = new() { InstanceState.Starting, InstanceState.Deleted },
        [InstanceState.Starting] = new() { InstanceState.Running, InstanceState.Stopped },
        [InstanceState.Running] = new() { InstanceState.Stopping, InstanceState.Restarting },
        [InstanceState.Stopping] = new() { InstanceState.Stopped },
        [InstanceState.Restarting] = new() { InstanceState.Running, InstanceState.Stopped },
        [InstanceState.Deleted] = new() { }
    };

    public Result<Unit> TransitionTo(InstanceState newState)
    {
        if (!s_allowedTransitions.TryGetValue(State, out var allowedStates))
        {
            return Result.Failure<Unit>($"No transitions defined for state {State}");
        }

        if (!allowedStates.Contains(newState))
        {
            return Result.Failure<Unit>($"Invalid state transition from {State} to {newState}");
        }

        State = newState;

        if (newState == InstanceState.Running)
        {
            LastStartedAt = DateTimeOffset.UtcNow;
        }

        return Result.Success();
    }
}
