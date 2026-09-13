using System.Text.Json;
using TitanOptimizer.Core.Models;

namespace TitanOptimizer.Persistence.Catalog;

public sealed class JsonOptimizationCatalog
{
    private readonly string _directory;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public JsonOptimizationCatalog(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

    public IReadOnlyList<OptimizationDefinition> LoadAll()
    {
        if (!Directory.Exists(_directory))
        {
            return [];
        }

        var definitions = new List<OptimizationDefinition>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var definition = JsonSerializer.Deserialize<OptimizationDefinition>(File.ReadAllText(path), _options)
                ?? throw new InvalidDataException($"Optimization definition '{path}' is empty.");
            Validate(definition, path);
            if (!ids.Add(definition.Id))
            {
                throw new InvalidDataException($"Duplicate optimization id '{definition.Id}'.");
            }

            definitions.Add(definition);
        }

        return definitions;
    }

    private static void Validate(OptimizationDefinition definition, string path)
    {
        if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidDataException($"Optimization definition '{path}' requires an id and name.");
        }

        if (definition.Tier == QualityTier.Rejected && definition.IsEligibleForAutomaticApplication)
        {
            throw new InvalidDataException($"Rejected optimization '{definition.Id}' cannot be automatic.");
        }
    }
}
