using System.Text.Json;
using Mastonet.Entities;
using Serilog;

namespace ImmediateMastodon;

public class Config {
    public static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ImmediateMastodon"
    );

    private static readonly string FilePath = Path.Combine(DataDirectory, "config.json");

    private static readonly JsonSerializerOptions Options = new() {
        IncludeFields = true,
        WriteIndented = true
    };

    public int Width;
    public int Height;

    public Dictionary<string, AppRegistration> OAuthApps = new();
    public Dictionary<string, Auth> AccountAuth = new();
    public string? LastAccount;

    public static Config Load() {
        try {
            if (File.Exists(FilePath)) {
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath), Options)!;
            }
        } catch (Exception e) {
            Log.Error(e, "Failed to load config");
        }

        var config = new Config();
        config.Save();
        return config;
    }

    public void Save() {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
    }
}
