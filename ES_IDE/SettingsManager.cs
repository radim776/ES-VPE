using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace EventScriptIDE
{
    public class AppSettings
    {
        [JsonProperty("buildtools_path")]
        public string BuildToolsPath { get; set; } = @"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools";

        [JsonProperty("VDO")]
        public bool VDO { get; set; } = false;
    }

    public static class SettingsManager
    {
        public static readonly string AppDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EventScriptIDE");

        public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EventScriptIDE", "Settings.json");

        public static readonly string ProjectsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EventScriptIDE", "Projects");

        public static readonly string ExtensionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EventScriptIDE", "Extensions");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var text = File.ReadAllText(SettingsPath);
                    var s = JsonConvert.DeserializeObject<AppSettings>(text);
                    if (s != null) return s;
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}
