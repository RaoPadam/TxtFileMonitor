using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TxtFileMonitor.Models;
using TxtFileMonitor.Utilities;

namespace TxtFileMonitor.Observers
{
    public static class FileObserver
    {
        // FileSystemObserver to monitor changes in the file
        private static FileSystemWatcher observer;

        // Size of each chunk for processing the file (10 MB)
        private static int chunkSize = 10 * 1024 * 1024;

        // Dictionary to store chunk data (hash, content, previous content, change flag)
        private static readonly ConcurrentDictionary<string, ChunkData> chunkData = new ConcurrentDictionary<string, ChunkData>();

        // Debounce time to avoid frequent processing
        private static int debounceTime = 5000; // milliseconds

        // Time of the last processed change
        private static DateTime lastProcessed = DateTime.MinValue;

        // Path to the file being monitored
        private static string filePath;

        // Path to the log file
        private static string logFilePath;

        // List to store changes for batching
        private static List<string> changeLog = new List<string>();

        // Starts the file observer
        public static void StartFileObserver(string path, string logPath)
        {
            filePath = path;
            logFilePath = logPath;
            SetupObserver(path);
        }

        // Sets up the FileSystemObserver
        private static void SetupObserver(string path)
        {
            observer = new FileSystemWatcher(Path.GetDirectoryName(path))
            {
                Filter = Path.GetFileName(path),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            // Subscribe to the Changed event
            observer.Changed += OnChanged;
            observer.EnableRaisingEvents = true;
        }

        // Handles file change events
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Debounce to avoid processing changes too frequently
            if ((DateTime.Now - lastProcessed).TotalMilliseconds < debounceTime) return;
            lastProcessed = DateTime.Now;

            // Process the file change asynchronously
            Task.Run(() => ProcessFileChangeAsync());
        }

        // Processes file changes asynchronously
        private static async Task ProcessFileChangeAsync()
        {
            int chunkIndex = 0;
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[chunkSize];
                    int bytesRead;

                    // Read the file in chunks
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string chunkName = ((char)('A' + chunkIndex)).ToString();
                        var newHash = HashUtility.ComputeHash(buffer, bytesRead);
                        var newContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Update chunk data
                        chunkData.AddOrUpdate(chunkName,
                            new ChunkData { Hash = newHash, Content = newContent, PreviousContent = "", IsChanged = false },
                            (key, oldValue) =>
                            {
                                if (oldValue.Hash != newHash)
                                {
                                    // Log the chunk change for debugging
                                    string logEntry = $"Timestamp: {DateTime.Now}\n";
                                    changeLog.Add(logEntry);

                                    // Return updated chunk data
                                    return new ChunkData { Hash = newHash, Content = newContent, PreviousContent = oldValue.Content, IsChanged = true };
                                }
                                return oldValue; // No change
                            });

                        chunkIndex++;
                    }
                }
                // Trim excess chunks if the file size has decreased
                TrimExcessChunks(chunkIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file change: {ex.Message}");
            }
        }

        // Processes the initial content of the file
        public static void ProcessInitialFileContent(string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            int chunkIndex = 0;

            for (int i = 0; i < buffer.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, buffer.Length - i);
                byte[] chunkBuffer = new byte[length];
                Array.Copy(buffer, i, chunkBuffer, 0, length);

                string chunkName = ((char)('A' + chunkIndex)).ToString();
                var hash = HashUtility.ComputeHash(chunkBuffer, length);
                var chunkContent = Encoding.UTF8.GetString(chunkBuffer, 0, length);

                chunkData.AddOrUpdate(chunkName,
                    new ChunkData { Hash = hash, Content = chunkContent, PreviousContent = "", IsChanged = false },
                    (key, oldValue) => new ChunkData { Hash = hash, Content = chunkContent, PreviousContent = oldValue.Content, IsChanged = true });

                chunkIndex++;
            }
        }

        // Removes chunks that are no longer valid
        private static void TrimExcessChunks(int validChunkCount)
        {
            var keysToRemove = chunkData.Keys.Where(k => k[0] >= 'A' + validChunkCount).ToList();
            foreach (var key in keysToRemove)
            {
                chunkData.TryRemove(key, out _);
            }
        }

        // Reports changes every 15 seconds
        public static void ReportChanges(object state)
        {
            StringBuilder logBuilder = new StringBuilder();
            bool anyChanges = false;

            foreach (var entry in chunkData)
            {
                if (entry.Value.IsChanged)
                {
                    anyChanges = true;

                    string timestampLog = $"Timestamp: {DateTime.Now}\n";
                    changeLog.Add(timestampLog);
                    logBuilder.AppendLine(timestampLog);

                    // Use DiffPlex to build the diff model
                    var diffBuilder = new InlineDiffBuilder(new DiffPlex.Differ());
                    var diff = diffBuilder.BuildDiffModel(entry.Value.PreviousContent, entry.Value.Content);

                    // Report changes
                    foreach (var line in diff.Lines)
                    {
                        switch (line.Type)
                        {
                            case ChangeType.Inserted:
                                string inserted = $"Change Type: Inserted\nChange Value: {line.Text}\n";
                                changeLog.Add(inserted);
                                logBuilder.AppendLine(inserted);
                                break;
                            case ChangeType.Deleted:
                                string deleted = $"Change Type: Deleted\nChange Value: {line.Text}\n";
                                changeLog.Add(deleted);
                                logBuilder.AppendLine(deleted);
                                break;
                            case ChangeType.Unchanged:
                                // Optionally handle unchanged lines
                                break;
                        }
                    }

                    // Update the previous content only if there are changes
                    chunkData[entry.Key].PreviousContent = entry.Value.Content;
                    chunkData[entry.Key].IsChanged = false; // Reset the change flag
                }
            }

            if (!anyChanges)
            {
                string noChangesLog = $"Timestamp: {DateTime.Now} - No changes detected.\n";
                logBuilder.AppendLine(noChangesLog);
            }

            try
            {
                // Write changes to the log file
                if (logBuilder.Length > 0)
                {
                    File.AppendAllText(logFilePath, logBuilder.ToString());
                }

                // Display changes on the console
                foreach (var logEntry in changeLog)
                {
                    Console.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }

            // Clear the change log
            changeLog.Clear();
        }

        // Checks if there are any changes
        public static bool HasChanges()
        {
            return chunkData.Values.Any(cd => cd.IsChanged);
        }
    }
}
