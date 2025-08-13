using DrJaw.Models;
using System;
using System.IO;
using System.Text.Json;
using static DrJaw.Models.Storage;

namespace DrJaw.Utils
{
    public static class AppSettingsManager
    {
        private const string SettingsFilePath = "DrJaw.conf";

        public static void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);
        }

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }
}
