using System;
using System.Threading.Tasks;

namespace AutoCrack.Core.Services.Interfaces
{
    public interface ICrackerService
    {
        /// <summary>
        /// Triggered when the service wants to report progress or status text to the UI.
        /// </summary>
        event Action<string> OnLog;

        /// <summary>
        /// Downloads the Steam API DLLs from GitHub if they are not cached locally.
        /// </summary>
        Task PrepareResourcesAsync();

        /// <summary>
        /// Performs the crack operation on the specified directory.
        /// </summary>
        /// <returns>The number of files replaced.</returns>
        Task<int> ApplyCrackAsync(string targetDirectory);

        /// <summary>
        /// Sends a log entry to the Fluxy API for analytics.
        /// </summary>
        Task LogAttemptToApiAsync(bool success, string gameName, string discordId, string discordName);
    }
}