using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NASBackup
{
    public class BackupConfig
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NASBackup",
            "config.json"
        );

        public List<string> SourcePaths { get; set; } = new List<string>();
        public string SourcePath { get; set; } = ""; // Kept for backward compatibility
        public string DestinationPath { get; set; } = "";
        public string NasServer { get; set; } = "";
        public string Username { get; set; } = "";
        
        [JsonIgnore]
        public string Password { get; set; } = "";
        
        public bool UseCredentials { get; set; } = false;
        public bool ScheduleEnabled { get; set; } = false;
        public TimeSpan ScheduleTime { get; set; } = new TimeSpan(2, 0, 0); // 2:00 AM default
        public bool[] ScheduleDays { get; set; } = new bool[7]; // Sunday to Saturday
        public bool EnableDuplicateDetection { get; set; } = true;
        public bool AutoRemoveDuplicates { get; set; } = false;
        public string AwsAccessKey { get; set; } = "";
        public string AwsSecretKey { get; set; } = "";
        public string AwsRegion { get; set; } = "us-east-1";
        public string BedrockModel { get; set; } = "anthropic.claude-3-haiku-20240307-v1:0";

        public static BackupConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<BackupConfig>(json) ?? new BackupConfig();
                    
                    // Load sensitive data from secure storage
                    config.Password = LoadPasswordFromSecureStorage(config.Username);
                    config.AwsSecretKey = LoadPasswordFromSecureStorage($\"aws_secret_{config.AwsAccessKey}\");
                    
                    return config;
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with default config
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }
            
            return new BackupConfig();
        }

        public void Save()
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // Save sensitive data to secure storage
                SavePasswordToSecureStorage(Username, Password);
                SavePasswordToSecureStorage($\"aws_secret_{AwsAccessKey}\", AwsSecretKey);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        private static string LoadPasswordFromSecureStorage(string username)
        {
            try
            {
                // Simplified password storage - in production, use Windows Credential Manager
                // or encrypt the password before storing
                var passwordFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NASBackup",
                    "credentials.dat"
                );

                if (File.Exists(passwordFile) && !string.IsNullOrEmpty(username))
                {
                    var encryptedData = File.ReadAllBytes(passwordFile);
                    return System.Text.Encoding.UTF8.GetString(
                        System.Security.Cryptography.ProtectedData.Unprotect(
                            encryptedData,
                            System.Text.Encoding.UTF8.GetBytes(username),
                            System.Security.Cryptography.DataProtectionScope.CurrentUser
                        )
                    );
                }
            }
            catch
            {
                // Return empty string if decryption fails
            }

            return "";
        }

        private static void SavePasswordToSecureStorage(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    return;

                var passwordFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NASBackup",
                    "credentials.dat"
                );

                var passwordDir = Path.GetDirectoryName(passwordFile);
                if (!Directory.Exists(passwordDir))
                {
                    Directory.CreateDirectory(passwordDir);
                }

                var encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                    System.Text.Encoding.UTF8.GetBytes(password),
                    System.Text.Encoding.UTF8.GetBytes(username),
                    System.Security.Cryptography.DataProtectionScope.CurrentUser
                );

                File.WriteAllBytes(passwordFile, encryptedData);
            }
            catch
            {
                // Ignore encryption errors - password just won't be saved
            }
        }
    }
}