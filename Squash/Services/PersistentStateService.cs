using Squash.Extensions;
using Squash.Lib;
using System.Text.Json;

namespace Squash.Services;

public sealed class PersistentStateService
{
    private readonly FilePath        _filePath;
    private readonly HashSet<string> _completedActions;
    private readonly SemaphoreSlim   _lock = new(1, 1);

    public PersistentStateService()
    {
        var dir = FilePath.FromSpecialFolder(Environment.SpecialFolder.ApplicationData) / Constants.Product.Organization / Constants.Product.AppName;
        dir.Mkdir(true, true);
        
        _filePath = dir / "persistent-state.json";

        _completedActions = Load();
    }

    public bool HasCompleted(string key)
    {
        key = key.CreateGuidFrom("B");
        return _completedActions.Contains(key);
    }

    public async Task MarkCompletedAsync(string key, CancellationToken ct = default)
    {
        key = key.CreateGuidFrom("B");
        
        await _lock.WaitAsync(ct);

        try
        {
            if (_completedActions.Add(key))
            {
                await SaveAsync(ct).ConfigureAwait(false);
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
            if (_completedActions.Remove(key))
            {
                await SaveAsync(ct).ConfigureAwait(false);
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
            if (!_filePath.Exists())
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

    private async Task SaveAsync(CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(_completedActions);

        await _filePath.WriteTextAsync(json, ct: ct).ConfigureAwait(false);
    }
}
