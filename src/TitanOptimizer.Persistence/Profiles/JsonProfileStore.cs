using System.Text.Json;
using TitanOptimizer.Core.Profiles;

namespace TitanOptimizer.Persistence.Profiles;

public sealed class JsonProfileStore
{
    private readonly string _directory;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public JsonProfileStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
    }

    public void Save(OptimizationProfile profile)
    {
        Validate(profile);
        var path = GetPath(profile.Id);
        var temporaryPath = path + ".tmp";
        var json = JsonSerializer.Serialize(profile with { UpdatedUtc = DateTimeOffset.UtcNow }, _options);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, path, true);
    }

    public OptimizationProfile? Load(string id)
    {
        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        var profile = JsonSerializer.Deserialize<OptimizationProfile>(File.ReadAllText(path), _options)
            ?? throw new InvalidDataException($"Profile '{id}' is empty.");
        Validate(profile);
        return profile;
    }

    public IReadOnlyList<OptimizationProfile> List()
    {
        return Directory.EnumerateFiles(_directory, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Select(Load)
            .Where(profile => profile is not null)
            .Cast<OptimizationProfile>()
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string GetPath(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Profile identifiers must be safe file names.", nameof(id));
        }

        return Path.Combine(_directory, id + ".json");
    }

    private static void Validate(OptimizationProfile profile)
    {
        if (profile.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported profile schema version: {profile.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new InvalidDataException("Profiles require a non-empty id and name.");
        }
    }
}
