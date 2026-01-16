using System;
using AutoCrack.Core.Services.Interfaces;
using DiscordRPC;
using DiscordRPC.Logging;

namespace AutoCrack.Core.Services
{
    public class DiscordService : IDiscordService
    {
        private DiscordRpcClient? _client;
        private readonly string _clientId;

        public event EventHandler<User>? OnUserReady;
        public event EventHandler<string>? OnConnectionFailed;

        public DiscordService()
        {
            _clientId = Constants.DiscordClientId;
        }

        public void Initialize()
        {
            try
            {
                _client = new DiscordRpcClient(_clientId)
                {
                    Logger = new ConsoleLogger(LogLevel.Warning)
                };

                _client.OnReady += (sender, e) => OnUserReady?.Invoke(this, e.User);
                
                _client.OnConnectionFailed += (sender, e) => 
                    OnConnectionFailed?.Invoke(this, "Connection failed. Is Discord running?");
                
                _client.OnError += (sender, e) => 
                    OnConnectionFailed?.Invoke(this, $"Discord Error: {e.Message}");

                _client.Initialize();
            }
            catch (Exception ex)
            {
                OnConnectionFailed?.Invoke(this, $"Init Error: {ex.Message}");
            }
        }

        public void UpdatePresence(string details, string state, string largeImageKey = "app_logo", string largeImageText = "AutoCrack")
        {
            if (_client == null || !_client.IsInitialized) return;

            _client.SetPresence(new RichPresence()
            {
                Details = details,
                State = state,
                Assets = new Assets()
                {
                    LargeImageKey = largeImageKey,
                    LargeImageText = largeImageText
                },
                Timestamps = Timestamps.Now
            });
        }

        public void ClearPresence()
        {
            _client?.ClearPresence();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}