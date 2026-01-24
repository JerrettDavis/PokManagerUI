using FluentValidation;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.InstanceLifecycle.StartInstance;

/// <summary>
/// Handler for starting a stopped Palworld server instance.
/// Implements railway-oriented programming with Result monad pattern.
/// </summary>
public class StartInstanceHandler(
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
    private readonly StartInstanceRequestValidator _validator = new();

    /// <summary>
    /// Handles the StartInstance request with full orchestration:
    /// 1. Validate request
    /// 2. Acquire operation lock
    /// 3. Check current instance status
    /// 4. Call PokManager to start instance
    /// 5. Create audit event
    /// 6. Release lock
    /// </summary>
    public async Task<Result<StartInstanceResponse>> Handle(
        StartInstanceRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = _clock.UtcNow;
        IOperationLock? operationLock = null;

        try
        {
            // Step 1: Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result.Failure<StartInstanceResponse>(errors);
            }

            // Step 2: Acquire operation lock
            var lockResult = await _lockManager.AcquireLockAsync(
                request.InstanceId,
                request.CorrelationId,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            if (lockResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    "StartInstance",
                    "Failure",
                    startTime,
                    lockResult.Error,
                    cancellationToken);

                return Result.Failure<StartInstanceResponse>(lockResult.Error);
            }

            operationLock = lockResult.Value;

            // Step 3: Check current instance status
            var statusResult = await _pokManagerClient.GetInstanceStatusAsync(
                request.InstanceId,
                cancellationToken);

            if (statusResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    "StartInstance",
                    "Failure",
                    startTime,
                    statusResult.Error,
                    cancellationToken);

                return Result.Failure<StartInstanceResponse>(statusResult.Error);
            }

            var currentState = statusResult.Value.State;

            // Check if instance is already running
            if (currentState == InstanceState.Running)
            {
                const string error = "InstanceAlreadyRunning";
                await EmitAuditEventAsync(
                    request.InstanceId,
                    "StartInstance",
                    "Failure",
                    startTime,
                    error,
                    cancellationToken);

                return Result.Failure<StartInstanceResponse>(error);
            }

            // Step 4: Call PokManager to start the instance
            var startResult = await _pokManagerClient.StartInstanceAsync(
                request.InstanceId,
                cancellationToken);

            if (startResult.IsFailure)
            {
                await EmitAuditEventAsync(
                    request.InstanceId,
                    "StartInstance",
                    "Failure",
                    startTime,
                    startResult.Error,
                    cancellationToken);

                return Result.Failure<StartInstanceResponse>(startResult.Error);
            }

            // Step 5: Create success audit event
            await EmitAuditEventAsync(
                request.InstanceId,
                "StartInstance",
                "Success",
                startTime,
                null,
                cancellationToken);

            // Step 6: Invalidate cache and trigger refresh
            await _cacheInvalidation.InvalidateInstanceAsync(request.InstanceId, cancellationToken);

            // Step 7: Return success response
            var response = new StartInstanceResponse(
                request.InstanceId,
                "Instance started successfully");

            return Result<StartInstanceResponse>.Success(response);
        }
        finally
        {
            // Always release the lock if acquired
            if (operationLock != null)
            {
                await operationLock.DisposeAsync();
            }
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
        var duration = _clock.UtcNow - startTime;
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            instanceId,
            operationType,
            "System", // TODO: Get actual user/service identity
            _clock.UtcNow,
            outcome,
            duration,
            null,
            errorMessage);

        await _auditSink.EmitAsync(auditEvent, cancellationToken);
    }
}
