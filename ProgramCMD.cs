using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

class Program
{
    private const string GITHUB_API = "https://api.github.com/repos/FluxyRepacks/AutoCrack/releases/latest";
    private const string GITHUB_REPO = "https://github.com/FluxyRepacks/AutoCrack";
    private const string GITHUB_RAW = "https://raw.githubusercontent.com/FluxyRepacks/AutoCrack/main/steam";
    private const string CURRENT_VERSION = "1.0.0";
    
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main()
    {
        httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoCrack-Console");
        
        Console.Title = $"Fluxy Repacks - AutoCrack v{CURRENT_VERSION}";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine($"║   Fluxy Repacks - AutoCrack v{CURRENT_VERSION}    ║");
        Console.WriteLine("║   Steam API DLL Replacer              ║");
        Console.WriteLine($"║   GitHub: {GITHUB_REPO.PadRight(24)}║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        // Check for updates
        await CheckForUpdates();

        // Get the directory containing the executable
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string steamDir = Path.Combine(appDir, "steam");

        // Check if steam folder exists, if not download it
        if (!Directory.Exists(steamDir) || !HasSteamDlls(steamDir))
        {
            await DownloadSteamFolder(appDir, steamDir);
        }

        // Check for DLL files in steam folder
        string steamApi32 = Path.Combine(steamDir, "steam_api.dll");
        string steamApi64 = Path.Combine(steamDir, "steam_api64.dll");

        bool has32bit = File.Exists(steamApi32);
        bool has64bit = File.Exists(steamApi64);

        if (!has32bit && !has64bit)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] No DLL files found even after download attempt!");
            Console.ResetColor();
            Console.WriteLine($"Please check: {GITHUB_REPO}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"[INFO] Found in steam folder:");
        if (has32bit) Console.WriteLine("  ✓ steam_api.dll");
        if (has64bit) Console.WriteLine("  ✓ steam_api64.dll");
        Console.WriteLine();

        // Get target directory from user
        Console.Write("Enter the target directory path: ");
        string targetDir = Console.ReadLine()?.Trim('"');

        if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] Invalid directory path!");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("[INFO] Scanning directory and subdirectories...");
        Console.WriteLine();

        int replaced32 = 0;
        int replaced64 = 0;
        int errors = 0;

        try
        {
            // Search for all matching DLL files
            if (has32bit)
            {
                string[] files32 = Directory.GetFiles(targetDir, "steam_api.dll", SearchOption.AllDirectories);
                foreach (string file in files32)
                {
                    try
                    {
                        File.Copy(steamApi32, file, true);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[REPLACED] {file}");
                        Console.ResetColor();
                        replaced32++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[FAILED] {file} - {ex.Message}");
                        Console.ResetColor();
                        errors++;
                    }
                }
            }

            if (has64bit)
            {
                string[] files64 = Directory.GetFiles(targetDir, "steam_api64.dll", SearchOption.AllDirectories);
                foreach (string file in files64)
                {
                    try
                    {
                        File.Copy(steamApi64, file, true);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[REPLACED] {file}");
                        Console.ResetColor();
                        replaced64++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[FAILED] {file} - {ex.Message}");
                        Console.ResetColor();
                        errors++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║            SUMMARY                     ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine($"steam_api.dll replaced: {replaced32}");
        Console.WriteLine($"steam_api64.dll replaced: {replaced64}");
        Console.WriteLine($"Total files replaced: {replaced32 + replaced64}");
        if (errors > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Errors encountered: {errors}");
            Console.ResetColor();
        }
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static bool HasSteamDlls(string steamDir)
    {
        if (!Directory.Exists(steamDir)) return false;
        
        string steamApi32 = Path.Combine(steamDir, "steam_api.dll");
        string steamApi64 = Path.Combine(steamDir, "steam_api64.dll");
        
        return File.Exists(steamApi32) || File.Exists(steamApi64);
    }

    private static async Task DownloadSteamFolder(string appDir, string steamDir)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[INFO] Steam folder not found. Downloading from GitHub repository...");
        Console.ResetColor();

        try
        {
            if (!Directory.Exists(steamDir))
                Directory.CreateDirectory(steamDir);

            // Download steam_api.dll
            Console.WriteLine("[INFO] Downloading steam_api.dll...");
            try
            {
                var dll32Data = await httpClient.GetByteArrayAsync($"{GITHUB_RAW}/steam_api.dll");
                File.WriteAllBytes(Path.Combine(steamDir, "steam_api.dll"), dll32Data);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[SUCCESS] steam_api.dll downloaded!");
                Console.ResetColor();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[WARNING] steam_api.dll not found in repository");
                Console.ResetColor();
            }

            // Download steam_api64.dll
            Console.WriteLine("[INFO] Downloading steam_api64.dll...");
            try
            {
                var dll64Data = await httpClient.GetByteArrayAsync($"{GITHUB_RAW}/steam_api64.dll");
                File.WriteAllBytes(Path.Combine(steamDir, "steam_api64.dll"), dll64Data);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[SUCCESS] steam_api64.dll downloaded!");
                Console.ResetColor();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[WARNING] steam_api64.dll not found in repository");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Failed to download Steam DLLs: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine($"Please download manually from: {GITHUB_REPO}/tree/main/steam");
            Console.WriteLine();
        }
    }

    private static async Task CheckForUpdates()
    {
        try
        {
            Console.WriteLine("[INFO] Checking for updates...");
            var response = await httpClient.GetStringAsync(GITHUB_API);
            var jsonDoc = JsonDocument.Parse(response);
            string latestVersion = jsonDoc.RootElement.GetProperty("tag_name").GetString().TrimStart('v');

            if (IsNewerVersion(latestVersion, CURRENT_VERSION))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[UPDATE] New version available: v{latestVersion} (Current: v{CURRENT_VERSION})");
                Console.ResetColor();
                Console.Write("Do you want to update now? (Y/N): ");
                string answer = Console.ReadLine()?.Trim().ToUpper();

                if (answer == "Y" || answer == "YES")
                {
                    await DownloadAndInstallUpdate(jsonDoc);
                }
                else
                {
                    Console.WriteLine("[INFO] Update skipped. Continuing...");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[INFO] You are using the latest version!");
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        catch
        {
            // Silently skip if no releases exist
            Console.WriteLine("[INFO] No updates available.");
            Console.WriteLine();
        }
    }

    private static async Task DownloadAndInstallUpdate(JsonDocument releaseData)
    {
        try
        {
            Console.WriteLine("[INFO] Downloading update...");

            var assets = releaseData.RootElement.GetProperty("assets");
            string downloadUrl = null;

            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString();
                if (name.EndsWith(".exe") && !name.Contains("GUI"))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (downloadUrl == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Update file not found!");
                Console.ResetColor();
                return;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string newExePath = Path.Combine(appDir, "AutoCrack_new.exe");
            string batchPath = Path.Combine(appDir, "update.bat");

            var exeData = await httpClient.GetByteArrayAsync(downloadUrl);
            File.WriteAllBytes(newExePath, exeData);

            // Create update batch script
            string batchContent = $@"@echo off
timeout /t 2 /nobreak >nul
del ""{currentExe}""
move /y ""{newExePath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""";

            File.WriteAllText(batchPath, batchContent);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SUCCESS] Update downloaded! Restarting...");
            Console.ResetColor();

            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Update failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        var latestParts = latest.Split('.');
        var currentParts = current.Split('.');

        for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
        {
            if (int.TryParse(latestParts[i], out int l) && int.TryParse(currentParts[i], out int c))
            {
                if (l > c) return true;
                if (l < c) return false;
            }
        }
        return false;
    }
}