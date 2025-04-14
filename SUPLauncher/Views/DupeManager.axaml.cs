using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Platform;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace SUPLauncher.Views;

public partial class DupeManager : Window
{
    private bool _isDragging;
    private Point _dragStartPoint;
    public static Bitmap FolderIcon = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/folder.png")));
    public static Bitmap FileIcon = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/file.png")));
    private string rootPath = $@"{FindGmodFolder()}\garrysmod\data\advdupe2"; // The path to the root folder
    private DispatcherTimer _timer; // Timer for the animation
    private int _dotIndex = 0;  // To keep track of the current dot pattern
    public DupeManager()
    {
        InitializeComponent();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)  // 500ms interval for dot animation
        };
        _timer.Tick += OnTimerTick;  // Attach the Tick event handler
        LoadTreeView(rootPath); // Load the TreeView asynchronously
    }
    private async void LoadTreeView(string rootPath)
    {
        StartLoadingAnimation();
        var rootItem = await CreateTreeItemAsync(rootPath); // Use async loading of directories/files
        FileTree.ItemsSource = new List<TreeViewItem> { rootItem };
        StopLoadingAnimation();
        
    }
    private void StartLoadingAnimation()
    {
        // Start the timer that updates the dots animation
        LoadingText.IsVisible = true;
        _timer.Start();
    }
    private void OnTimerTick(object sender, EventArgs e)
    {
        // Define the dots sequence: ".", "..", "..."
        string[] dots = { ".", "..", "..." };

        // Update the TextBlock with the next dot pattern
        LoadingText.Text = "Loading/Refreshing Dupes" + dots[_dotIndex];

        // Move to the next dot pattern
        _dotIndex = (_dotIndex + 1) % dots.Length;
    }
    private void StopLoadingAnimation()
    {
        // Stop the timer and hide the loading message once the data is loaded
        _timer.Stop();
        LoadingText.IsVisible = false;

    }
    private void TopBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        // Capture pointer and store the initial drag point
        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        e.Pointer.Capture((IInputElement?)sender);
    }

    private void TopBar_PointerMoved(object sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            // Get current pointer position
            var currentPoint = e.GetPosition(this);

            // Calculate the new window position
            var offsetX = currentPoint.X - _dragStartPoint.X;
            var offsetY = currentPoint.Y - _dragStartPoint.Y;

            // Move the window
            Position = new PixelPoint(Position.X + (int)offsetX, Position.Y + (int)offsetY);
        }
    }

    private void TopBar_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        // Release pointer capture and stop dragging
        _isDragging = false;
        e.Pointer.Capture(null);
    }
    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    // Asynchronously create a TreeViewItem for a given directory
    private async Task<TreeViewItem> CreateTreeItemAsync(string path, int i = 0)
    {
        bool isDirectory = Directory.Exists(path);
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name))
            name = path;

        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        stack.Children.Add(new Image
        {
            Source = isDirectory ? FolderIcon : FileIcon,
            Width = 16,
            Height = 16
        });
        stack.Children.Add(new TextBlock { Text = name });

        var item = new TreeViewItem { Header = stack };
        var contextMenu = new ContextMenu();
        // Add context menu for right-click
        var deleteItem = new MenuItem { Header = "Delete" };
        deleteItem.Click += (sender, e) => DeleteFile(path); // Handle delete click
        contextMenu.Items.Add(deleteItem);
        var moveItem = new MenuItem { Header = "Move" };
        moveItem.Click += (sender, e) => MoveFile(path); // Handle move click
        contextMenu.Items.Add(moveItem);
        contextMenu.Items.Add(new Separator());
        var importItem = new MenuItem { Header = "Import File" };
        importItem.Click += (sender, e) => ImportFile(path); // Handle import click
        contextMenu.Items.Add(importItem);

        var importFolder = new MenuItem { Header = "Import Folder" };
        importFolder.Click += (sender, e) => ImportFolder(path); // Handle import click
        contextMenu.Items.Add(importFolder);

        item.ContextMenu = contextMenu;
        contextMenu.Opening += MenuOpening;

        if (isDirectory)
        {
            // Asynchronously load sub-items
            var directories = await Task.Run(() => Directory.GetDirectories(path));
            var files = await Task.Run(() => Directory.GetFiles(path));

            foreach (var dir in directories)
            {
                item.Items.Add(await CreateTreeItemAsync(dir)); // Recursively add sub-directories
            }

            foreach (var file in files)
            {
                item.Items.Add(await CreateTreeItemAsync(file)); // Add files
            }
        }

        return item;
    }
    private void MenuOpening(object sender, CancelEventArgs e)
    {
        // Get the currently hovered/selected item
        var selectedItem = FileTree.SelectedItem;

        // Check if it's the root folder
        if (FileTree.Items?.Cast<object>().FirstOrDefault() == selectedItem) // Yeah i dont fucking know man -Nick
        {
            // Cancel context menu for the root folder
            e.Cancel = true;
        }
    }
    private void UpdateTreeView()
    {
        // Ensure that UI updates are done on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Clear the current items in the TreeView
                FileTree.ItemsSource = null;

                // Load the TreeView again from the root path
                LoadTreeView(rootPath); 
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the update
                Console.WriteLine($"Error updating TreeView: {ex.Message}");
            }
        });
    }


    // Delete a file or directory
    private void DeleteFile(object parameter)
    {
        if (parameter is string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true); // Delete directory and its contents
                }
                UpdateTreeView();
            }
            catch (Exception ex)
            {
                // Handle errors, e.g., file in use, permission errors
                Console.WriteLine($"Error deleting: {ex.Message}");
            }
        }
    }
    private void MoveFile(string path)
    {
        string FileToMove = path;

        // Open Folder Dialog for destination selection
        OpenFolderDialog dialog = new OpenFolderDialog { Title = "Select Destination Folder", Directory = rootPath };
        dialog.ShowAsync(this).ContinueWith(task =>
        {
            if (task.Result != null)
            {
                string destinationFolder = task.Result;

                try
                {
                    // Check if the source is a directory or file
                    if (Directory.Exists(FileToMove))
                    {
                        // Handle directory move
                        string newDirectoryPath = Path.Combine(destinationFolder, Path.GetFileName(FileToMove));

                        // Check if destination already exists
                        if (Directory.Exists(newDirectoryPath))
                        {
                            Console.WriteLine($"Error: A directory with the same name already exists in the destination.");
                            return;
                        }

                        Directory.Move(FileToMove, newDirectoryPath);
                    }
                    else if (File.Exists(FileToMove))
                    {
                        // Handle file move
                        string newFilePath = Path.Combine(destinationFolder, Path.GetFileName(FileToMove));

                        // Check if destination already exists
                        if (File.Exists(newFilePath))
                        {
                            Console.WriteLine($"Error: A file with the same name already exists in the destination.");
                            return;
                        }

                        File.Move(FileToMove, newFilePath);
                    }
                    else
                    {
                        Console.WriteLine($"Error: The specified path does not exist as either a file or directory.");
                        return;
                    }

                    // Update the TreeView to reflect the changes
                    UpdateTreeView();
                }
                catch (Exception ex)
                {
                    // Handle any errors during move
                    Console.WriteLine($"Error moving file/folder: {ex.Message}");
                }
            }
        });
    }


    // Import a new file into the selected directory
    private void ImportFile(object parameter)
    {
        if (parameter is string directoryPath)
        {
            var dialog = new OpenFileDialog { 
                Title = "Select File to Import",
                Directory = $"{Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), "Downloads")}",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "Dupes",
                        Extensions = new List<string> {"txt"}
                    },
                }
            };
            dialog.ShowAsync(this).ContinueWith(task =>
            {
                if (task.Result != null && task.Result.Length > 0)
                {
                    string sourcePath = task.Result[0];
                    string destinationPath = Path.Combine(directoryPath, Path.GetFileName(sourcePath));

                    try
                    {
                        File.Copy(sourcePath, destinationPath); // Copy the selected file to the directory
                        UpdateTreeView();
                    }
                    catch (Exception ex)
                    {
                        // Handle errors, e.g., file already exists, permission errors
                        Console.WriteLine($"Error importing file: {ex.Message}");
                    }
                }
            });
        }
    }
    private async void ImportFolder(object parameter)
    {
        if (parameter is string directoryPath)
        {
            var dialog = new OpenFolderDialog();
            {
                Title = "Select Folder to Import";
            };
            var selectedFolder = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                string[] allowedExtensions = { ".txt" };

                // Get the relative path from the selected folder to preserve directory structure
                string relativePath = selectedFolder.Substring(selectedFolder.LastIndexOf("\\"));
                string destinationRoot = parameter.ToString() + relativePath;

                // Process all files (including subdirectories)
                var textFiles = Directory
                    .EnumerateFiles(selectedFolder, "*.*", SearchOption.AllDirectories)
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .ToList();

                try
                {
                    // Create all directories in the destination first
                    foreach (var dir in Directory.EnumerateDirectories(selectedFolder, "*", SearchOption.AllDirectories))
                    {
                        string relativeDir = dir.Substring(selectedFolder.Length);
                        Directory.CreateDirectory(Path.Combine(destinationRoot, relativeDir.TrimStart('\\')));
                    }

                    // Also create the root directory if it doesn't exist
                    Directory.CreateDirectory(destinationRoot);
                }
                catch (Exception) { }

                if (textFiles.Any())
                {
                    foreach (var file in textFiles)
                    {
                        string relativeFilePath = file.Substring(selectedFolder.Length);
                        string destinationPath = Path.Combine(destinationRoot, relativeFilePath.TrimStart('\\'));

                        // Ensure the target directory exists (in case we missed it)
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        File.Move(file, destinationPath);
                    }
                    UpdateTreeView();
                }
            }
        }
    }
    private static string FindGmodFolder()
    {
        if (Registry.GetValue(Environment.Is64BitOperatingSystem ? @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam" : @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) is string steamInstallPath)
        {
            Console.WriteLine($"Found steam install: {steamInstallPath}");

            string steamLibraryVdf = System.IO.Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");

            if (File.Exists(steamLibraryVdf))
            {
                Console.WriteLine($"Found steamlibrary vdf: {steamLibraryVdf}");
                VProperty libraries = VdfConvert.Deserialize(File.ReadAllText(steamLibraryVdf));

                foreach (VProperty library in libraries.Value.Children<VProperty>())
                {
                    foreach (VProperty path in library.Value.Children<VProperty>().Where((v) => v.Key == "path"))
                    {
                        string gmodFolder = System.IO.Path.Combine(path.Value.ToString(), "steamapps", "common", "GarrysMod");

                        if (Directory.Exists(gmodFolder) && File.Exists(System.IO.Path.Combine(gmodFolder, "garrysmod", "cfg", "mount.cfg")))
                        {
                            Console.WriteLine($"Found gmod folder: {gmodFolder}");
                            return gmodFolder;
                        }
                    }
                }
            }
        }
        return "";
    }
}

static class Extensions
{
    public static IEnumerable<object> AddOrCreate(this IEnumerable<object> list, object item)
    {
        var temp = new List<object>(list ?? new List<object>());
        temp.Add(item);
        return temp;
    }
}