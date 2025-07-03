using System;
using System.Collections.Generic;

namespace NASBackup
{
    public class BackupSimulationResult
    {
        public int TotalFiles { get; set; }
        public int FilesToCopy { get; set; }
        public int FilesToSkip { get; set; }
        public long TotalSize { get; set; }
        public long SizeToTransfer { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public List<string> SampleFilesToCopy { get; set; } = new List<string>();
        public List<string> SampleFilesToSkip { get; set; } = new List<string>();
        public string Summary => $"Files: {TotalFiles} total, {FilesToCopy} to copy, {FilesToSkip} to skip\n" +
                                $"Size: {FormatBytes(SizeToTransfer)} to transfer of {FormatBytes(TotalSize)} total\n" +
                                $"Estimated time: {EstimatedTime:mm\\:ss}";

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
    }
}