using Ahorro.Services.Abstractions;

namespace Ahorro.Services.Infrastructure;

public class NoOpSyncService : ISyncService
{
    public Task SyncAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class NoOpBackupService : IBackupService
{
    public Task BackupAsync(CancellationToken ct = default) => Task.CompletedTask;
}
