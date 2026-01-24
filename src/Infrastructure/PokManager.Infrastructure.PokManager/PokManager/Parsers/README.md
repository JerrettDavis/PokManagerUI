# POK Manager Output Parsers

This directory contains parsers that convert POK Manager plain text output into strongly-typed DTOs for the Application layer.

## Overview

POK Manager returns plain text output (not JSON), so these parsers are essential for:
- Converting text to Application layer DTOs
- Handling various output formats gracefully
- Categorizing and handling errors
- Parsing multi-line configuration output
- Extracting structured data from filenames

All parsers:
- Implement `IPokManagerOutputParser<T>`
- Return `Result<T>` (no exceptions thrown)
- Are stateless and thread-safe
- Handle malformed output gracefully
- Support case-insensitive parsing where appropriate

## Parsers

### 1. StatusOutputParser
**Input Format:**
```
Instance: MyServer, State: Running, Container: abc123, Health: Healthy
```

**Output:** `Result<InstanceStatus>`

**Features:**
- Parses instance state (Running, Stopped, Starting, etc.)
- Parses health status (Healthy, Degraded, Unhealthy, Unknown)
- Handles instance names with underscores/hyphens
- Case-insensitive enum parsing
- Detects and returns error messages

**Test Coverage:** 21 tests

### 2. DetailsOutputParser
**Input Format:**
```
SessionName: MyServer
ServerPassword: secret123
MaxPlayers: 20
CustomSetting1: Value1
```

**Output:** `Result<Dictionary<string, string>>`

**Features:**
- Parses multi-line key-value pairs
- Handles both standard and custom settings
- Preserves values with colons (paths, connection strings)
- Skips blank lines and comments (#)
- Handles duplicate keys (uses last value)
- Trims whitespace

**Test Coverage:** 19 tests

### 3. ListInstancesParser
**Input Format:**
```
Instance_Server1
Instance_Server2
Instance_Server3
```
Or comma-delimited: `Instance_Server1, Instance_Server2`

**Output:** `Result<IReadOnlyList<string>>`

**Features:**
- Extracts instance IDs from "Instance_" prefixed names
- Supports newline or comma-delimited lists
- Case-insensitive prefix matching
- Skips invalid entries
- Returns empty list for no instances
- Handles mixed valid/invalid entries

**Test Coverage:** 18 tests

### 4. BackupListParser
**Input Format:**
```
backup_MyServer_20250119_143022.tar.gz
backup_AnotherServer_20250119_150000.tar.zst
```

**Output:** `Result<IReadOnlyList<ParsedBackupInfo>>`

**Features:**
- Parses backup filename format: `backup_{instance}_{YYYYMMDD}_{HHMMSS}.tar.[gz|zst]`
- Extracts instance ID, timestamp, and compression format
- Handles full file paths (extracts filename)
- Validates timestamps
- Supports Gzip and Zstd compression
- Skips malformed filenames
- Returns `CompressionFormat.Unknown` for unknown extensions

**Test Coverage:** 20 tests

### 5. ErrorOutputParser
**Input Format:**
```
Error: Instance 'MyServer' not found
```

**Output:** `Result<PokManagerError>`

**Features:**
- Categorizes errors into specific error codes
- Preserves full error message
- Stores raw output for debugging
- Pattern-based error classification
- Handles multiline error messages
- Case-insensitive error detection

**Error Categories:**
- InstanceNotFound
- InstanceAlreadyExists
- InstanceNotRunning
- InstanceAlreadyRunning
- PermissionDenied
- InvalidConfiguration
- InvalidState
- BackupNotFound
- InsufficientDiskSpace
- NetworkError
- TimeoutError
- Unknown (fallback)

**Test Coverage:** 20 tests

## Supporting Types

### IPokManagerOutputParser<T>
Base interface for all parsers.

```csharp
public interface IPokManagerOutputParser<T>
{
    Result<T> Parse(string output);
}
```

### PokManagerError
Represents a categorized error from POK Manager.

```csharp
public record PokManagerError(
    PokManagerErrorCode ErrorCode,
    string Message,
    string RawOutput
);
```

### ParsedBackupInfo
Represents parsed backup information.

```csharp
public record ParsedBackupInfo(
    string FileName,
    string InstanceId,
    DateTimeOffset Timestamp,
    CompressionFormat CompressionFormat
);
```

## Usage Examples

### Parsing Instance Status
```csharp
var parser = new StatusOutputParser();
var output = "Instance: MyServer, State: Running, Container: abc123, Health: Healthy";
var result = parser.Parse(output);

if (result.IsSuccess)
{
    var status = result.Value;
    Console.WriteLine($"Instance {status.InstanceId} is {status.State}");
}
```

### Parsing Instance Details
```csharp
var parser = new DetailsOutputParser();
var output = @"SessionName: MyServer
ServerPassword: secret123
MaxPlayers: 20";
var result = parser.Parse(output);

if (result.IsSuccess)
{
    var settings = result.Value;
    Console.WriteLine($"Max Players: {settings["MaxPlayers"]}");
}
```

### Parsing Instance List
```csharp
var parser = new ListInstancesParser();
var output = @"Instance_Server1
Instance_Server2
Instance_Server3";
var result = parser.Parse(output);

if (result.IsSuccess)
{
    foreach (var instanceId in result.Value)
    {
        Console.WriteLine($"Found instance: {instanceId}");
    }
}
```

### Parsing Backup List
```csharp
var parser = new BackupListParser();
var output = "backup_MyServer_20250119_143022.tar.gz";
var result = parser.Parse(output);

if (result.IsSuccess)
{
    var backup = result.Value[0];
    Console.WriteLine($"Backup for {backup.InstanceId} created at {backup.Timestamp}");
}
```

### Parsing Errors
```csharp
var parser = new ErrorOutputParser();
var output = "Error: Instance 'MyServer' not found";
var result = parser.Parse(output);

if (result.IsSuccess)
{
    var error = result.Value;
    Console.WriteLine($"Error Code: {error.ErrorCode}");
    Console.WriteLine($"Message: {error.Message}");
}
```

## Test Coverage

Total: **105 tests** across 6 test files
- StatusOutputParserTests: 21 tests
- DetailsOutputParserTests: 19 tests
- ListInstancesParserTests: 18 tests
- BackupListParserTests: 20 tests
- ErrorOutputParserTests: 20 tests
- ParserValidationTests: 7 integration tests

All tests follow TinyBDD-style Given-When-Then pattern with descriptive method names.

## Edge Cases Covered

All parsers handle:
- ✅ Null input
- ✅ Empty strings
- ✅ Whitespace-only input
- ✅ Extra whitespace
- ✅ Malformed output
- ✅ Error messages from POK Manager
- ✅ Case-insensitive parsing
- ✅ Special characters
- ✅ Mixed valid/invalid data

## Design Principles

1. **No Exceptions**: All parsers return `Result<T>` instead of throwing exceptions
2. **Graceful Degradation**: Invalid entries are skipped, not fatal
3. **Thread-Safe**: All parsers are stateless and can be used concurrently
4. **Deterministic**: Same input always produces same output
5. **Testable**: Comprehensive test coverage with clear test names
6. **TDD**: All tests were written before implementation
