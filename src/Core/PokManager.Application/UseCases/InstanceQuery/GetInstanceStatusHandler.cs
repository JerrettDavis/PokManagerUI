using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.InstanceQuery;

/// <summary>
/// Handler for getting the status of a specific instance.
/// </summary>
public class GetInstanceStatusHandler(IPokManagerClient pokManagerClient)
{
    private readonly IPokManagerClient _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
    private readonly GetInstanceStatusRequestValidator _validator = new();

    /// <summary>
    /// Handles the request to get instance status.
    /// </summary>
    /// <param name="request">The request containing the instance ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the instance status response or an error.</returns>
    public async Task<Result<GetInstanceStatusResponse>> Handle(
        GetInstanceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<GetInstanceStatusResponse>(errors);
        }

        // Call POK Manager client to get instance status
        var statusResult = await _pokManagerClient.GetInstanceStatusAsync(
            request.InstanceId,
            cancellationToken);

        if (statusResult.IsFailure)
        {
            return Result.Failure<GetInstanceStatusResponse>(statusResult.Error);
        }

        // Map the InstanceStatus to InstanceStatusDto
        var status = statusResult.Value;
        var statusDto = new InstanceStatusDto(
            Id: status.InstanceId,
            State: status.State,
            LastCheckedAt: status.LastUpdated,
            ContainerId: null, // Not available in InstanceStatus model
            Health: status.Health,
            Uptime: status.Uptime,
            PlayerCount: status.PlayerCount,
            MaxPlayers: status.MaxPlayers,
            Version: status.Version);

        var response = new GetInstanceStatusResponse(statusDto);

        return Result<GetInstanceStatusResponse>.Success(response);
    }
}
