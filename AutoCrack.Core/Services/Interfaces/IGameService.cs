using System.Threading.Tasks;
using AutoCrack.Core.Models;

namespace AutoCrack.Core.Services.Interfaces
{
    public interface IGameService
    {
        /// <summary>
        /// Analyzes the target directory to determine the game name based on executable metadata.
        /// </summary>
        string DetectGameNameFromPath(string directoryPath);

        /// <summary>
        /// Queries the API to fetch the official game title and cover image.
        /// </summary>
        Task<GameMetadata> FetchGameMetadataAsync(string gameName);
    }
}