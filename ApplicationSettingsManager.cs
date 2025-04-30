using System.IO;
using System.Text.Json;

namespace MaciScriptIDE
{
    public class ApplicationSettings
    {
        public bool IsDarkMode { get; set; } = false;
    }

    // Add this class to store application settings
    public class ApplicationSettingsManager
    {
        // You can add more settings here in the future

        // Singleton instance
        private static ApplicationSettingsManager? _instance;
        public static ApplicationSettingsManager Instance => _instance ??= new ApplicationSettingsManager();

        public ApplicationSettings Settings => _settings ??= new ApplicationSettings();
        private ApplicationSettings _settings;

        // Private constructor for singleton pattern
        private ApplicationSettingsManager() { }

        // Load settings from file
        // Load settings from file
        public void Load()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MaciScriptIDE",
                    "settings.json");

                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);

                    // Check if the file content is valid
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Use JsonSerializerOptions to provide more flexibility
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            WriteIndented = true
                        };

                        var loadedSettings = JsonSerializer.Deserialize<ApplicationSettings>(json, options);

                        if (loadedSettings != null)
                        {
                            Settings.IsDarkMode = loadedSettings.IsDarkMode;
                            // Copy other properties as needed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the specific error
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        // Save settings to file
        public void Save()
        {
            try
            {
                string settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MaciScriptIDE");

                // Create directory if it doesn't exist
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                string settingsPath = Path.Combine(settingsDir, "settings.json");
                string json = JsonSerializer.Serialize(Settings);
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // If we can't save settings, just continue
            }
        }
    }
}
