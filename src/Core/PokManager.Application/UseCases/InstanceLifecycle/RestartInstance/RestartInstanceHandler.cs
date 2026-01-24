using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;

/// <summary>
/// Handler for restarting a Palworld server instance.
/// This is a MUTATING use-case that changes instance state.
/// </summary>
public class RestartInstanceHandler(
    IPokManagerClient pokManagerClient,
    IOperationLockManager lockManager,
    IAuditSink auditSink,
    IClock clock,
    ICacheInvalidationService cacheInvalidation
)
{
    private readonly IPokManagerClient _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
    private readonly IOperationLockManager _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    private readonly IAuditSink _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ICacheInvalidationService _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    private readonly RestartInstanceRequestValidator _validator = new();

    /// <summary>
    /// Handles the request to restart an instance.
    /// </summary>
    /// <param name="request">The request containing the instance ID and restart options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the restart response or an error.</returns>
    public async Task<Result<RestartInstanceResponse>> Handle(
        RestartInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var operationStart = _clock.UtcNow;

        // Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<RestartInstanceResponse>(errors);
        }

        // Acquire lock to prevent concurrent operations
        var lockResult = await _lockManager.AcquireLockAsync(
            request.InstanceId,
            request.CorrelationId,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (lockResult.IsFailure)
        {
            return Result.Failure<RestartInstanceResponse>(
                $"Failed to acquire lock for instance {request.InstanceId}: {lockResult.Error}");
        }

        var operationLock = lockResult.Value;

        try
        {
            // Check current instance state
            var statusResult = await _pokManagerClient.GetInstanceStatusAsync(
                request.InstanceId,
                cancellationToken);

            if (statusResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    request.CorrelationId,
                    "Failure",
                    operationStart,
                    statusResult.Error,
                    cancellationToken);

                return Result.Failure<RestartInstanceResponse>(statusResult.Error);
            }

            var currentState = statusResult.Value.State;

            // Validate that instance is in a restartable state
            if (currentState == InstanceState.Stopped)
            {
                var error = $"Instance {request.InstanceId} cannot restart a stopped instance. Use Start instead.";

                await EmitAuditEventAsync(
                    request.InstanceId,
                    request.CorrelationId,
                    "Failure",
                    operationStart,
                    error,
                    cancellationToken);

                return Result.Failure<RestartInstanceResponse>(error);
            }

            // Build restart options
            var options = new RestartInstanceOptions(
                Graceful: request.GracePeriodSeconds > 0,
                SaveWorld: request.SaveWorld,
                WaitForHealthy: request.WaitForHealthy,
                Timeout: request.GracePeriodSeconds > 0
                    ? TimeSpan.FromSeconds(request.GracePeriodSeconds)
                    : null);

            // Call POK Manager client to restart instance
            var restartResult = await _pokManagerClient.RestartInstanceAsync(
                request.InstanceId,
                options,
                cancellationToken);

            if (restartResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    request.CorrelationId,
                    "Failure",
                    operationStart,
                    restartResult.Error,
                    cancellationToken);

                return Result.Failure<RestartInstanceResponse>(restartResult.Error);
            }

            // Create successful response
            var response = new RestartInstanceResponse(
                request.InstanceId,
                "Instance restarted successfully",
                _clock.UtcNow);

            // Emit success audit event
            await EmitAuditEventAsync(
                request.InstanceId,
                request.CorrelationId,
                "Success",
                operationStart,
                null,
                cancellationToken);

            // Invalidate cache and trigger refresh
            await _cacheInvalidation.InvalidateInstanceAsync(request.InstanceId, cancellationToken);

            return Result<RestartInstanceResponse>.Success(response);
        }
        finally
        {
            // Always release the lock
            await operationLock.DisposeAsync();
        }
    }

    private async Task EmitAuditEventAsync(
        string instanceId,
        string correlationId,
        string outcome,
        DateTimeOffset operationStart,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var operationEnd = _clock.UtcNow;
        var duration = operationEnd - operationStart;

        var auditEvent = new AuditEvent(
            EventId: Guid.NewGuid(),
            InstanceId: instanceId,
            OperationType: "RestartInstance",
            PerformedBy: "System",
            PerformedAt: operationEnd,
            Outcome: outcome,
            Duration: duration,
            Details: new Dictionary<string, string>
            {
                ["CorrelationId"] = correlationId
            },
            ErrorMessage: errorMessage);

        await _auditSink.EmitAsync(auditEvent, cancellationToken);
    }
}
