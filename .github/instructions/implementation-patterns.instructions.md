# Implementation Patterns

Project-specific implementation patterns and common solutions for the AStar Dev OneDrive Sync Client.

---

## Handling Sealed/Unmockable Classes

**Problem**: Some Microsoft libraries (MSAL, Graph) use sealed classes that cannot be mocked directly.

**Solution**: Create wrapper interfaces around sealed classes for testability.

**Examples**:

- `IGraphApiClient` wraps `GraphServiceClient`
- `IAuthenticationClient` wraps `IPublicClientApplication`
- `IFileWatcherService` wraps `FileSystemWatcher`

**Pattern**:

```csharp
// Wrapper interface
public interface IGraphApiClient
{
    Task<DriveItem> GetItemAsync(string accountId, string itemId);
}

// Implementation wraps sealed class
[Service(ServiceLifetime.Singleton, As = typeof(IGraphApiClient))]
public class GraphApiClient : IGraphApiClient
{
    private readonly GraphServiceClient _graphClient;

    public async Task<DriveItem> GetItemAsync(string accountId, string itemId)
    {
        return await _graphClient.Me.Drive.Items[itemId].GetAsync();
    }
}
```

---

## File Watcher Pattern

**Purpose**: Detect local file system changes in real-time to trigger immediate synchronization.

**Implementation**:

- `FileWatcherService` monitors all configured sync directories
- Debounces rapid changes (multiple rapid edits = one sync operation)
- Triggers immediate local → remote upload for detected changes
- Handles file system events: Created, Changed, Deleted, Renamed

**Key Features**:

- Multi-directory monitoring for multi-account support
- Debounce threshold: 2 seconds (configurable)
- Filters out temporary files and system files
- Thread-safe event handling

---

## Delta Query Pattern

**Purpose**: Efficiently fetch only changed items from OneDrive instead of scanning entire drive.

**Flow**:

1. Fetch changes using Microsoft Graph `/delta` endpoint
2. Process each changed item (created, modified, deleted)
3. Extract `cTag` (content tag) from each item for future conflict detection
4. Store delta token from response for next query
5. Resume from saved token on next sync (incremental sync)

**Benefits**:

- Dramatically reduces API calls and bandwidth
- Enables efficient incremental synchronization
- Automatic pagination handling for large change sets
- Supports resumption after interruption

**Implementation**:

```csharp
public async IAsyncEnumerable<DriveItem> GetDeltaChangesAsync(
    string accountId,
    string deltaToken,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var request = string.IsNullOrEmpty(deltaToken)
        ? _graphClient.Me.Drive.Root.Delta()
        : _graphClient.Me.Drive.Root.Delta(deltaToken);

    do
    {
        var page = await request.GetAsync(cancellationToken: ct);

        foreach (var item in page.Value)
        {
            yield return item;
        }

        // Save delta token for next sync
        if (page.OdataDeltaLink != null)
        {
            await SaveDeltaTokenAsync(accountId, ExtractToken(page.OdataDeltaLink), ct);
        }

        request = page.OdataNextLink != null
            ? new DriveItemDeltaRequest(page.OdataNextLink, _graphClient, null)
            : null;

    } while (request != null);
}
```

---

## Conflict Resolution Storage

**Purpose**: Persist unresolved sync conflicts for user review and resolution.

**Storage**: `SyncConflictRepository` persists conflicts to SQLite database with full metadata.

**User Resolution Options**:

1. **Keep Local**: Discard remote version, re-upload local file to OneDrive
2. **Keep Remote**: Discard local version, re-download remote file from OneDrive
3. **View Both**: Open both versions side-by-side for manual inspection and decision

**Conflict Record Structure**:

- Account ID
- File path (local and remote)
- Conflict detection time
- Local file metadata (size, modified time, hash)
- Remote file metadata (size, cTag, modified time)
- Resolution status (pending, resolved)
- Resolution choice (if resolved)

**Application**: Resolution is applied during next sync phase:

- Pending conflicts block automatic sync of affected files
- User must resolve before file can be synced
- Resolution updates both local and remote state
- Conflict record marked as resolved with timestamp

---

## Progress Reporting

**Purpose**: Real-time synchronization progress feedback to UI layer.

**Mechanism**: `BehaviorSubject<SyncProgress>` observable stream using Reactive Extensions.

**Reported Metrics**:

- Total files to sync (count)
- Total bytes to sync (size)
- Files synced so far (count)
- Bytes synced so far (size)
- Current file being processed (name and path)
- Current sync stage (downloading/uploading)
- Estimated time remaining (calculated)
- Sync speed (bytes/second)

**UI Updates**:

- ReactiveUI bindings subscribe to `SyncEngine.Progress` observable
- Progress throttled to max 10 updates per second to avoid UI flooding
- Stage transitions reported immediately (download → upload)
- Errors reported via separate error stream

**Implementation**:

```csharp
private readonly BehaviorSubject<SyncProgress> _progressSubject = new();

public IObservable<SyncProgress> Progress => _progressSubject
    .Throttle(TimeSpan.FromMilliseconds(100))  // Max 10 updates/sec
    .ObserveOn(RxApp.MainThreadScheduler);      // Ensure UI thread

private void ReportProgress(SyncProgress progress)
{
    _progressSubject.OnNext(progress);
}
```

**Best Practices**:

- Report progress after each file operation (not per-chunk)
- Calculate percentages based on bytes, not file count (more accurate for mixed file sizes)
- Include cancellation status in progress updates
- Clear progress state when sync completes or fails
