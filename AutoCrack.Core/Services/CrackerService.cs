using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrack.Core.Services.Interfaces;

namespace AutoCrack.Core.Services
{
    public class CrackerService : ICrackerService
    {
        private readonly IFileService _fileService;
        private readonly HttpClient _httpClient;
        private readonly string _resourceDir;

        public event Action<string>? OnLog;

        public CrackerService(IFileService fileService)
        {
            _fileService = fileService;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);

            // Store DLLs in a localized 'steam_resources' folder relative to the executable
            _resourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_resources");
        }

        private void Log(string message) => OnLog?.Invoke(message);

        public async Task PrepareResourcesAsync()
        {
            if (!Directory.Exists(_resourceDir))
                Directory.CreateDirectory(_resourceDir);

            string[] dlls = { "steam_api.dll", "steam_api64.dll" };

            foreach (var dll in dlls)
            {
                string localPath = Path.Combine(_resourceDir, dll);
                
                // Only download if missing
                if (!File.Exists(localPath))
                {
                    Log($"Downloading resource: {dll}...");
                    try
                    {
                        string url = $"{Constants.GithubRawUrl}/{dll}";
                        var data = await _httpClient.GetByteArrayAsync(url);
                        await _fileService.WriteBytesAsync(localPath, data);
                        Log($"✓ Downloaded {dll} ({data.Length} bytes)");
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠ Failed to download {dll}: {ex.Message}");
                        throw new Exception($"Could not download {dll}. Check internet connection.");
                    }
                }
            }
        }

        public async Task<int> ApplyCrackAsync(string targetDirectory)
        {
            await PrepareResourcesAsync();

            Log("Scanning game directory for Steam API files...");
            int replacedCount = 0;

            // Run on a background thread to keep UI responsive
            await Task.Run(() =>
            {
                // We look for both 32-bit and 64-bit DLLs
                var foundFiles = _fileService.FindFiles(targetDirectory, "steam_api*.dll");

                foreach (var file in foundFiles)
                {
                    string fileName = Path.GetFileName(file).ToLower();
                    
                    // Validate specific filenames to avoid replacing unrelated files
                    if (fileName == "steam_api.dll" || fileName == "steam_api64.dll")
                    {
                        string sourcePath = Path.Combine(_resourceDir, fileName);

                        if (File.Exists(sourcePath))
                        {
                            try
                            {
                                _fileService.CopyFile(sourcePath, file);
                                Log($"✓ Replaced: {fileName} in {Path.GetFileName(Path.GetDirectoryName(file))}");
                                replacedCount++;
                            }
                            catch (Exception ex)
                            {
                                Log($"❌ Error replacing {file}: {ex.Message}");
                            }
                        }
                    }
                }
            });

            if (replacedCount == 0)
            {
                Log("⚠ No Steam API files found. This might not be a Steam game or the folder is incorrect.");
            }

            return replacedCount;
        }

        public async Task LogAttemptToApiAsync(bool success, string gameName, string discordId, string discordName)
        {
            try
            {
                var payload = new
                {
                    discordId = discordId ?? "Anonymous",
                    discordName = discordName ?? "LocalUser",
                    gameName,
                    status = success ? "SUCCESS" : "FAILED"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await _httpClient.PostAsync($"{Constants.BaseApiUrl}/api/log", content);
            }
            catch
            {
                // Silently fail logging to avoid annoying the user
            }
        }
    }
}