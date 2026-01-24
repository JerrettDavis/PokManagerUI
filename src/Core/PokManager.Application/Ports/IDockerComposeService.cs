using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Service for managing Docker Compose operations.
/// Orchestrates docker-compose commands for container lifecycle management.
/// </summary>
public interface IDockerComposeService
{
    /// <summary>
    /// Creates and starts containers defined in a docker-compose file.
    /// </summary>
    /// <param name="dockerComposeFilePath">Absolute path to the docker-compose.yaml file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result<Unit>> UpAsync(string dockerComposeFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops and removes containers defined in a docker-compose file.
    /// </summary>
    /// <param name="dockerComposeFilePath">Absolute path to the docker-compose.yaml file.</param>
    /// <param name="removeVolumes">If true, removes named volumes declared in the compose file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result<Unit>> DownAsync(
        string dockerComposeFilePath,
        bool removeVolumes = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a docker-compose file for syntax errors.
    /// </summary>
    /// <param name="dockerComposeFilePath">Absolute path to the docker-compose.yaml file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating if the file is valid.</returns>
    Task<Result<Unit>> ValidateAsync(string dockerComposeFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of containers defined in a docker-compose file.
    /// </summary>
    /// <param name="dockerComposeFilePath">Absolute path to the docker-compose.yaml file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing container status information.</returns>
    Task<Result<string>> GetStatusAsync(
        string dockerComposeFilePath,
        CancellationToken cancellationToken = default);
}
