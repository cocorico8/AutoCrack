using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using AutoCrack.Core.Services.Interfaces;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoCrack.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        // --- Services ---
        private readonly IFileService _fileService;
        private readonly IGameService _gameService;
        private readonly IDiscordService _discordService;
        private readonly ICrackerService _crackerService;

        // --- Observable Properties (UI State) ---
        
        [ObservableProperty] 
        private string _gamePath = string.Empty;

        [ObservableProperty] 
        private string _gameName = "Waiting for selection...";

        [ObservableProperty] 
        private string _statusMessage = "Ready";

        [ObservableProperty] 
        private string _userName = "Connecting...";

        [ObservableProperty] 
        private Bitmap? _userAvatar;

        [ObservableProperty] 
        private Bitmap? _gameCover;

        [ObservableProperty] 
        [NotifyCanExecuteChangedFor(nameof(ApplyCrackCommand))]
        [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
        private bool _isBusy;

        // Collection for the log window
        public ObservableCollection<string> Logs { get; } = new();

        // --- Constructor ---
        public MainViewModel(
            IFileService fileService, 
            IGameService gameService, 
            IDiscordService discordService, 
            ICrackerService crackerService)
        {
            _fileService = fileService;
            _gameService = gameService;
            _discordService = discordService;
            _crackerService = crackerService;

            InitializeServices();
        }

        private void InitializeServices()
        {
            // Subscribe to Cracker Logs
            _crackerService.OnLog += (msg) =>
            {
                // Ensure UI updates happen on the Main Thread
                Dispatcher.UIThread.Post(() => 
                {
                    Logs.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                    // Keep only last 100 logs to prevent memory issues
                    if (Logs.Count > 100) Logs.RemoveAt(0);
                });
            };

            // Subscribe to Discord Events
            _discordService.OnUserReady += async (s, user) =>
            {
                Dispatcher.UIThread.Post(() => UserName = user.Username);
                
                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    var avatarUrl = user.GetAvatarURL(DiscordRPC.User.AvatarFormat.PNG);
                    await LoadUserAvatarAsync(avatarUrl);
                }
            };

            _discordService.OnConnectionFailed += (s, err) =>
            {
                Dispatcher.UIThread.Post(() => 
                {
                    UserName = "No Discord";
                    Logs.Add($"[Warn] {err}");
                });
            };

            // Start Discord RPC
            _discordService.Initialize();
            _discordService.UpdatePresence("Idle", "Waiting for game...");
        }

        // --- Commands ---

        [RelayCommand(CanExecute = nameof(CanInteract))]
        public async Task Browse(IStorageProvider storageProvider)
        {
            var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Game Installation Folder",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                GamePath = result[0].Path.LocalPath;
                await AnalyzeSelectedFolder();
            }
        }

        [RelayCommand(CanExecute = nameof(CanCrack))]
        public async Task ApplyCrack()
        {
            if (string.IsNullOrEmpty(GamePath)) return;

            IsBusy = true;
            StatusMessage = "Applying Crack...";
            _discordService.UpdatePresence("Cracking...", GameName);

            try
            {
                int filesReplaced = await _crackerService.ApplyCrackAsync(GamePath);
                
                StatusMessage = "Done.";
                string discordId = "Anonymous"; // In a real app, you'd cache the ID from the OnUserReady event

                if (filesReplaced > 0)
                {
                    await _crackerService.LogAttemptToApiAsync(true, GameName, discordId, UserName);
                    _discordService.UpdatePresence("Idle", "Crack applied successfully");
                }
                else
                {
                    await _crackerService.LogAttemptToApiAsync(false, GameName, discordId, UserName);
                    _discordService.UpdatePresence("Idle", "Failed to find files");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error occurred";
                Logs.Add($"[Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // --- Helper Methods ---

        private bool CanInteract() => !IsBusy;
        private bool CanCrack() => !IsBusy && !string.IsNullOrEmpty(GamePath);

        private async Task AnalyzeSelectedFolder()
        {
            StatusMessage = "Analyzing...";
            GameName = _gameService.DetectGameNameFromPath(GamePath);
            
            Logs.Add($"[Info] Detected: {GameName}");
            _discordService.UpdatePresence("Selecting Game", GameName);

            // Fetch Cover Image
            var metadata = await _gameService.FetchGameMetadataAsync(GameName);
            
            if (metadata.Found)
            {
                GameName = metadata.Title; // Update to official title
                if (metadata.CoverImageBytes != null)
                {
                    using var stream = new MemoryStream(metadata.CoverImageBytes);
                    GameCover = new Bitmap(stream);
                }
            }
            else
            {
                GameCover = null; // Reset or show placeholder
            }
            
            StatusMessage = "Ready";
        }

        private async Task LoadUserAvatarAsync(string url)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var data = await http.GetByteArrayAsync(url);
                using var stream = new MemoryStream(data);
                
                Dispatcher.UIThread.Post(() => UserAvatar = new Bitmap(stream));
            }
            catch { /* Ignore avatar load errors */ }
        }

        public void Dispose()
        {
            _discordService?.Dispose();
        }
    }
}