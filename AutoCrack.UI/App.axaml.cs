using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using AutoCrack.Core.Services;
using AutoCrack.Core.Services.Interfaces;
using AutoCrack.UI.ViewModels;

namespace AutoCrack.UI
{
    public partial class App : Application
    {
        // Container for our services
        public IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 1. Configure Dependency Injection
            var collection = new ServiceCollection();
            
            // Register Core Services
            collection.AddSingleton<IFileService, FileService>();
            collection.AddSingleton<IGameService, GameService>();
            collection.AddSingleton<IDiscordService, DiscordService>();
            collection.AddSingleton<ICrackerService, CrackerService>();

            // Register ViewModel
            collection.AddTransient<MainViewModel>();

            // Build the provider
            Services = collection.BuildServiceProvider();

            // 2. Launch the Main Window
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Resolve the ViewModel automatically with all dependencies injected
                var viewModel = Services.GetRequiredService<MainViewModel>();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = viewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}