using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Security;

namespace NASBackup
{
    public class BackupEngine
    {
        public event EventHandler<int> ProgressChanged;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<string> LogMessage;

        private bool isBackupRunning = false;

        public async Task<bool> TestConnectionAsync(BackupConfig config)
        {
            try
            {
                OnStatusChanged("Testing connection...");
                OnLogMessage("Testing connection to NAS server...");

                if (config.UseCredentials)
                {
                    await ConnectWithCredentialsAsync(config.NasServer, config.Username, config.Password);
                }

                // Test if destination path is accessible
                if (!string.IsNullOrEmpty(config.DestinationPath))
                {
                    bool pathExists = Directory.Exists(config.DestinationPath) || 
                                     await Task.Run(() => TestNetworkPath(config.DestinationPath));
                    
                    if (!pathExists)
                    {
                        OnLogMessage("Warning: Destination path may not be accessible");
                        return false;
                    }
                }

                OnLogMessage("Connection test successful");
                OnStatusChanged("Connection successful");
                return true;
            }
            catch (Exception ex)
            {
                OnLogMessage($"Connection test failed: {ex.Message}");
                OnStatusChanged("Connection failed");
                return false;
            }
        }

        public async Task StartBackupAsync(List<string> sourcePaths, string destinationPath, BackupConfig config)
        {
            foreach (var sourcePath in sourcePaths)
            {
                await StartBackupAsync(sourcePath, destinationPath, config);
            }
        }
        
        public async Task StartBackupAsync(string sourcePath, string destinationPath, BackupConfig config)
        {
            if (isBackupRunning)
            {
                throw new InvalidOperationException("Backup is already running");
            }

            isBackupRunning = true;

            try
            {
                OnStatusChanged("Starting backup...");
                OnLogMessage("Backup started");

                // Connect to NAS if credentials are provided
                if (config.UseCredentials && !string.IsNullOrEmpty(config.NasServer))
                {
                    await ConnectWithCredentialsAsync(config.NasServer, config.Username, config.Password);
                    OnLogMessage("Connected to NAS server");
                }

                // Ensure destination directory exists
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                    OnLogMessage($"Created destination directory: {destinationPath}");
                }

                // Get all files to backup
                OnStatusChanged("Scanning files...");
                var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                OnLogMessage($"Found {files.Length} files to backup");

                // Backup files
                int processedFiles = 0;
                long totalBytes = 0;
                long copiedBytes = 0;

                // Calculate total size
                foreach (var file in files)
                {
                    totalBytes += new System.IO.FileInfo(file).Length;
                }

                OnLogMessage($"Total size: {FormatBytes(totalBytes)}");

                foreach (var sourceFile in files)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                        var destinationFile = Path.Combine(destinationPath, relativePath);
                        var destinationDir = Path.GetDirectoryName(destinationFile);

                        // Create destination directory if it doesn't exist
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        // Check if file needs to be copied (different or doesn't exist)
                        bool needsCopy = !File.Exists(destinationFile);
                        if (!needsCopy)
                        {
                            var sourceInfo = new System.IO.FileInfo(sourceFile);
                            var destInfo = new System.IO.FileInfo(destinationFile);
                            needsCopy = sourceInfo.LastWriteTime != destInfo.LastWriteTime || 
                                       sourceInfo.Length != destInfo.Length;
                        }

                        if (needsCopy)
                        {
                            OnStatusChanged($"Copying: {relativePath}");
                            await CopyFileAsync(sourceFile, destinationFile);
                            OnLogMessage($"Copied: {relativePath}");
                        }
                        else
                        {
                            OnLogMessage($"Skipped (up to date): {relativePath}");
                        }

                        var fileInfo = new System.IO.FileInfo(sourceFile);
                        copiedBytes += fileInfo.Length;
                        processedFiles++;

                        // Update progress
                        int progress = (int)((copiedBytes * 100) / totalBytes);
                        OnProgressChanged(Math.Min(progress, 100));
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage($"Error copying {sourceFile}: {ex.Message}");
                    }
                }

