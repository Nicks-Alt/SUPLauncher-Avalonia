using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SUPLauncher.ViewModels;
using SUPLauncher.Views;
using System;
using System.Threading.Tasks;
using SUPLauncher.Helpers;

namespace SUPLauncher;

public partial class App : Application
{
    private MainWindow _mainWindow;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        // Create and show the splash screen
        var splashScreen = new SplashScreen();
        splashScreen.Show();  // Show the splash screen

        await Task.Delay(2000); // Give it a damn second jeez
        // Initialize MainWindow
        _mainWindow = new MainWindow();

        // Start the background task for splash screen timeout and running code
        await Task.Run(async () =>
        {
            // Wait for 5 seconds asynchronously (non-blocking)
            await Task.Delay(5000);

            // Call the methods while splash screen is showing
            await RunBackgroundTasksAsync();

            // Once 5 seconds are up and tasks are done, show the main window
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _mainWindow.Show();  // Show the main window
            });

            // Close the splash screen after the main window has been shown
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                splashScreen.Close();
            });
        });
    }

    private async Task RunBackgroundTasksAsync()
    {
        // These methods are from MainWindow, so we call them on the MainWindow instance
        await Task.Run(() =>
        {
            // Assuming these methods are in MainWindow class
            _mainWindow.SteamCheck();
            _mainWindow.InitValvecmd();
            Telemetry.Update();
        });
    }


}
