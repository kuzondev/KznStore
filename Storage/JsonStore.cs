using System.Text.Json;
using KznStore.Core;

namespace KznStore.Storage;

public sealed class JsonStore
{
    private readonly string _directory;
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string DirectoryPath => _directory;
    public JsonStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }
    public T Load<T>(string name, Func<T> fallback)
    {
        lock (_gate)
        {
            var path = Path.Combine(_directory, name);
            if (!File.Exists(path)) return fallback();
            try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? fallback(); }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                StoreLog.Error($"Read {name}", ex);
                if (ex is JsonException) File.Copy(path, path + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}", true);
                return fallback();
            }
        }
    }
    public void Save<T>(string name, T value)
    {
        lock (_gate)
        {
            var path = Path.Combine(_directory, name);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
            File.Move(temp, path, true);
        }
    }
}
