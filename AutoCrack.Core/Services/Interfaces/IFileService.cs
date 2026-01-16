using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoCrack.Core.Services.Interfaces
{
    /// <summary>
    /// Provides an abstraction over the file system to handle game directory scanning and file manipulation.
    /// </summary>
    public interface IFileService
    {
        bool DirectoryExists(string path);
        
        /// <summary>
        /// Recursively finds all files in a directory matching a specific pattern.
        /// </summary>
        IEnumerable<string> FindFiles(string rootPath, string searchPattern);

        /// <summary>
        /// Attempts to get the Product Name from a file's version info (used for game detection).
        /// Returns null if unable to read.
        /// </summary>
        string? GetFileProductName(string filePath);

        /// <summary>
        /// asynchronous write operation to save downloaded assets.
        /// </summary>
        Task WriteBytesAsync(string path, byte[] data);

        /// <summary>
        /// Copies a file with overwrite permission.
        /// </summary>
        void CopyFile(string source, string destination);
    }
}