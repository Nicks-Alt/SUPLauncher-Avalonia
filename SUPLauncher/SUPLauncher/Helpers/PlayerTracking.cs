using MySqlConnector;
using System;

namespace SUPLauncher.Helpers;

class PlayerTracking
    {
    private static SteamBridge steam = new SteamBridge();
        public static void Update()
        {
        try
        {
            using (MySqlConnection con = new MySqlConnection($"Server=3.20.94.69;Port=3306;Database=suplauncher;Uid=suplauncher;Pwd=penguingivecc;Connection Timeout=5"))
            {
                con.Open();
                MySqlCommand command = new MySqlCommand($"REPLACE INTO suplauncher (steamID, steamName, lastUsed) VALUES ('{steam.GetSteamId()}', '{SUPLauncher.Views.MainWindow.Username.ToString()}', '{DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}');", con);
                command.ExecuteNonQuery();
                con.Close();
            }
        }
        catch (Exception){}
            
        }
    }
