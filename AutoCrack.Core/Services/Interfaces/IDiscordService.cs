using System;
using DiscordRPC;

namespace AutoCrack.Core.Services.Interfaces
{
    public interface IDiscordService : IDisposable
    {
        event EventHandler<User> OnUserReady;
        event EventHandler<string> OnConnectionFailed;

        void Initialize();
        void UpdatePresence(string details, string state, string largeImageKey = "app_logo", string largeImageText = "AutoCrack");
        void ClearPresence();
    }
}