using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AutoCrack.Core.Services.Interfaces;

namespace AutoCrack.Core.Services
{
    public class FileService : IFileService
    {
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public IEnumerable<string> FindFiles(string rootPath, string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                yield break;
            }

            // Using a queue for iterative traversal to avoid StackOverflowException on deep directories
            var pending = new Queue<string>();
            pending.Enqueue(rootPath);

            while (pending.Count > 0)
            {
                var currentPath = pending.Dequeue();
                string[]? files = null;

                try
                {
                    files = Directory.GetFiles(currentPath, searchPattern);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip system directories or protected folders
                    continue; 
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }

                if (files != null)
                {
                    foreach (var file in files)
                    {
                        yield return file;
                    }
                }

                try
                {
                    var subDirs = Directory.GetDirectories(currentPath);
                    foreach (var dir in subDirs)
                    {
                        pending.Enqueue(dir);
                    }
                }
                catch (UnauthorizedAccessException) { /* Ignore */ }
            }
        }

        public string? GetFileProductName(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                var info = FileVersionInfo.GetVersionInfo(filePath);
                return !string.IsNullOrWhiteSpace(info.ProductName) ? info.ProductName : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task WriteBytesAsync(string path, byte[] data)
        {
            // Ensure directory exists before writing
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(path, data);
        }

        public void CopyFile(string source, string destination)
        {
            // Ensure destination directory exists
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(source, destination, true);
        }
    }
}