using FluentValidation;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.InstanceLifecycle.StopInstance;

/// <summary>
/// Handler for stopping a Palworld server instance.
/// Orchestrates validation, locking, client calls, and audit event creation.
/// </summary>
public class StopInstanceHandler(
    IPokManagerClient pokManagerClient,
    IOperationLockManager lockManager,
    IAuditSink auditSink,
    IClock clock,
    ICacheInvalidationService cacheInvalidation
)
{
    private readonly StopInstanceRequestValidator _validator = new();

    /// <summary>
    /// Handles the StopInstance request.
    /// </summary>
    public async Task<Result<StopInstanceResponse>> Handle(
        StopInstanceRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = clock.UtcNow;

        // Step 1: Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<StopInstanceResponse>(errors);
        }

        // Step 2: Acquire operation lock
        var lockResult = await lockManager.AcquireLockAsync(
            request.InstanceId,
            request.CorrelationId,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (lockResult.IsFailure)
        {
            await EmitAuditEventAsync(
                request.InstanceId,
                "StopInstance",
                "Failure",
                startTime,
                lockResult.Error,
                cancellationToken);

            return Result.Failure<StopInstanceResponse>(lockResult.Error);
        }

        await using var opLock = lockResult.Value;

        try
        {
            // Step 3: Check current instance status
            var statusResult = await pokManagerClient.GetInstanceStatusAsync(
                request.InstanceId,
                cancellationToken);

            if (statusResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    "StopInstance",
                    "Failure",
                    startTime,
                    statusResult.Error,
                    cancellationToken);

                return Result.Failure<StopInstanceResponse>(statusResult.Error);
            }

            // Step 4: Build stop options from request
            var stopOptions = new StopInstanceOptions(
                Graceful: !request.ForceKill,
                Timeout: TimeSpan.FromSeconds(request.TimeoutSeconds),
                SaveWorld: true);

            // Step 5: Call PokManagerClient to stop the instance
            var stopResult = await pokManagerClient.StopInstanceAsync(
                request.InstanceId,
                stopOptions,
                cancellationToken);

            if (stopResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    "StopInstance",
                    "Failure",
                    startTime,
                    stopResult.Error,
                    cancellationToken);

                return Result.Failure<StopInstanceResponse>(stopResult.Error);
            }

            // Step 6: Create success audit event
            await EmitAuditEventAsync(
                request.InstanceId,
                "StopInstance",
                "Success",
                startTime,
                null,
                cancellationToken);

            // Step 7: Invalidate cache and trigger refresh
            await cacheInvalidation.InvalidateInstanceAsync(request.InstanceId, cancellationToken);

            // Step 8: Return success response
            var response = new StopInstanceResponse(
                request.InstanceId,
                "Instance stopped successfully");

            return Result<StopInstanceResponse>.Success(response);
        }
        catch (Exception ex)
        {
            await EmitAuditEventAsync(
                request.InstanceId,
                "StopInstance",
                "Failure",
                startTime,
                ex.Message,
                cancellationToken);

            return Result.Failure<StopInstanceResponse>($"Unexpected error: {ex.Message}");
        }
    }

    private async Task EmitAuditEventAsync(
        string instanceId,
        string operationType,
        string outcome,
        DateTimeOffset startTime,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var duration = clock.UtcNow - startTime;

        var auditEvent = new AuditEvent(
            EventId: Guid.NewGuid(),
            InstanceId: instanceId,
            OperationType: operationType,
            PerformedBy: "System",
            PerformedAt: clock.UtcNow,
            Outcome: outcome,
            Duration: duration,
            Details: null,
            ErrorMessage: errorMessage);

        await auditSink.EmitAsync(auditEvent, cancellationToken);
    }
}
