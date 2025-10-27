using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Migrations;

/// <summary>
/// Manages schema snapshots for migration scaffolding
/// </summary>
public class SchemaSnapshot
{
    private const string SnapshotFileName = ".migrations-snapshot.json";

    /// <summary>
    /// Saves a schema snapshot to disk
    /// </summary>
    public static async Task SaveAsync(DatabaseSchema schema, string? directory = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        var snapshotPath = Path.Combine(directory, SnapshotFileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(schema, options);
        await File.WriteAllTextAsync(snapshotPath, json);
    }

    /// <summary>
    /// Loads a schema snapshot from disk
    /// </summary>
    public static async Task<DatabaseSchema?> LoadAsync(string? directory = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        var snapshotPath = Path.Combine(directory, SnapshotFileName);

        if (!File.Exists(snapshotPath))
            return null;

        var json = await File.ReadAllTextAsync(snapshotPath);
        return JsonSerializer.Deserialize<DatabaseSchema>(json);
    }

    /// <summary>
    /// Checks if a snapshot exists
    /// </summary>
    public static bool Exists(string? directory = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        var snapshotPath = Path.Combine(directory, SnapshotFileName);
        return File.Exists(snapshotPath);
    }

    /// <summary>
    /// Deletes the snapshot file
    /// </summary>
    public static void Delete(string? directory = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        var snapshotPath = Path.Combine(directory, SnapshotFileName);

        if (File.Exists(snapshotPath))
            File.Delete(snapshotPath);
    }
}
