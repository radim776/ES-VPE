using Newtonsoft.Json;
using System;
using System.IO;

namespace EventScriptIDE
{
    public class AppSettings
    {
        [JsonProperty("buildtools_path")]
        public string BuildToolsPath { get; set; } =
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools";

        [JsonProperty("discordrpc")]
        public bool DiscordRpc { get; set; } = false;
    }

    public static class SettingsManager
    {
        public static readonly string AppData =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "EventScriptIDE");

        public static readonly string SettingsPath =
            Path.Combine(AppData, "Settings.json");

        public static readonly string ProjectsRoot =
            Path.Combine(AppData, "Projects");

        public static readonly string ExtensionsDir =
            Path.Combine(AppData, "Extensions");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* fall through */ }
            return new AppSettings();
        }

        public static void Save(AppSettings s)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(s, Formatting.Indented));
        }
    }
}