using Caprine.FilePath;
using Squash.Core.Extensions;
using System.Text.Json;

namespace Squash.Core.Services;

public sealed class PersistentStateService
{
    private readonly FilePath        _filePath;
    private HashSet<string>          _completedActions;
    private readonly SemaphoreSlim   _lock = new(1, 1);

    public PersistentStateService()
    {
        var dir = FilePath.FromSpecialFolder(Environment.SpecialFolder.ApplicationData) /
                  GlobalShared.Product.Organization                                     /
                  GlobalShared.Product.AppName;
        dir.Mkdir(true, true);

        _filePath = dir / "persistent-state.json";

        _completedActions = Load();
    }

    public bool HasCompleted(string key)
    {
        key = key.CreateGuidFrom("B");
        return Volatile.Read(ref _completedActions).Contains(key);
    }

    public async Task MarkCompletedAsync(string key, CancellationToken ct = default)
    {
        key = key.CreateGuidFrom("B");

        await _lock.WaitAsync(ct);

        try
        {
            var updatedActions = new HashSet<string>(_completedActions);
            if (updatedActions.Add(key))
            {
                await SaveAsync(updatedActions, ct).ConfigureAwait(false);
                Volatile.Write(ref _completedActions, updatedActions);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        key = key.CreateGuidFrom("B");

        await _lock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            var updatedActions = new HashSet<string>(_completedActions);
            if (updatedActions.Remove(key))
            {
                await SaveAsync(updatedActions, ct).ConfigureAwait(false);
                Volatile.Write(ref _completedActions, updatedActions);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private HashSet<string> Load()
    {
        try
        {
            if (!_filePath.Exists)
            {
                return [];
            }

            var json = _filePath.ReadText();

            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task SaveAsync(HashSet<string> completedActions, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(completedActions);
        var temporaryPath = $"{_filePath.FullPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, ct).ConfigureAwait(false);
            File.Move(temporaryPath, _filePath.FullPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
