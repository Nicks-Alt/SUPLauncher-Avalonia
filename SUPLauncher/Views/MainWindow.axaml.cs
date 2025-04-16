#region Depends 
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using Avalonia.Media.Imaging;
using SUPLauncher.Helpers;
using MsBox.Avalonia;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using System.Linq;
using System.IO.Compression;
using System.Threading;
using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Input;
using DiscordRPC;
using System.Reactive;
#endregion

namespace SUPLauncher.Views;
/*
    Questions for peng/king:

    Golden poop script?
    Addntl features??
 */
public partial class MainWindow : Window
{
    #region Globals
    private int danktownPlayerCount = 0;
    private int c18PlayerCount = 0;
    private int cwrpPlayerCount = 0; 
    private int cwrp2PlayerCount = 0;
    private int cwrp3PlayerCount = 0;
    private int danktownMaxPly = 0;
    private int c18MaxPly = 0;
    private int cwrpMaxPly = 0;
    private int cwrp2MaxPly = 0;
    private int cwrp3MaxPly = 0;
    public static string rp1 = Dns.GetHostEntry("rp.superiorservers.co").AddressList[0].ToString();
    public static string rp2 = Dns.GetHostEntry("rp2.superiorservers.co").AddressList[0].ToString();
    public static string cwrp1 = Dns.GetHostEntry("cwrp.superiorservers.co").AddressList[0].ToString();
    public static string cwrp2 = Dns.GetHostEntry("cwrp2.superiorservers.co").AddressList[0].ToString();
    public static string cwrp3 = Dns.GetHostEntry("cwrp3.superiorservers.co").AddressList[0].ToString();
    private static SteamBridge steam = new SteamBridge();
    public static string Username = "";
    private DispatcherTimer tmrAFK = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(20000)};
    private DispatcherTimer tmrClipboard = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000)};
    private DispatcherTimer tmrRefresh = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10000) };
    private DispatcherTimer tmrUpdatePresence = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(2) };
    public static Bitmap MEMBER = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/MEMBER.png"))); // Rank images
    public static Bitmap VIP = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/VIP.png")));
    public static Bitmap MOD = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/MOD.png")));
    public static Bitmap ADMIN = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/ADMIN.png")));
    public static Bitmap DOUBLE = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/DOUBLE.png")));
    public static Bitmap SUPER = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/SUPER.png")));
    public static Bitmap COUNCIL = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/co-blue.png")));
    public static Bitmap CC = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/cc-forumbar.png")));
    public static Bitmap ROOT = new Bitmap(AssetLoader.Open(new Uri("avares://SUPLauncher/Assets/Images/ROOT.png")));
    private string _clipboardLastText = ""; // Clipboard previous text
    private bool blnAllowRefresh = true; // For player count refresh
    private bool _isDragging;
    private Avalonia.Point _dragStartPoint;
    private DiscordRpcClient discord = new DiscordRpcClient("594668399653814335") { Logger = new DiscordRPC.Logging.ConsoleLogger(DiscordRPC.Logging.LogLevel.Info, true) };
    #endregion

    #region Main
    public MainWindow()
    {
        InitializeComponent();
        GetPlayerCountAllServers();
        InitUser();
        tmrAFK.Tick += new EventHandler(tmrAFK_Tick);
        tmrRefresh.Tick += new EventHandler(tmrRefresh_Tick);
        tmrUpdatePresence.Tick += new EventHandler(tmrUpdatePresence_Tick);
        if (chkStaff.IsChecked == true) { tmrClipboard.Tick += new EventHandler(tmrClipboard_Tick); tmrClipboard.Start(); }
        tmrAFK.Start();
        tmrRefresh.Start();
        tmrUpdatePresence.Start();
        discord.Initialize(); // Initialize discord presence
        discord.SetPresence(new RichPresence()
        {
            Details = "Waiting to join a server...",
            State = "SuperiorServers.co",
            Timestamps = new Timestamps() { Start = DateTime.Now },
            Buttons = new DiscordRPC.Button[]
                {
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                                new DiscordRPC.Button(){ Label = "Bans", Url = "https://superiorservers.co/bans" }
                },
            Assets = new Assets()
            {
                LargeImageKey = "suplogo",
                LargeImageText = "SuperiorServers.co"
            }
        });

    }
    #endregion

    #region Events

    private async void btnDupeManager_Click(object sender, RoutedEventArgs args)
    {
        _ = new DupeManager().ShowDialog(this);
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
    private async void btnRefresh_Click(object sender, RoutedEventArgs args)
    {
        if (blnAllowRefresh)
            GetPlayerCountAllServers();
        // Find the Image control using FindControl
        var reloadImage = this.FindControl<Image>("ReloadImage");

        // Access the RotateTransform applied to the Image
        if (reloadImage?.RenderTransform is RotateTransform rotateTransform)
        {
            btnRefresh.IsEnabled = false;
            const double totalAngle = 360; // Full rotation
            const double duration = 100; // Total animation time in milliseconds
            const int steps = 60;         // Number of animation steps (frames)
            double angleIncrement = totalAngle / steps;
            double stepDuration = duration / steps;

            for (int i = 0; i < steps; i++)
            {
                rotateTransform.Angle += angleIncrement;
                await Task.Delay((int)stepDuration);
            }

            // Reset to 0 to avoid accumulation errors
            rotateTransform.Angle = 0;
        }
        btnRefresh.IsEnabled = true;
        blnAllowRefresh = false;
    }
    private void btnClose(object sender, RoutedEventArgs args)
    {
        this.Close();
    }
    private void btnMinimize(object sender, RoutedEventArgs args)
    {
        this.WindowState = WindowState.Minimized;
    }
    private void imgPointerPressed(object sender, RoutedEventArgs args) => Process.Start(new ProcessStartInfo
    {
        UseShellExecute = true,
        FileName = "https://github.com/Nicks-Alt/SUPLauncher-Avalonia/releases/latest"
    });
    private void btnDanktown_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit  -w 300 -h 300 -single_core -nojoy -low -nosound -sw -nopix -novid -noshaderapi -nopreload -nopreloadmodels -multirun +cl_mouselook 0 +connect {rp1}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -w {Screens.Primary.Bounds.Width} -h {Screens.Primary.Bounds.Height} -windowed -noborder +cl_mouselook 1 +connect {rp1}" 
            });
        
    }
    private void btnC18_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +cl_mouselook 0 +connect {rp2}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -w {Screens.Primary.Bounds.Width} -h {Screens.Primary.Bounds.Height} -windowed -noborder +cl_mouselook 1 +connect {rp2}"
            });
    }
    private void btnCWRP_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +connect {cwrp1}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -w {Screens.Primary.Bounds.Width} -h {Screens.Primary.Bounds.Height} -windowed -noborder +connect {cwrp1}"
            });
    }
    private void btnCWRP2_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +connect {cwrp2}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -w {Screens.Primary.Bounds.Width} -h {Screens.Primary.Bounds.Height} -windowed -noborder +connect {cwrp2}"
            });
    }
    private void btnCWRP3_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +connect {cwrp3}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//-64bit -w {Screens.Primary.Bounds.Width} -h {Screens.Primary.Bounds.Height} -windowed -noborder +connect {cwrp3}"
            });
    }
    private void Teamspeak(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = $"ts3server://TS.SuperiorServers.co:9987"
        });
    }
    private void Discord(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = $"https://discordapp.com/invite/2mKYFwhj"
        });
    }
    private void Forums(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = $"https://forum.superiorservers.co/"
        });
    }
    private void chkAFK_CheckChanged(object sender, RoutedEventArgs args)
    {
        try
        {
            Process.Start(new ProcessStartInfo(){
                FileName = "cmd.exe",
                Arguments = "/C taskkill /im gmod.exe /f /t",
                UseShellExecute = false,
                CreateNoWindow = true
            }).Start();
            Process.Start(new ProcessStartInfo(){
                FileName = "cmd.exe",
                Arguments = "/C taskkill /im hl2.exe /f /t",
                UseShellExecute = false,
                CreateNoWindow = true
            }).Start();
            
            /*
             
             THIS IS CANCER AND I DO NOT CONDONE THIS^^^
             -Nick

             */
        }
        catch (Exception){} // ignore exceptions
    }
    private void chkStaff_CheckChanged(object sender, RoutedEventArgs args)
    {
        if (chkStaff.IsChecked == true) { tmrClipboard.Tick += tmrClipboard_Tick; tmrClipboard.Start(); }
        else tmrClipboard.Stop();
    }
    private void chkShowDarkRP_CheckChanged(object sender, RoutedEventArgs args)
    {
        if (chkShowDarkRP.IsChecked == true)
        {
            foreach (Control c in StatsGrid.Children)
            {
                if (c.Name is null) continue;
                if (c.Name.Contains("drp"))
                    c.IsVisible = true;
            }
            Canvas.SetTop(chkShowDarkRP, -35);
            if (drpOrg.Text == "") { drpOrg.IsVisible = false; drpOrgText.IsVisible = false; drpBorderOrg1.IsVisible = false; drpBorderOrg2.IsVisible = false; Canvas.SetTop(chkShowDarkRP, -55); }
        }
        else
        {
            foreach (Control c in StatsGrid.Children)
            {
                if (c.Name is null) continue;
                if (c.Name.Contains("drp"))
                    c.IsVisible = false;
            }
            Canvas.SetTop(chkShowDarkRP, -95);
        }
    }
    private void tmrAFK_Tick(object sender, EventArgs e)
    {
        if (chkAFK.IsChecked == true && GetGarrysModProcess() != null)
        {
            Task.Factory.StartNew(() => // Do Task Factory because no hanging! // Thank you to Jaiden/Particles for helping with this!
            {
                #region AFK Macro
                SendAFKCommand("\"rp spawn; echo [SUPLauncher] Attempting to spawn...\"");
                Thread.Sleep(500);
                SendAFKCommand("\"rp selectweapon pocket; echo [SUPLauncher] Selected pocket\"");
                Thread.Sleep(500);
                SendAFKCommand("\"+lookdown; echo [SUPLauncher] Ran lookdown\"");
                Thread.Sleep(500);
                SendAFKCommand("\"rp poop; echo [SUPLauncher] Ran /poop\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("+moveright");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveright");
                SendAFKCommand("+moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                Thread.Sleep(200);
                SendAFKCommand("-moveleft");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"+attack\""); Thread.Sleep(10);
                Thread.Sleep(10);
                SendAFKCommand("\"-attack\"");
                SendAFKCommand("\"echo [SUPLauncher] Thank you for AFKing on SUP!\"");
                #endregion
            });
        }
    }
    private void tmrUpdatePresence_Tick(object sender, EventArgs e)
    {
        GetCurrentServer(); // For Discord status updates
    }
    private void tmrClipboard_Tick(object sender, EventArgs e)
    {
        try
        {
            Regex SteamID64Regex = new Regex(@"^7\d{16}$");
            Regex SteamIDRegex = new Regex(@"^STEAM_(0|1):[01]:\d{1,10}$");
            var clipboard = TopLevel.GetTopLevel(this).Clipboard;
            string text = "";
            if (clipboard.GetTextAsync().Result != null)
            {
                text = clipboard.GetTextAsync().Result.ToString();
                if ((SteamID64Regex.IsMatch(text) || SteamIDRegex.IsMatch(text)))
                {
                    if ((text != _clipboardLastText) && (SteamID64Regex.IsMatch(text) || SteamIDRegex.IsMatch(text)))
                    {
                        string targUsername = GetNameFromSteamID(text);
                        List<string> bans = GetBans(text);
                        SendAFKCommand($"\"say /pm {steam.GetSteamId()} [SUP Launcher POs] {targUsername}({text}) has {bans.Count} POs.\"");
                        for (int i = 0; i < bans.Count; i++) // Length, reason, timeocurred
                        {
                            Thread.Sleep(2000); // You must wait 1 seconds before running "pm" again!
                            string[] temp = bans[i].Split(",");
                            SendAFKCommand($"\"say /pm {steam.GetSteamId()} [SUP Launcher POs] #{i + 1}: Banned for {FormatDuration(Convert.ToDouble(temp[0]))} for {temp[1]} on {DateTime.UnixEpoch.AddSeconds(Convert.ToDouble(temp[2])).ToShortDateString()}\"");
                        }
                        _clipboardLastText = text.ToString();
                    }
                }
            }

        }
        catch (Exception) { }
    }
    private void tmrRefresh_Tick(object sender, EventArgs e) 
    {
        if (blnAllowRefresh)
            blnAllowRefresh = false;
        else
            blnAllowRefresh = true;
    
    }

    #endregion

    #region Helpers
    [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int AllocConsole();

    private const int STD_OUTPUT_HANDLE = -11;
    private const int MY_CODE_PAGE = 437;
    private async void DownloadTextures(object sender, RoutedEventArgs args)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Download CSS Textures?", "Are you sure you want to download the CSS textures for Garry's Mod?\n\n WARNING: THIS WILL TAKE SOME TIME!", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Info, WindowStartupLocation.CenterOwner);
        var result = await box.ShowAsync();
        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
        {
            if (!(Directory.Exists($"{FindGmodFolder()}\\garrysmod\\addons\\css-content-gmodcontent")))
            {
                AllocConsole();
                IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true);
                FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                StreamWriter standardOutput = new StreamWriter(fileStream);
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
                Console.WriteLine("DOWNLOADING CSS CONTENT. DO NOT CLOSE THIS WINDOW UNTIL PROCESS IS FINISHED!");
                string url = "https://suplauncher.s3.us-east-2.amazonaws.com/css-content-gmodcontent.zip"; // Replace with your download URL
                string downloadPath = "downloaded.zip";      // Temporarily downloaded file
                string extractPath = $"{FindGmodFolder()}\\garrysmod\\addons";    // Directory to extract contents
                Console.WriteLine("Downloading file...");
                DownloadFile(url, downloadPath);

                Console.WriteLine("Extracting contents...");
                ExtractZip(downloadPath, extractPath);

                Console.WriteLine("Cleaning up...");
                File.Delete(downloadPath); // Delete the downloaded zip file

                Console.WriteLine("Process completed successfully. Press any key to close the launcher...");
                Console.ReadKey();
                this.Close();
            }
            else
                await MessageBoxManager.GetMessageBoxStandard("Error", "CSS textures already installed.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
        }
    }
    /// <summary>
    /// Gets the server name and IP the provided steam user is on
    /// </summary>
    /// <param name="steamID">The steamid to use</param>
    /// <param name="normalState">Whether or not it is normally called via timer or not.</param>
    void GetCurrentServer()
    {
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Secure security protocol for querying the steam API
            HttpWebRequest request = WebRequest.CreateHttp("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=7875E26FC3C740C9901DDA4C6E74EB4E&steamids=" + steam.GetSteamId());
            request.UserAgent = "Nick";
            WebResponse response = null;
            response = request.GetResponse(); // Get Response from webrequest
            StreamReader sr = new StreamReader(response.GetResponseStream()); // Create stream to access web data
            var rawResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());
            string ip = rawResults.response.players.First.gameserverip.ToString();
            string playerName = rawResults.response.players.First.personaname.ToString();
            discord.RegisterUriScheme("4000", executable: "explorer steam://rungameid/4000");
            if (ip == $"{rp1}:27015")
            {
                // Playing on DT
                discord.SetPresence(new RichPresence()
                {
                    Buttons = new DiscordRPC.Button[]
                            {
                                new DiscordRPC.Button() { Label = "Join", Url = $"steam://connect/{rp1}:27015" },
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                            },
                    Details = "Playing on Danktown",
                    State = "SuperiorServers.co",
                    Timestamps = new Timestamps() { Start = DateTime.Now },
                    Party = new Party()
                    {
                        ID = "balls",
                        Size = danktownPlayerCount,
                        Max = danktownMaxPly,
                        Privacy = Party.PrivacySetting.Public
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "suplogo",
                        LargeImageText = "SuperiorServers.co",
                    },

                });
            }
            else if (ip == $"{rp2}:27015")
            {
                // Playing on C18
                discord.SetPresence(new RichPresence()
                {
                    Buttons = new DiscordRPC.Button[]
                            {
                                new DiscordRPC.Button() { Label = "Join", Url = $"steam://connect/{rp2}:27015" },
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                            },
                    Details = "Playing on C18",
                    State = "SuperiorServers.co",
                    Timestamps = new Timestamps() { Start = DateTime.Now },
                    Party = new Party()
                    {
                        ID = "balls2",
                        Size = c18PlayerCount,
                        Max = c18MaxPly,
                        Privacy = Party.PrivacySetting.Public
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "suplogo",
                        LargeImageText = "SuperiorServers.co"
                    }
                });
            }
            else if (ip == $"{cwrp1}:27015")
            {
                // Playing on S1
                discord.SetPresence(new RichPresence()
                {
                    Buttons = new DiscordRPC.Button[]
                            {
                                new DiscordRPC.Button() { Label = "Join", Url = $"steam://connect/{cwrp1}:27015" },
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                            },
                    Details = "Playing on S1",
                    State = "SuperiorServers.co",
                    Timestamps = new Timestamps() { Start = DateTime.Now },
                    Party = new Party()
                    {
                        ID = "balls4",
                        Size = cwrpPlayerCount,
                        Max = cwrpMaxPly,
                        Privacy = Party.PrivacySetting.Public
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "suplogo",
                        LargeImageText = "SuperiorServers.co"
                    }
                });
            }
            else if (ip == $"{cwrp2}:27015")
            {
                // Playing on S2
                discord.SetPresence(new RichPresence()
                {
                    Buttons = new DiscordRPC.Button[]
                            {
                                new DiscordRPC.Button() { Label = "Join", Url = $"steam://connect/{cwrp2}:27015" },
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                            },
                    Details = "Playing on S2",
                    State = "SuperiorServers.co",
                    Timestamps = new Timestamps() { Start = DateTime.Now },
                    Party = new Party()
                    {
                        ID = "balls5",
                        Size = cwrp2PlayerCount,
                        Max = cwrp2MaxPly,
                        Privacy = Party.PrivacySetting.Public
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "suplogo",
                        LargeImageText = "SuperiorServers.co"
                    }
                });
            }
            else if (ip == $"{cwrp3}:27015")
            {
                // Playing on S3
                discord.SetPresence(new RichPresence()
                {
                    Buttons = new DiscordRPC.Button[]
                            {
                                new DiscordRPC.Button() { Label = "Join", Url = $"steam://connect/{cwrp2}:27015" },
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                            },
                    Details = "Playing on CWRP #2",
                    State = "SuperiorServers.co",
                    Timestamps = new Timestamps() { Start = DateTime.Now },
                    Party = new Party()
                    {
                        ID = "balls6",
                        Size = cwrp3PlayerCount,
                        Max = cwrp3MaxPly,
                        Privacy = Party.PrivacySetting.Public
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "suplogo",
                        LargeImageText = "SuperiorServers.co"
                    }
                });
            }
        }
        catch (Exception)
        {
            // Revert to default presence if shit breaks
            discord.SetPresence(new RichPresence()
            {
                Details = "Waiting to join a server...",
                State = "SuperiorServers.co",
                Timestamps = new Timestamps() { Start = DateTime.Now },
                Buttons = new DiscordRPC.Button[]
                {
                                new DiscordRPC.Button(){ Label = "Forums", Url = "https://superiorservers.co/" },
                                new DiscordRPC.Button(){ Label = "Bans", Url = "https://superiorservers.co/bans" }
                },
                Assets = new Assets()
                {
                    LargeImageKey = "suplogo",
                    LargeImageText = "SuperiorServers.co"
                }
            });
        }
    }
    public async void SteamCheck()
    {
        if (Process.GetProcessesByName("steam").Length == 0)
        {
            await MessageBoxManager.GetMessageBoxStandard("ERROR", "Steam not initialized! Make sure steam is running then run this program again!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            this.Close();
        }
    }
    static Process[] GetGarrysModProcess()
    {
        var temp = Process.GetProcessesByName("gmod");
        if (temp.Length == 0)
            temp = Process.GetProcessesByName("hl2");
        if (temp.Length == 0)
            temp = null;
        return temp;
    }
    public void InitValvecmd()
    {
        if (!File.Exists("valvecmd.exe"))
            using (FileStream fsDst = new FileStream("valvecmd.exe", FileMode.CreateNew, FileAccess.Write))
            {
                byte[] bytes = Properties.Resources.valvecmd;
                fsDst.Write(bytes, 0, bytes.Length);
                fsDst.Close();
                fsDst.Dispose();
            }
    }
    private string GetNameFromSteamID(string steamid)
    {
        string Url = $"https://superiorservers.co/api/profile/{steamid}";
        CookieContainer cookieJar = new CookieContainer();
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
        request.CookieContainer = cookieJar;
        request.Accept = @"text/html, application/xhtml+xml, */*";
        request.Referer = @"https://superiorservers.co/api";
        request.Headers.Add("Accept-Language", "en-GB");
        request.UserAgent = @"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)";
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string htmlString;
        using (var reader = new StreamReader(response.GetResponseStream())) { htmlString = reader.ReadToEnd(); }
        string username = JsonDocument.Parse(htmlString).RootElement.GetProperty("Badmin").GetProperty("Name").ToString();
        return username;
    }
    private List<string> GetBans(string steamid)
    {
        string Url = $"https://superiorservers.co/api/profile/{steamid}";
        CookieContainer cookieJar = new CookieContainer();
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
        request.CookieContainer = cookieJar;
        request.Accept = @"text/html, application/xhtml+xml, */*";
        request.Referer = @"https://superiorservers.co/api";
        request.Headers.Add("Accept-Language", "en-GB");
        request.UserAgent = @"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)";
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string htmlString;
        using (var reader = new StreamReader(response.GetResponseStream())) { htmlString = reader.ReadToEnd(); }
        JsonArray banArray = JsonSerializer.Deserialize<JsonArray>(JsonDocument.Parse(htmlString).RootElement.GetProperty("Badmin").GetProperty("Bans"));
        List<string> list = new List<string>(); // Length, Reason, Time.
        foreach (var ban in banArray)
        {
            list.Add($"{ban["Length"]},{ban["Reason"]},{ban["Time"]}");
        }
        return list;
    }
    public static string FormatDuration(double seconds)
    {
        const int SecondsInMinute = 60;
        const int SecondsInHour = 3600;
        const int SecondsInDay = 86400; // 24 * 3600
        const int SecondsInWeek = 604800; // 7 * 24 * 3600

        if (seconds >= SecondsInWeek)
        {
            int weeks = (int)(seconds / SecondsInWeek);
            return $"{weeks} week{(weeks > 1 ? "s" : "")}";
        }
        else if (seconds >= SecondsInDay)
        {
            int days = (int)(seconds / SecondsInDay);
            return $"{days} day{(days > 1 ? "s" : "")}";
        }
        else if (seconds >= SecondsInHour)
        {
            int hours = (int)(seconds / SecondsInHour);
            return $"{hours} hour{(hours > 1 ? "s" : "")}";
        }
        else if (seconds >= SecondsInMinute)
        {
            int minutes = (int)(seconds / SecondsInMinute);
            return $"{minutes} minute{(minutes > 1 ? "s" : "")}";
        }
        else
        {
            return $"{(int)seconds} second{(seconds > 1 ? "s" : "")}";
        }
    }
    private void SendAFKCommand(string cmd)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "valvecmd.exe";
        startInfo.Arguments = cmd;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = false;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        Process processTemp = new Process();
        processTemp.StartInfo = startInfo;
        processTemp.EnableRaisingEvents = false;
        processTemp.Start();
    }
    static void DownloadFile(string url, string downloadPath)
    {
        try
        {
            // Create a web request to the URL
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            CookieContainer cookieJar = new CookieContainer();
            request.CookieContainer = cookieJar;
            request.Accept = @"text/html, application/xhtml+xml, */*";
            request.Referer = @"https://superiorservers.co/api";
            request.Headers.Add("Accept-Language", "en-GB");
            request.UserAgent = @"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)";
            // Get the web response
            using (var response = request.GetResponse())
            {
                // Get the stream containing content returned by the server
                using (var stream = response.GetResponseStream())
                {
                    // Create a FileStream to write the downloaded file
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        long totalBytes = response.ContentLength; // Total size of the file

                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // Calculate the progress percentage
                            int progress = (int)(((double)totalBytesRead / totalBytes) * 100);

                            // Display progress in console
                            Console.Write($"\rDownloading... {progress}%");
                        }
                    }
                }
            }

            Console.WriteLine(); // Move to next line after download completes
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
        }
    }
    static void ExtractZip(string zipFilePath, string extractPath)
    {
        try
        {
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting zip file: {ex.Message}");
        }
    }
    /// <summary>
    /// Finds the Garry's Mod install via the Windows Registry
    /// </summary>
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
    public void GetPlayerCountAllServers() // Ported from OG SUP Launcher
    {
        try
        {
            string Url = "https://superiorservers.co/api/servers";
            CookieContainer cookieJar = new CookieContainer();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.CookieContainer = cookieJar;
            request.Accept = @"text/html, application/xhtml+xml, */*";
            request.Referer = @"https://superiorservers.co/api";
            request.Headers.Add("Accept-Language", "en-GB");
            request.UserAgent = @"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string htmlString;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                htmlString = reader.ReadToEnd();
            }
            var jsonRoot = JsonDocument.Parse(htmlString).RootElement.GetProperty("response").GetProperty("Servers");

            danktownPlayerCount = jsonRoot[2].GetProperty("Players").GetInt32();
            danktownMaxPly = jsonRoot[2].GetProperty("MaxPlayers").GetInt32();
            c18PlayerCount = jsonRoot[3].GetProperty("Players").GetInt32();
            c18MaxPly = jsonRoot[3].GetProperty("MaxPlayers").GetInt32();
            cwrpPlayerCount = jsonRoot[4].GetProperty("Players").GetInt32();
            cwrpMaxPly = jsonRoot[4].GetProperty("MaxPlayers").GetInt32();
            cwrp2PlayerCount = jsonRoot[5].GetProperty("Players").GetInt32();
            cwrp2MaxPly = jsonRoot[5].GetProperty("MaxPlayers").GetInt32();
            cwrp3PlayerCount = jsonRoot[6].GetProperty("Players").GetInt32();
            cwrp3MaxPly = jsonRoot[6].GetProperty("MaxPlayers").GetInt32();

            lblDanktownPlyCount.Content = $"{danktownPlayerCount.ToString()}/{danktownMaxPly}";
            lblC18PlyCount.Content = $"{c18PlayerCount.ToString()}/{c18MaxPly}";
            lblCWRPPlyCount.Content = $"{cwrpPlayerCount.ToString()}/{cwrpMaxPly}";
            lblCWRP2PlyCount.Content = $"{cwrp2PlayerCount.ToString()}/{cwrp2MaxPly}";
            lblCWRP3PlyCount.Content = $"{cwrp3PlayerCount.ToString()}/{cwrp3MaxPly}";
        }
        catch (Exception){ }
    }
    public void InitUser()
    {
        // Get Avatar from Steam
        try
        {
            var client = new WebClient();
            string Url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=7875E26FC3C740C9901DDA4C6E74EB4E&steamids={steam.GetSteamId()}";
            CookieContainer cookieJar = new CookieContainer();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.CookieContainer = cookieJar;
            request.Accept = @"text/html, application/xhtml+xml, */*";
            request.Referer = @"https://superiorservers.co/api";
            request.Headers.Add("Accept-Language", "en-GB");
            request.UserAgent = @"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                JsonDocument json = JsonDocument.Parse(sr.ReadToEnd());
                Username = json.RootElement.GetProperty("response").GetProperty("players")[0].GetProperty("personaname").GetString();
                byte[] avatarData = client.DownloadData(json.RootElement.GetProperty("response").GetProperty("players")[0].GetProperty("avatarfull").GetString());
                ImageBrush imageBrush = new ImageBrush();
                imageBrush.Source = new Bitmap(new MemoryStream(avatarData));
                picAvatar.Fill = imageBrush;
            }
        }
        catch (Exception){ Console.WriteLine("Error getting avatar from steam"); }

        // Get Rank from SUP
        try
        {
            HttpWebRequest request = WebRequest.CreateHttp("https://superiorservers.co/api/profile/" + steam.GetSteamId());
            request.UserAgent = "Browser";
            WebResponse response = null;
            response = request.GetResponse(); // Get Response from webrequest
            StreamReader sr = new StreamReader(response.GetResponseStream()); // Create stream to access web data
            JsonDocument returnData = JsonDocument.Parse(sr.ReadToEnd());
            JsonElement darkrproot = returnData.RootElement.GetProperty("DarkRP");
            JsonElement badminRoot = returnData.RootElement.GetProperty("Badmin");

            FirstJoin.Text = DateTime.UnixEpoch.AddSeconds(double.Parse(badminRoot.GetProperty("FirstJoin").GetString())).ToShortDateString();
            LastSeen.Text = $"{FormatDuration(badminRoot.GetProperty("LastSeen").GetDouble())} ago";
            TimeSpan t = TimeSpan.FromSeconds(double.Parse(badminRoot.GetProperty("PlayTime").GetString()));
            string t2 = $"{t.Days * 24 + t.Hours}:{t.Minutes}:{t.Seconds}";
            Playtime.Text = t2;
            drpMoney.Text = double.Parse(darkrproot.GetProperty("Money").GetString()).ToString("C0");
            drpKarma.Text = darkrproot.GetProperty("Karma").GetString();
            drpOrg.Text = darkrproot.GetProperty("OrgName").GetString();
            if (drpOrg.Text == "") { drpBorderOrg1.IsVisible = false; drpBorderOrg2.IsVisible = false; drpOrg.IsVisible = false; drpOrgText.IsVisible = false; }
            JsonElement ranksFromResult = badminRoot.GetProperty("Ranks");
            switch (ranksFromResult.GetProperty("DarkRP").GetString())
            {
                case "VIP":
                    {
                        Rank.Text = "VIP";
                        picRank.Source = VIP;
                        chkStaff.IsVisible = false;
                        break;
                    }
                case "Moderator":
                    {
                        Rank.Text = "Moderator";
                        picRank.Source = MOD;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Admin":
                    {
                        Rank.Text = "Admin";
                        picRank.Source = ADMIN;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Double Admin":
                    {
                        Rank.Text = "Double Admin";
                        picRank.Source = DOUBLE;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Super Admin":
                    {
                        Rank.Text = "Super Admin";
                        picRank.Source = SUPER;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Council":
                    {
                        Rank.Text = "Council";
                        picRank.Source = COUNCIL;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Root":
                    {
                        Rank.Text = "Root";
                        picRank.Source = ROOT;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Content Creator":
                    {
                        Rank.Text = "Content Creator";
                        picRank.Source = CC;
                        chkStaff.IsVisible = true;
                        break;
                    }
                default:
                    {
                        Rank.Text = "User";
                        picRank.Source = MEMBER;
                        chkStaff.IsVisible = false;
                        break;
                    }
            }
            if (Rank.Text != "User" && Rank.Text != "VIP") return;
            switch (ranksFromResult.GetProperty("CWRP").GetString())
            {
                case "VIP":
                    {
                        Rank.Text = "VIP";
                        picRank.Source = VIP;
                        chkStaff.IsVisible = false;
                        break;
                    }
                case "Moderator":
                    {
                        Rank.Text = "Moderator";
                        picRank.Source = MOD;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Admin":
                    {
                        Rank.Text = "Admin";
                        picRank.Source = ADMIN;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Double Admin":
                    {
                        Rank.Text = "Double Admin";
                        picRank.Source = DOUBLE;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Super Admin":
                    {
                        Rank.Text = "Super Admin";
                        picRank.Source = SUPER;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Council":
                    {
                        Rank.Text = "Council";
                        picRank.Source = COUNCIL;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Root":
                    {
                        Rank.Text = "Root";
                        picRank.Source = ROOT;
                        chkStaff.IsVisible = true;
                        break;
                    }
                case "Content Creator":
                    {
                        Rank.Text = "Content Creator";
                        picRank.Source = CC;
                        chkStaff.IsVisible = true;
                        break;
                    }
                default:
                    {
                        Rank.Text = "User";
                        picRank.Source = MEMBER;
                        chkStaff.IsVisible = false;
                        break;
                    }
            }
        }
        catch (Exception)
        {

            throw;
        }

    }
    #endregion
}