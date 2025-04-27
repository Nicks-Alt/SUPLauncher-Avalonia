using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class A2S
{
    public static (int players, int maxPlayers) GetPlayerCounts(string ip, int port, int timeout = 1000)
    {
        using (var client = new UdpClient())
        {
            client.Client.SendTimeout = timeout;
            client.Client.ReceiveTimeout = timeout;
            client.Connect(ip, port);

            // Send A2S_INFO request
            byte[] request = new byte[] {
                0xFF, 0xFF, 0xFF, 0xFF, // Header
                0x54, // A2S_INFO code
                0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 // "Source Engine Query\0"
            };
            client.Send(request, request.Length);

            // Get response
            IPEndPoint remoteEP = null;
            byte[] response = client.Receive(ref remoteEP);

            // Parse response
            using (var ms = new MemoryStream(response))
            using (var br = new BinaryReader(ms))
            {
                // Skip header (0x49) and protocol byte
                br.ReadBytes(2);

                // Skip server name, map, folder, game strings
                SkipNullTerminatedString(br); // Name
                SkipNullTerminatedString(br); // Map
                SkipNullTerminatedString(br); // Folder
                SkipNullTerminatedString(br); // Game

                // Skip AppID (2 bytes)
                br.ReadInt16();

                // Read player counts
                byte players = br.ReadByte();
                byte maxPlayers = br.ReadByte();

                return (players, maxPlayers);
            }
        }
    }

    private static void SkipNullTerminatedString(BinaryReader br)
    {
        while (br.ReadByte() != 0) { }
    }
}