using System.IO;
using System.Text.Json;
using ConsoleDeck.Models;

namespace ConsoleDeck.Core;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public string DataDir { get; }
    public string ProfilesDir { get; }
    private string ActiveProfileFile { get; }
    public string DiscordAuthFile { get; }
    public string HaAuthFile { get; }

    public event Action? ConfigChanged;
    public event Action? ProfileListChanged;

    private FileSystemWatcher? _watcher;
    private DateTime _lastWrite;
    private string _activeProfile = "Default";

    public ConfigService()
    {
        DataDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        ProfilesDir     = Path.Combine(DataDir, "profiles");
        ActiveProfileFile = Path.Combine(DataDir, "active_profile.txt");
        DiscordAuthFile = Path.Combine(DataDir, "discord_auth.json");
        HaAuthFile      = Path.Combine(DataDir, "ha_auth.json");

        Directory.CreateDirectory(ProfilesDir);
        MigrateIfNeeded();
        _activeProfile = ReadActiveProfileName();
    }

    // One-time migration: copy old flat config.json → profiles/Default.json
    private void MigrateIfNeeded()
    {
        var oldConfig = Path.Combine(DataDir, "config.json");
        var defaultProfile = ProfilePath("Default");
        if (File.Exists(oldConfig) && !File.Exists(defaultProfile))
            File.Copy(oldConfig, defaultProfile);
    }

    private string ReadActiveProfileName()
    {
        if (!File.Exists(ActiveProfileFile)) return "Default";
        var name = File.ReadAllText(ActiveProfileFile).Trim();
        return File.Exists(ProfilePath(name)) ? name : GetProfiles().FirstOrDefault() ?? "Default";
    }

    // ── Profile management ──────────────────────────────────────────────────

    public string GetActiveProfile() => _activeProfile;

    public List<string> GetProfiles()
        => Directory.GetFiles(ProfilesDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .OrderBy(n => n)
            .ToList();

    public void SetActiveProfile(string name)
    {
        _activeProfile = name;
        File.WriteAllText(ActiveProfileFile, name);
        if (_watcher != null)
        {
            _watcher.Filter = $"{name}.json";
            var path = ProfilePath(name);
            _lastWrite = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
        }
        ConfigChanged?.Invoke();
    }

    public void CreateProfile(string name)
    {
        var path = ProfilePath(name);
        if (File.Exists(path)) return;
        var def = Enumerable.Range(1, 9).ToDictionary(i => $"BUTTON_{i}", _ => new ButtonAction());
        File.WriteAllText(path, JsonSerializer.Serialize(def, JsonOpts));
        ProfileListChanged?.Invoke();
    }

    public void DeleteProfile(string name)
    {
        var path = ProfilePath(name);
        if (File.Exists(path)) File.Delete(path);
        ProfileListChanged?.Invoke();
    }

    public void RenameProfile(string oldName, string newName)
    {
        var oldPath = ProfilePath(oldName);
        var newPath = ProfilePath(newName);
        if (!File.Exists(oldPath) || File.Exists(newPath)) return;
        File.Move(oldPath, newPath);
        if (_activeProfile == oldName)
        {
            _activeProfile = newName;
            File.WriteAllText(ActiveProfileFile, newName);
            if (_watcher != null) _watcher.Filter = $"{newName}.json";
        }
        ProfileListChanged?.Invoke();
    }

    private string ProfilePath(string name) => Path.Combine(ProfilesDir, $"{name}.json");

    // ── Button config ────────────────────────────────────────────────────────

    public Dictionary<string, ButtonAction> LoadConfig()
    {
        var path = ProfilePath(_activeProfile);
        if (!File.Exists(path))
        {
            var def = Enumerable.Range(1, 9).ToDictionary(i => $"BUTTON_{i}", _ => new ButtonAction());
            SaveConfig(def);
            return def;
        }
        return JsonSerializer.Deserialize<Dictionary<string, ButtonAction>>(File.ReadAllText(path)) ?? new();
    }

    public void SaveConfig(Dictionary<string, ButtonAction> config)
        => File.WriteAllText(ProfilePath(_activeProfile), JsonSerializer.Serialize(config, JsonOpts));

    // ── Auth ─────────────────────────────────────────────────────────────────

    public DiscordAuth LoadDiscordAuth()
    {
        if (!File.Exists(DiscordAuthFile)) return new();
        return JsonSerializer.Deserialize<DiscordAuth>(File.ReadAllText(DiscordAuthFile)) ?? new();
    }

    public void SaveDiscordAuth(DiscordAuth auth)
        => File.WriteAllText(DiscordAuthFile, JsonSerializer.Serialize(auth, JsonOpts));

    public HaAuth LoadHaAuth()
    {
        if (!File.Exists(HaAuthFile)) return new();
        return JsonSerializer.Deserialize<HaAuth>(File.ReadAllText(HaAuthFile)) ?? new();
    }

    public void SaveHaAuth(HaAuth auth)
        => File.WriteAllText(HaAuthFile, JsonSerializer.Serialize(auth, JsonOpts));

    // ── File watching ────────────────────────────────────────────────────────

    public void StartWatching()
    {
        var path = ProfilePath(_activeProfile);
        _lastWrite = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
        _watcher = new FileSystemWatcher(ProfilesDir, $"{_activeProfile}.json")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var path = ProfilePath(_activeProfile);
        var mtime = File.GetLastWriteTime(path);
        if (mtime == _lastWrite) return;
        _lastWrite = mtime;
        ConfigChanged?.Invoke();
    }

    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
