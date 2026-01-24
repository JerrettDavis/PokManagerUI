# POK Manager Command Builders - Examples

## Overview
The command builders provide a fluent, type-safe API for constructing bash commands for POK Manager operations with built-in security validation.

## Command Builder Examples

### StatusCommandBuilder
```csharp
// Get status of a specific instance
var command = StatusCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .WithVerbose()
    .WithJsonOutput()
    .Build();
// Result: /usr/local/bin/pok.sh status island_main --verbose --json
```

### StartCommandBuilder
```csharp
// Start an instance with timeout
var command = StartCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .WithDetached()
    .WithTimeout(300)
    .Build();
// Result: /usr/local/bin/pok.sh start island_main --timeout 300 --detached
```

### StopCommandBuilder
```csharp
// Stop with grace period and save
var command = StopCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .WithGracePeriod(300)
    .WithSave()
    .Build();
// Result: /usr/local/bin/pok.sh stop island_main --grace-period 300 --save
```

### RestartCommandBuilder
```csharp
// Restart with options
var command = RestartCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .WithGracePeriod(300)
    .WithWait()
    .WithSave()
    .Build();
// Result: /usr/local/bin/pok.sh restart island_main --grace-period 300 --wait --save
```

### BackupCommandBuilder
```csharp
// Create compressed backup
var command = BackupCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .WithCompression("gzip")
    .WithIncremental()
    .WithDescription("Daily backup")
    .Build();
// Result: /usr/local/bin/pok.sh backup island_main --compress gzip --description 'Daily backup' --incremental
```

### RestoreCommandBuilder
```csharp
// Restore from backup
var command = RestoreCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .FromBackup(backupId)
    .WithStopBeforeRestore()
    .WithStartAfterRestore()
    .Build();
// Result: /usr/local/bin/pok.sh restore island_main --backup-id island_main_backup_2025-01-19_12-00-00 --stop-before-restore --start-after-restore
```

### UpdateCommandBuilder
```csharp
// Update with backup and validation
var command = UpdateCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .ToVersion("1.2.3")
    .WithBackupBeforeUpdate()
    .WithValidate()
    .WithRestartAfterUpdate()
    .Build();
// Result: /usr/local/bin/pok.sh update island_main --version 1.2.3 --backup-before-update --validate --restart-after-update
```

### CreateInstanceCommandBuilder
```csharp
// Create new instance with full configuration
var command = CreateInstanceCommandBuilder
    .Create("/usr/local/bin/pok.sh")
    .ForInstance(instanceId)
    .WithMap("TheIsland")
    .WithMaxPlayers(20)
    .WithPort(7777)
    .WithServerName("My Palworld Server")
    .WithPassword(password)
    .WithPublic()
    .WithPvE()
    .WithStartAfterCreate()
    .Build();
// Result: /usr/local/bin/pok.sh create island_main --map TheIsland --players 20 --port 7777 --server-name 'My Palworld Server' --password MyPass123 --public --pve --start-after-create
```

## Security Features

All command builders include:
1. **Command Injection Prevention**: Validates and rejects dangerous characters (`;`, `&`, `|`, `` ` ``, `$`, `(`, `)`, `<`, `>`, `\`)
2. **Path Traversal Protection**: Detects and blocks `../` patterns in file paths
3. **Proper Shell Escaping**: Uses single quotes with proper escape handling for arguments with spaces or special characters
4. **Type-Safe Instance IDs**: Uses domain value objects that are pre-validated
5. **Result Pattern**: All builders return `Result<string>` for safe error handling

## Usage Pattern

```csharp
var result = StatusCommandBuilder
    .Create(scriptPath)
    .ForInstance(instanceId)
    .Build();

if (result.IsSuccess)
{
    var command = result.Value;
    // Execute command safely
}
else
{
    // Handle validation error
    var error = result.Error;
}
```
