using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Newtonsoft.Json;

public class AutoUpdater
{
    private readonly HttpClient _httpClient;
    private readonly Window _mainWindow;
    private readonly string _owner;
    private readonly string _repo;

    public AutoUpdater(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _owner = "Nicks-Alt";
        _repo = "SUPLauncher-Avalonia";

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new System.Net.Http.Headers.ProductInfoHeaderValue("AutoUpdater", version));
    }

    public async Task CheckAndShowUpdateDialog()
    {
        try
        {
            var apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var response = await _httpClient.GetStringAsync(apiUrl);
            dynamic release = JsonConvert.DeserializeObject(response);

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestVersion = new Version(release.tag_name.ToString().TrimStart('v', 'V'));

            if (latestVersion > currentVersion)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    // Ensure main window is visible
                    if (!_mainWindow.IsVisible)
                    {
                        _mainWindow.Show();
                    }

                    // Create buttons
                    var updateNowButton = new Button
                    {
                        Content = "Update Now",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    var laterButton = new Button
                    {
                        Content = "Later",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 10, 10, 0)
                    };

                    // Create dialog
                    var dialog = new Window
                    {
                        Title = "Update Available",
                        Width = 450,
                        Height = 300,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        SystemDecorations = SystemDecorations.None,
                        CanResize = false,
                        Content = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Children =
                        {
                            new TextBlock
                            {
                                Text = $"Version {latestVersion} is available!",
                                FontWeight = FontWeight.Bold,
                                FontSize = 16,
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new TextBlock
                            {
                                Text = $"Your version: {currentVersion}",
                                Margin = new Thickness(0, 0, 0, 20)
                            },
                            new ScrollViewer
                            {
                                Height = 120,
                                Content = new TextBlock
                                {
                                    Text = release.body.ToString(),
                                    TextWrapping = TextWrapping.Wrap
                                },
                                Margin = new Thickness(0, 0, 0, 20)
                            },
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Children = { laterButton, updateNowButton }
                            }
                        }
                        }
                    };

                    // Button handlers
                    updateNowButton.Click += async (s, e) =>
                    {
                        updateNowButton.IsEnabled = false;
                        dialog.Close();
                        await StartUpdateProcess(release);
                    };

                    laterButton.Click += (s, e) => dialog.Close();

                    await dialog.ShowDialog(_mainWindow);
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    private async Task ShowUpdateDialog(dynamic release, string currentVersion, string latestVersion)
    {
        var dialog = new Window
        {
            Title = "Update Available",
            Width = 450,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.BorderOnly,
            CanResize = false
        };

        var updateNowButton = new Button
        {
            Content = "Update Now",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var laterButton = new Button
        {
            Content = "Later",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 10, 0)
        };

        // Disable button during update process
        updateNowButton.Click += async (s, e) =>
        {
            updateNowButton.IsEnabled = false;
            await StartUpdateProcess(release);
        };

        laterButton.Click += (s, e) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock
                {
                    Text = $"Version {latestVersion} is available!",
                    FontWeight = FontWeight.Bold,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                new TextBlock
                {
                    Text = $"Your version: {currentVersion}",
                    Margin = new Thickness(0, 0, 0, 20)
                },
                new ScrollViewer
                {
                    Height = 120,
                    Content = new TextBlock
                    {
                        Text = release.body.ToString(),
                        TextWrapping = TextWrapping.Wrap
                    },
                    Margin = new Thickness(0, 0, 0, 20)
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { laterButton, updateNowButton }
                }
            }
        };

        await dialog.ShowDialog(_mainWindow);
    }

    private async Task StartUpdateProcess(dynamic release)
    {
        string tempDownloadPath = Path.Combine(Path.GetTempPath(), "app_update.exe");

        try
        {
            // Create progress UI
            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 20
            };

            var progressText = new TextBlock
            {
                Text = "Downloading update...",
                Margin = new Thickness(0, 10, 0, 0)
            };

            var progressDialog = new Window
            {
                Title = "Updating Application",
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SystemDecorations = SystemDecorations.BorderOnly,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Children = { progressBar, progressText }
                }
            };

            // Show progress immediately
            progressDialog.Show();

            // Find download URL
            string downloadUrl = null;
            foreach (var asset in release.assets)
            {
                if (asset.name.ToString().EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.browser_download_url.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                progressDialog.Close();
                await ShowError("No executable found in release");
                return;
            }

            // Download with progress
            bool downloadSuccess = false;
            await Task.Run(async () =>
            {
                try
                {
                    using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        long totalBytes = response.Content.Headers.ContentLength ?? 0;
                        long receivedBytes = 0;
                        var buffer = new byte[8192];

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fs = new FileStream(tempDownloadPath, FileMode.Create))
                        {
                            int bytesRead;
                            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                            {
                                await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                                receivedBytes += bytesRead;

                                double percent = totalBytes > 0 ? receivedBytes * 100.0 / totalBytes : 0;
                                Dispatcher.UIThread.Post(() =>
                                {
                                    progressBar.Value = percent;
                                    progressText.Text = $"Downloaded: {percent:F0}% ({receivedBytes / (1024 * 1024)}MB / {totalBytes / (1024 * 1024)}MB)";
                                });
                            }
                        }
                    }
                    downloadSuccess = true;
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        progressDialog.Close();
                        ShowError($"Download failed: {ex.Message}");
                    });
                }
            });

            if (!downloadSuccess || !File.Exists(tempDownloadPath))
            {
                await ShowError("Download failed or file corrupted");
                return;
            }

            // Switch to applying update
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                progressText.Text = "Applying update...";
                progressBar.IsIndeterminate = true;
            });

            // Prepare restart script
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string restartScript = Path.Combine(Path.GetTempPath(), "update_script.bat");

            string scriptContent = $@"
@echo off
timeout /t 1 /nobreak >nul
taskkill /F /PID {Process.GetCurrentProcess().Id} >nul 2>&1
move /Y ""{tempDownloadPath}"" ""{currentExe}"" >nul 2>&1
start """" ""{currentExe}""
del ""%~f0"" >nul 2>&1
";

            await File.WriteAllTextAsync(restartScript, scriptContent);

            // Launch and exit
            Process.Start(new ProcessStartInfo
            {
                FileName = restartScript,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            });

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            await ShowError($"Update failed: {ex.Message}");
            try { File.Delete(tempDownloadPath); } catch { }
        }
    }

    private async Task ShowError(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.BorderOnly,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 20, 0, 0)
                        }
                    }
                }
            };

            dialog.FindControl<Button>("Button").Click += (s, e) => dialog.Close();
            await dialog.ShowDialog(_mainWindow);
        });
    }
}