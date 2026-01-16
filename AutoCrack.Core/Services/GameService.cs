using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrack.Core.Models;
using AutoCrack.Core.Services.Interfaces;

namespace AutoCrack.Core.Services
{
    public class GameService : IGameService
    {
        private readonly IFileService _fileService;
        private readonly HttpClient _httpClient;

        public GameService(IFileService fileService)
        {
            _fileService = fileService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
        }

        public string DetectGameNameFromPath(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !_fileService.DirectoryExists(directoryPath))
            {
                return "Unknown";
            }

            // Fallback: Use the directory name
            string bestName = new DirectoryInfo(directoryPath).Name;

            try
            {
                // Find potential main game executables
                var exes = _fileService.FindFiles(directoryPath, "*.exe")
                    .Where(f => !IsExcludedExecutable(Path.GetFileName(f)))
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Length) // Largest EXE is usually the game
                    .ToList();

                if (exes.Count > 0)
                {
                    string mainExe = exes[0].FullName;
                    string? productName = _fileService.GetFileProductName(mainExe);
                    
                    if (!string.IsNullOrWhiteSpace(productName))
                    {
                        return productName;
                    }
                }
            }
            catch
            {
                // If analysis fails, return the directory name
            }

            return bestName;
        }

        public async Task<GameMetadata> FetchGameMetadataAsync(string gameName)
        {
            var result = new GameMetadata { Found = false, Title = gameName };

            try
            {
                string url = $"{Constants.BaseApiUrl}/api/game-info?name={Uri.EscapeDataString(gameName)}";
                string jsonResponse = await _httpClient.GetStringAsync(url);
                
                var data = JsonSerializer.Deserialize<GameMetadata>(jsonResponse);

                if (data != null && data.Found)
                {
                    result = data;

                    // Download the image bytes if a URL is provided
                    if (!string.IsNullOrEmpty(result.ImageUrl))
                    {
                        result.CoverImageBytes = await _httpClient.GetByteArrayAsync(result.ImageUrl);
                    }
                }
            }
            catch
            {
                // API failures should not crash the app; return the basic metadata with Found=false
            }

            return result;
        }

        private bool IsExcludedExecutable(string fileName)
        {
            string lowerName = fileName.ToLowerInvariant();
            return lowerName.Contains("unins") || 
                   lowerName.Contains("setup") || 
                   lowerName.Contains("crash") || 
                   lowerName.Contains("launcher") ||
                   lowerName.Contains("dxwebsetup") ||
                   lowerName.Contains("vcredist");
        }
    }
}