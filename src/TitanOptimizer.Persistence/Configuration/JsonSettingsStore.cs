using System.Text.Json;
using TitanOptimizer.Core.Configuration;

namespace TitanOptimizer.Persistence.Configuration;

public sealed class JsonSettingsStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public JsonSettingsStore(string path)
    {
        _path = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(_path)
            ?? throw new ArgumentException("Settings path must include a directory.", nameof(path));
        Directory.CreateDirectory(directory);
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), _options)
            ?? throw new InvalidDataException("The settings file is empty.");
        if (settings.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported settings schema version: {settings.SchemaVersion}.");
        }

        return settings;
    }

    public void Save(AppSettings settings)
    {
        if (settings.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported settings schema version: {settings.SchemaVersion}.");
        }

        var temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, _options));
        File.Move(temporaryPath, _path, true);
    }
}
