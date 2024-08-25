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
using System.Windows;
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

namespace SUPLauncher.Views;
/*
    Questions for peng/king:

    Golden poop script?
    


    TODO FROM BLACK MAN:
    - Move SUPeriorServers to left
    - Move Server list down
    - "Your info" box like sup loading screen
 */
public partial class MainWindow : Window
{
    #region Globals
    private int danktownPlayerCount = 0;
    private int c18PlayerCount = 0;
    private int cwrpPlayerCount = 0; 
    private int cwrp2PlayerCount = 0;
    public static string rp1 = Dns.GetHostEntry("rp.superiorservers.co").AddressList[0].ToString();
    public static string rp2 = Dns.GetHostEntry("rp2.superiorservers.co").AddressList[0].ToString();
    public static string cwrp1 = Dns.GetHostEntry("cwrp.superiorservers.co").AddressList[0].ToString();
    public static string cwrp2 = Dns.GetHostEntry("cwrp2.superiorservers.co").AddressList[0].ToString();
    private static SteamBridge steam = new SteamBridge();
    public static string Username = "";
    private DispatcherTimer tmrAFK = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(15000)};
    #endregion
    public MainWindow()
    {
        InitializeComponent();
        GetPlayerCountAllServers();
        GetAvatar();
        InitValvecmd();
        PlayerTracking.Update();
        tmrAFK.Tick += new EventHandler(tmrAFK_Tick);
        tmrAFK.Start();
    }
    private void btnClose(object sender, RoutedEventArgs args)
    {
        this.Close();
    }
    private void btnMinimize(object sender, RoutedEventArgs args)
    {
        this.WindowState = WindowState.Minimized;
    }
    private void imgPointerPressed(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "https://github.com/Nicks-Alt/SUPLauncher/releases/latest"
        }); // TODO: CHANGE TO NEW REPO ONCE MADE
    }
    private void btnDanktown_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit  -w 300 -h 300 -single_core -nojoy -low -nosound -sw -nopix -novid -nopreload -nopreloadmodels -multirun +cl_mouselook 0 +connect {rp1}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -w 1920 -h 1080 -windowed -noborder +cl_mouselook 1 +connect {rp1}" 
            });
        
    }
    private void btnC18_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +cl_mouselook 0 +connect {rp2}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -w 1920 -h 1080 -windowed -noborder +cl_mouselook 1 +connect {rp2}"
            });
    }
    private void btnCWRP_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +connect {cwrp1}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -w 1920 -h 1080 -windowed -noborder +connect {cwrp1}"
            });
    }
    private void btnCWRP2_Click(object sender, RoutedEventArgs args)
    {
        if (chkAFK.IsChecked == true)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -h 300 -w 100 -single_core -nojoy -low -nosound -sw -noshaderapi -nopix -novid -nopreload -nopreloadmodels -multirun +connect {cwrp2}"
            });
        else
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = $"steam://run/4000//64bit -w 1920 -h 1080 -windowed -noborder +connect {cwrp2}"
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
            Process.GetProcessesByName("hl2")[0].Kill(true);
            Process.GetProcessesByName("gmod")[0].Kill(true);
        }
        catch (Exception){} // ignore exceptions
    }
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
    #region Helpers
    static Process[] GetGarrysModProcess()
    {
        //try
        //{
        var temp = Process.GetProcessesByName("gmod");
        if (temp.Length == 0)
            temp = Process.GetProcessesByName("hl2");
        if (temp.Length == 0)
            temp = null;
        return temp;
        //}
        //catch (Exception)
        //{

        //    throw;
        //}
    }
    private void InitValvecmd()
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
    private void GetPlayerCountAllServers() // Ported from OG SUP Launcher
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
            c18PlayerCount = jsonRoot[3].GetProperty("Players").GetInt32();
            cwrpPlayerCount = jsonRoot[4].GetProperty("Players").GetInt32();
            cwrp2PlayerCount = jsonRoot[5].GetProperty("Players").GetInt32();

            lblDanktownPlyCount.Content = $"{danktownPlayerCount.ToString()}/128";
            lblC18PlyCount.Content = $"{c18PlayerCount.ToString()}/128";
            lblCWRPPlyCount.Content = $"{cwrpPlayerCount.ToString()}/128";
            lblCWRP2PlyCount.Content = $"{cwrp2PlayerCount.ToString()}/128";
        }
        catch (Exception){ }
    }
    void GetAvatar()
    {
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
            //if (Settings.BackgroundImagePath != "")
            //    panel1.BackgroundImage = Image.FromFile(Settings.BackgroundImagePath);


        }
        catch (Exception){}

    }
    #endregion
}