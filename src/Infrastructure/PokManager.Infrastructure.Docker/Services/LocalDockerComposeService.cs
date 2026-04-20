using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.Docker.Services;

/// <summary>
/// Local implementation of IDockerComposeService that executes docker-compose commands.
/// </summary>
public class LocalDockerComposeService : IDockerComposeService
{
    private readonly ILogger<LocalDockerComposeService> _logger;

    public LocalDockerComposeService(ILogger<LocalDockerComposeService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<Unit>> UpAsync(string dockerComposeFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating and starting container from {FilePath}", dockerComposeFilePath);

        if (!File.Exists(dockerComposeFilePath))
        {
            return Result.Failure<Unit>($"Docker compose file not found: {dockerComposeFilePath}");
        }

        // Validate first
        var validateResult = await ValidateAsync(dockerComposeFilePath, cancellationToken);
        if (validateResult.IsFailure)
        {
            return Result.Failure<Unit>($"Docker compose file is invalid: {validateResult.Error}");
        }

        var result = await ExecuteDockerComposeAsync(
            dockerComposeFilePath,
            "up -d",
            "Create and start container",
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Container created and started successfully from {FilePath}", dockerComposeFilePath);
        }

        return result;
    }

    public async Task<Result<Unit>> DownAsync(
        string dockerComposeFilePath,
        bool removeVolumes = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping and removing container from {FilePath} (removeVolumes: {RemoveVolumes})",
            dockerComposeFilePath, removeVolumes);

        if (!File.Exists(dockerComposeFilePath))
        {
            return Result.Failure<Unit>($"Docker compose file not found: {dockerComposeFilePath}");
        }

        var arguments = removeVolumes ? "down -v" : "down";

        var result = await ExecuteDockerComposeAsync(
            dockerComposeFilePath,
            arguments,
            "Stop and remove container",
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Container stopped and removed from {FilePath}", dockerComposeFilePath);
        }

        return result;
    }

    public async Task<Result<Unit>> ValidateAsync(string dockerComposeFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating docker-compose file: {FilePath}", dockerComposeFilePath);

        if (!File.Exists(dockerComposeFilePath))
        {
            return Result.Failure<Unit>($"Docker compose file not found: {dockerComposeFilePath}");
        }

        var result = await ExecuteDockerComposeAsync(
            dockerComposeFilePath,
            "config --quiet",
            "Validate docker-compose file",
            cancellationToken);

        return result;
    }

    public async Task<Result<string>> GetStatusAsync(
        string dockerComposeFilePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting status for containers in {FilePath}", dockerComposeFilePath);

        if (!File.Exists(dockerComposeFilePath))
        {
            return Result.Failure<string>($"Docker compose file not found: {dockerComposeFilePath}");
        }

        var (exitCode, output, error) = await ExecuteCommandAsync(
            "docker-compose",
            $"-f \"{dockerComposeFilePath}\" ps",
            cancellationToken);

        if (exitCode != 0)
        {
            _logger.LogWarning("Failed to get container status: {Error}", error);
            return Result.Failure<string>($"Failed to get container status: {error}");
        }

        return Result<string>.Success(output);
    }

    private async Task<Result<Unit>> ExecuteDockerComposeAsync(
        string dockerComposeFilePath,
        string arguments,
        string operation,
        CancellationToken cancellationToken)
    {
        var (exitCode, output, error) = await ExecuteCommandAsync(
            "docker-compose",
            $"-f \"{dockerComposeFilePath}\" {arguments}",
            cancellationToken);

        if (exitCode != 0)
        {
            _logger.LogError("{Operation} failed. Exit code: {ExitCode}, Error: {Error}",
                operation, exitCode, error);
            return Result.Failure<Unit>($"{operation} failed: {error}");
        }

        _logger.LogDebug("{Operation} succeeded. Output: {Output}", operation, output);
        return Result<Unit>.Success(Unit.Value);
    }

    private async Task<(int exitCode, string output, string error)> ExecuteCommandAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
            }
        };

        _logger.LogDebug("Executing: {Command} {Arguments}", command, arguments);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        var exitCode = process.ExitCode;
        var output = outputBuilder.ToString().Trim();
        var error = errorBuilder.ToString().Trim();

        _logger.LogDebug("Command completed. Exit code: {ExitCode}", exitCode);

        return (exitCode, output, error);
    }
}
