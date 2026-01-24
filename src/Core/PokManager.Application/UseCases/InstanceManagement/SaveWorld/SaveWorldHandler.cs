using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using System.Diagnostics;

namespace PokManager.Application.UseCases.InstanceManagement.SaveWorld;

/// <summary>
/// Handler for saving the world state of a running instance.
/// Implements full orchestration with locking, state validation, client interaction, and auditing.
/// </summary>
public class SaveWorldHandler(
    IPokManagerClient pokManagerClient,
    IOperationLockManager lockManager,
    IAuditSink auditSink,
    IClock clock
)
{
    private readonly IPokManagerClient _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
    private readonly IOperationLockManager _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    private readonly IAuditSink _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly SaveWorldRequestValidator _validator = new();

    /// <summary>
    /// Handles the request to save the world state.
    /// Pattern: Validate → Lock → Check State → Call Client → Audit → Unlock
    /// </summary>
    /// <param name="request">The request containing the instance ID and correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the save world response or an error.</returns>
    public async Task<Result<SaveWorldResponse>> Handle(
        SaveWorldRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = _clock.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<SaveWorldResponse>(errors);
        }

        // Step 2: Acquire lock to prevent concurrent operations
        var lockResult = await _lockManager.AcquireLockAsync(
            request.InstanceId,
            request.CorrelationId,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (lockResult.IsFailure)
        {
            return Result.Failure<SaveWorldResponse>(lockResult.Error);
        }

        IOperationLock? acquiredLock = lockResult.Value;

        try
        {
            // Step 3: Check instance state - must be running
            var statusResult = await _pokManagerClient.GetInstanceStatusAsync(
                request.InstanceId,
                cancellationToken);

            if (statusResult.IsFailure)
            {
                await EmitAuditEventAsync(request, "Failure", stopwatch.Elapsed, statusResult.Error);
                return Result.Failure<SaveWorldResponse>(statusResult.Error);
            }

            var instanceStatus = statusResult.Value;
            if (instanceStatus.State != InstanceState.Running)
            {
                var errorMessage = $"Cannot save world for instance '{request.InstanceId}' because it is not running (current state: {instanceStatus.State})";
                await EmitAuditEventAsync(request, "Failure", stopwatch.Elapsed, errorMessage);
                return Result.Failure<SaveWorldResponse>(errorMessage);
            }

            // Step 4: Call the POK Manager client to save the world
            var saveResult = await _pokManagerClient.SaveWorldAsync(
                request.InstanceId,
                cancellationToken);

            if (saveResult.IsFailure)
            {
                await EmitAuditEventAsync(request, "Failure", stopwatch.Elapsed, saveResult.Error);
                return Result.Failure<SaveWorldResponse>(saveResult.Error);
            }

            // Step 5: Create success response
            var response = new SaveWorldResponse(
                request.InstanceId,
                _clock.UtcNow,
                "World saved successfully");

            // Step 6: Emit success audit event
            stopwatch.Stop();
            await EmitAuditEventAsync(request, "Success", stopwatch.Elapsed, null);

            return Result<SaveWorldResponse>.Success(response);
        }
        finally
        {
            // Step 7: Always release the lock
            if (acquiredLock != null)
            {
                await acquiredLock.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Emits an audit event for the save world operation.
    /// </summary>
    private async Task EmitAuditEventAsync(
        SaveWorldRequest request,
        string outcome,
        TimeSpan duration,
        string? errorMessage)
    {
        var auditEvent = new AuditEvent(
            EventId: Guid.NewGuid(),
            InstanceId: request.InstanceId,
            OperationType: "SaveWorld",
            PerformedBy: "System",
            PerformedAt: _clock.UtcNow,
            Outcome: outcome,
            Duration: duration,
            Details: new Dictionary<string, string>
            {
                ["CorrelationId"] = request.CorrelationId
            },
            ErrorMessage: errorMessage
        );

        await _auditSink.EmitAsync(auditEvent);
    }
}
