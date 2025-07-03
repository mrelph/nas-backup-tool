using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon;

namespace NASBackup
{
    public class BackupFileInfo
    {
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Hash { get; set; } = "";
        public string Name => System.IO.Path.GetFileName(Path);
        public string Extension => System.IO.Path.GetExtension(Path);
        public string Directory => System.IO.Path.GetDirectoryName(Path) ?? "";
    }

    public class DuplicateGroup
    {
        public List<BackupFileInfo> Files { get; set; } = new List<BackupFileInfo>();
        public string Reason { get; set; } = "";
        public long TotalSize => Files.Sum(f => f.Size);
        public long WastedSpace => Files.Count > 1 ? Files.Skip(1).Sum(f => f.Size) : 0;
        public string RecommendedAction { get; set; } = "";
    }

    public class DuplicateAnalyzer : IDisposable
    {
        public event EventHandler<string> ProgressChanged;
        public event EventHandler<string> LogMessage;

        private readonly AmazonBedrockRuntimeClient bedrockClient;
        private readonly string awsAccessKey;
        private readonly string awsSecretKey;
        private readonly string awsRegion;
        private readonly string bedrockModel;

        public DuplicateAnalyzer(string accessKey = "", string secretKey = "", string region = "us-east-1", string model = "anthropic.claude-3-haiku-20240307-v1:0")
        {
            awsAccessKey = accessKey;
            awsSecretKey = secretKey;
            awsRegion = region;
            bedrockModel = model;
            
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                var config = new AmazonBedrockRuntimeConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
                };
                bedrockClient = new AmazonBedrockRuntimeClient(accessKey, secretKey, config);
            }
        }

        public async Task<List<DuplicateGroup>> AnalyzeDuplicatesAsync(List<string> sourcePaths)
        {
            var allFiles = new List<BackupFileInfo>();
            var duplicateGroups = new List<DuplicateGroup>();

            OnProgressChanged("Scanning files...");
            OnLogMessage("Starting duplicate analysis");

            // Collect all files from source paths
            foreach (var sourcePath in sourcePaths)
            {
                if (Directory.Exists(sourcePath))
                {
                    await CollectFilesAsync(sourcePath, allFiles);
                }
            }

            OnLogMessage($"Found {allFiles.Count} files to analyze");

            // Find exact duplicates by hash
            OnProgressChanged("Finding exact duplicates...");
            var exactDuplicates = await FindExactDuplicatesAsync(allFiles);
            duplicateGroups.AddRange(exactDuplicates);

            // Find similar files by name and size
            OnProgressChanged("Finding similar files...");
            var similarFiles = FindSimilarFiles(allFiles);
            duplicateGroups.AddRange(similarFiles);

            // Use Bedrock for intelligent analysis if credentials are provided
            if (bedrockClient != null)
            {
                OnProgressChanged("Running AI analysis...");
                await EnhanceWithBedrockAnalysisAsync(duplicateGroups);
            }

            OnLogMessage($"Analysis complete. Found {duplicateGroups.Count} duplicate groups");
            return duplicateGroups;
        }

        private async Task CollectFilesAsync(string path, List<BackupFileInfo> fileList)
        {
            try
            {
                var files = await Task.Run(() => Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                
                foreach (var file in files)
                {
                    try
                    {
                        var info = new System.IO.FileInfo(file);
                        fileList.Add(new BackupFileInfo
                        {
                            Path = file,
                            Size = info.Length,
                            ModifiedDate = info.LastWriteTime
                        });
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage($"Error accessing file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error scanning directory {path}: {ex.Message}");
            }
        }

        private async Task<List<DuplicateGroup>> FindExactDuplicatesAsync(List<BackupFileInfo> files)
        {
            var duplicateGroups = new List<DuplicateGroup>();
            var hashGroups = new Dictionary<string, List<BackupFileInfo>>();

            // Group files by size first (optimization)
            var sizeGroups = files.GroupBy(f => f.Size).Where(g => g.Count() > 1);

            foreach (var sizeGroup in sizeGroups)
            {
                foreach (var file in sizeGroup)
                {
                    try
                    {
                        file.Hash = await ComputeFileHashAsync(file.Path);
                        
                        if (!hashGroups.ContainsKey(file.Hash))
                            hashGroups[file.Hash] = new List<BackupFileInfo>();
                        
                        hashGroups[file.Hash].Add(file);
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage($"Error computing hash for {file.Path}: {ex.Message}");
                    }
                }
            }

            // Create duplicate groups for files with same hash
            foreach (var hashGroup in hashGroups.Where(g => g.Value.Count > 1))
            {
                duplicateGroups.Add(new DuplicateGroup
                {
                    Files = hashGroup.Value,
                    Reason = "Exact duplicate (same content)",
                    RecommendedAction = "Keep newest file, delete others"
                });
            }

            return duplicateGroups;
        }

        private List<DuplicateGroup> FindSimilarFiles(List<BackupFileInfo> files)
        {
            var duplicateGroups = new List<DuplicateGroup>();
            
            // Group by similar names
            var nameGroups = files
                .GroupBy(f => GetSimilarityKey(f.Name))
                .Where(g => g.Count() > 1 && g.Key.Length > 3);

            foreach (var nameGroup in nameGroups)
            {
                var similarFiles = nameGroup.ToList();
                
                // Check if files are similar in size (within 10%)
                var avgSize = similarFiles.Average(f => f.Size);
                var similarSizeFiles = similarFiles
                    .Where(f => Math.Abs(f.Size - avgSize) / avgSize < 0.1)
                    .ToList();

                if (similarSizeFiles.Count > 1)
                {
                    duplicateGroups.Add(new DuplicateGroup
                    {
                        Files = similarSizeFiles,
                        Reason = "Similar name and size",
                        RecommendedAction = "Review manually - may be different versions"
                    });
                }
            }

            return duplicateGroups;
        }

        private string GetSimilarityKey(string fileName)
        {
            // Remove common version patterns and normalize
            var key = fileName.ToLowerInvariant();
            key = System.Text.RegularExpressions.Regex.Replace(key, @"[\d\._\-\(\)]+", "");
            key = System.Text.RegularExpressions.Regex.Replace(key, @"\s+", "");
            return key;
        }

        private async Task<string> ComputeFileHashAsync(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = await Task.Run(() => md5.ComputeHash(stream));
                return Convert.ToBase64String(hash);
            }
        }

        private async Task EnhanceWithBedrockAnalysisAsync(List<DuplicateGroup> duplicateGroups)
        {
            try
            {
                foreach (var group in duplicateGroups.Take(5)) // Limit to prevent excessive API calls
                {
                    var prompt = CreateAnalysisPrompt(group);
                    var response = await CallBedrockApiAsync(prompt);
                    
                    if (!string.IsNullOrEmpty(response))
                    {
                        group.RecommendedAction = $"AI Suggestion: {response}";
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Bedrock analysis failed: {ex.Message}");
            }
        }

        private string CreateAnalysisPrompt(DuplicateGroup group)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Analyze these potentially duplicate files and provide a recommendation:");
            sb.AppendLine($"Reason for grouping: {group.Reason}");
            sb.AppendLine("Files:");
            
            foreach (var file in group.Files)
            {
                sb.AppendLine($"- {file.Name} ({FormatBytes(file.Size)}) in {file.Directory}");
                sb.AppendLine($"  Modified: {file.ModifiedDate:yyyy-MM-dd HH:mm:ss}");
            }
            
            sb.AppendLine("\nProvide a brief recommendation (1-2 sentences) on which file(s) to keep and which to delete, considering:");
            sb.AppendLine("- File names and potential versioning");
            sb.AppendLine("- Modification dates");
            sb.AppendLine("- Directory locations");
            sb.AppendLine("- File sizes");
            
            return sb.ToString();
        }

        private async Task<string> CallBedrockApiAsync(string prompt)
        {
            try
            {
                if (bedrockClient == null)
                    return "";

                var systemPrompt = "You are a helpful assistant that analyzes duplicate files and provides concise recommendations for file management. Respond with a brief recommendation (1-2 sentences) on which file(s) to keep and which to delete.";
                
                var claudeRequest = new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 150,
                    temperature = 0.3,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var requestJson = JsonSerializer.Serialize(claudeRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                var request = new InvokeModelRequest
                {
                    ModelId = bedrockModel,
                    ContentType = "application/json",
                    Accept = "application/json",
                    Body = new MemoryStream(requestBytes)
                };

                var response = await bedrockClient.InvokeModelAsync(request);
                
                using var reader = new StreamReader(response.Body);
                var responseJson = await reader.ReadToEndAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                if (responseObj.TryGetProperty("content", out var content) && content.GetArrayLength() > 0)
                {
                    var firstContent = content[0];
                    if (firstContent.TryGetProperty("text", out var text))
                    {
                        return text.GetString()?.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Bedrock API call failed: {ex.Message}");
            }
            
            return "";
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }

        protected virtual void OnProgressChanged(string progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }

        protected virtual void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, message);
        }

        public void Dispose()
        {
            bedrockClient?.Dispose();
        }
    }
}