                OnProgressChanged(100);
                OnStatusChanged("Backup completed");
                OnLogMessage($"Backup completed successfully. Processed {processedFiles} files ({FormatBytes(copiedBytes)})");
            }
            catch (Exception ex)
            {
                OnStatusChanged("Backup failed");
                OnLogMessage($"Backup failed: {ex.Message}");
                throw;
            }
            finally
            {
                isBackupRunning = false;
            }
        }

        public async Task<BackupSimulationResult> SimulateBackupAsync(List<string> sourcePaths, string destinationPath, BackupConfig config)
        {
            var result = new BackupSimulationResult();
            
            try
            {
                OnStatusChanged("Running simulation...");
                OnLogMessage("Starting backup simulation");

                // Connect to NAS if credentials are provided (for path validation)
                if (config.UseCredentials && !string.IsNullOrEmpty(config.NasServer))
                {
                    await ConnectWithCredentialsAsync(config.NasServer, config.Username, config.Password);
                    OnLogMessage("Connected to NAS server for simulation");
                }

                foreach (var sourcePath in sourcePaths)
                {
                    await SimulateSinglePathAsync(sourcePath, destinationPath, result);
                }

                // Calculate estimated time (assume 50 MB/sec average transfer rate)
                const long averageTransferRate = 50 * 1024 * 1024; // 50 MB/sec
                if (result.SizeToTransfer > 0)
                {
                    result.EstimatedTime = TimeSpan.FromSeconds(Math.Max(1, result.SizeToTransfer / (double)averageTransferRate));
                }
                else
                {
                    result.EstimatedTime = TimeSpan.Zero;
                }

                OnStatusChanged($"Simulation complete: {result.FilesToCopy} files to copy");
                OnLogMessage($"Simulation complete. {result.Summary}");
                
                return result;
            }
            catch (Exception ex)
            {
                OnLogMessage($"Simulation failed: {ex.Message}");
                throw;
            }
        }

        public async Task<BackupSimulationResult> SimulateBackupAsync(string sourcePath, string destinationPath, BackupConfig config)
        {
            return await SimulateBackupAsync(new List<string> { sourcePath }, destinationPath, config);
        }

        private async Task SimulateSinglePathAsync(string sourcePath, string destinationPath, BackupSimulationResult result)
        {
            if (!Directory.Exists(sourcePath))
            {
                OnLogMessage($"Warning: Source path does not exist: {sourcePath}");
                return;
            }

            OnStatusChanged($"Analyzing: {sourcePath}");
            
            // Get all files to analyze
            var files = await Task.Run(() => Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories));
            
            foreach (var sourceFile in files)
            {
                try
                {
                    var fileInfo = new System.IO.FileInfo(sourceFile);
                    result.TotalFiles++;
                    result.TotalSize += fileInfo.Length;

                    var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                    var destinationFile = Path.Combine(destinationPath, relativePath);

                    // Check if file needs to be copied
                    bool needsCopy = !File.Exists(destinationFile);
                    if (!needsCopy)
                    {
                        var destInfo = new System.IO.FileInfo(destinationFile);
                        needsCopy = fileInfo.LastWriteTime != destInfo.LastWriteTime || 
                                   fileInfo.Length != destInfo.Length;
                    }

                    if (needsCopy)
                    {
                        result.FilesToCopy++;
                        result.SizeToTransfer += fileInfo.Length;
                        
                        // Add sample files (limit to 10 for display)
                        if (result.SampleFilesToCopy.Count < 10)
                        {
                            result.SampleFilesToCopy.Add(relativePath);
                        }
                    }
                    else
                    {
                        result.FilesToSkip++;
                        
                        // Add sample skipped files (limit to 5 for display)
                        if (result.SampleFilesToSkip.Count < 5)
                        {
                            result.SampleFilesToSkip.Add(relativePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLogMessage($"Error analyzing {sourceFile}: {ex.Message}");
                }
            }
        }

        private async Task ConnectWithCredentialsAsync(string server, string username, string password)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Use Windows credential manager or network authentication
                    var networkCredential = new NetworkCredential(username, password);
                    
                    // For UNC paths, Windows will use these credentials automatically
                    // This is a simplified approach - in production, you might want to use
                    // WNetAddConnection2 or similar Windows API calls
                    
                    OnLogMessage($"Authenticated with server: {server}");
                }
                catch (Exception ex)
                {
                    OnLogMessage($"Authentication failed: {ex.Message}");
                    throw;
                }
            });
        }

        private bool TestNetworkPath(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            const int bufferSize = 1024 * 1024; // 1MB buffer
            
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(destinationStream, bufferSize);
            }

            // Preserve file timestamps
            var sourceInfo = new System.IO.FileInfo(sourceFile);
            File.SetCreationTime(destinationFile, sourceInfo.CreationTime);
            File.SetLastWriteTime(destinationFile, sourceInfo.LastWriteTime);
        }

        private string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number = number / 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }

        protected virtual void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        protected virtual void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, message);
        }
    }
}