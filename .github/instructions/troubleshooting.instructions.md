# Debugging & Troubleshooting

Common issues, debugging techniques, and troubleshooting guides for the AStar Dev OneDrive Sync Client.

---

## Debug Logging

The application includes comprehensive debug logging infrastructure for troubleshooting sync issues.

### Accessing Debug Logs

**UI Access**:

- Navigate to DebugLogs view in the application
- Filter by account, date range, log level, or search text
- Export logs to file for sharing or analysis

**Database Access**:

```sql
SELECT * FROM DebugLogs
WHERE AccountId = 'account-id'
  AND Timestamp > datetime('now', '-1 hour')
ORDER BY Timestamp DESC;
```

### Adding Debug Logs

**Entry/Exit Logging**:

```csharp
await DebugLog.EntryAsync(
    DebugLogMetadata.Services.MyService.MyMethod,
    accountId,
    cancellationToken);

// Method implementation

await DebugLog.ExitAsync(
    DebugLogMetadata.Services.MyService.MyMethod,
    accountId,
    cancellationToken);
```

**Informational Logging**:

```csharp
await DebugLog.LogInfoAsync(
    "MyService.MyMethod",
    accountId,
    $"Processing {itemCount} items",
    cancellationToken);
```

**Error Logging**:

```csharp
await DebugLog.LogErrorAsync(
    "MyService.MyMethod",
    accountId,
    exception,
    "Failed to process items",
    cancellationToken);
```

**Performance Logging**:

```csharp
using var perfLogger = await DebugLog.StartPerformanceLogAsync(
    "MyService.MyMethod",
    accountId,
    cancellationToken);

// Code to measure

// Automatically logs duration on disposal
```

### Log Levels

- **Trace**: Method entry/exit, detailed flow
- **Debug**: Detailed diagnostic information
- **Info**: General informational messages
- **Warning**: Warning messages (recoverable issues)
- **Error**: Error messages (exceptions, failures)
- **Fatal**: Fatal errors (application crashes)

---

## Common Issues

### Sync Not Starting

**Symptoms**: Sync button appears disabled or sync does not progress.

**Troubleshooting**:

1. Check account authentication status in Accounts view
2. Verify sync folder is selected in Configuration
3. Check if another sync operation is in progress
4. Review debug logs for authentication errors
5. Ensure network connectivity to OneDrive

**Solutions**:

- Re-authenticate account if token expired
- Select sync folder if none configured
- Wait for current sync to complete
- Check firewall/proxy settings

### Conflicts Not Detected

**Symptoms**: Files sync without conflict prompt when both local and remote changed.

**Troubleshooting**:

1. Verify `cTag` is being stored for remote files
2. Check local file `mtime` (modification time) is accurate
3. Review conflict detection threshold (60 seconds)
4. Check debug logs for conflict detection logic
5. Verify file metadata is being tracked in database

**Solutions**:

- Ensure database migrations are up-to-date
- Verify file system timestamps are correct
- Check if files are within conflict threshold window
- Review `SyncConflictRepository` for pending conflicts

### Database Locked

**Symptoms**: "Database is locked" error messages.

**Troubleshooting**:

1. Check if multiple application instances are running
2. Verify no other process has database file open
3. Check for unfinished transactions
4. Review connection string configuration
5. Check disk space and permissions

**Solutions**:

- Close all running instances of the application
- Restart application (closes lingering connections)
- Check for stuck background sync operations
- Verify SQLite WAL mode is enabled for concurrency

### Graph API Errors

**Symptoms**: API calls fail with HTTP errors (401, 403, 429, 500, etc.)

**Troubleshooting**:

1. **401 Unauthorized**: Check token expiration, re-authenticate
2. **403 Forbidden**: Verify API permissions (Files.ReadWrite.All)
3. **429 Too Many Requests**: Rate limit exceeded, implement backoff
4. **500 Internal Server Error**: Temporary Microsoft service issue, retry
5. Review Graph API response headers for additional information

**Solutions**:

- Implement exponential backoff for retries
- Cache frequently accessed data to reduce API calls
- Use delta queries to minimize data transfer
- Monitor rate limits and adjust sync frequency
- Check Microsoft 365 Service Health Dashboard

---

## Running Specific Tests

### All Tests in a Project

```bash
dotnet test test/AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/
```

### Specific Test Class

```bash
dotnet test --filter WindowPreferencesServiceShould
```

### Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~WindowPreferencesServiceShould.ReturnNullWhenNoPreferencesExist"
```

### Tests by Category

```bash
dotnet test --filter "Category=Integration"
```

### Watch Mode (Auto-Rerun on Changes)

```bash
dotnet watch test
```

### With Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Verbose Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## Performance Profiling

### Using dotnet-trace

```bash
# Start recording
dotnet trace collect --process-id <pid> --providers Microsoft-Windows-DotNETRuntime

# Stop with Ctrl+C
# Analyze trace with PerfView or Visual Studio
```

### Memory Profiling

```bash
# Install dotMemory or dotMemory Unit
# Profile specific test or application startup
# Look for memory leaks, large allocations, GC pressure
```

### CPU Profiling

```bash
# Use Visual Studio Performance Profiler
# Or dotnet-counters for real-time metrics
dotnet counters monitor --process-id <pid>
```

---

## Logging Best Practices

1. **Log at appropriate levels**: Don't use Error for warnings, Info for debug details
2. **Include context**: Always include account ID, file paths, operation IDs
3. **Avoid logging sensitive data**: Never log tokens, passwords, or personal data
4. **Use structured logging**: Pass parameters for structured queries
5. **Log exceptions properly**: Include full exception details (message, stack trace, inner exceptions)
6. **Avoid excessive logging**: Don't log in tight loops or per-file in batch operations
7. **Clean up logs**: Implement log retention policy (30 days default)
