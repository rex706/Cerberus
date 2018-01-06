using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using Discord.WebSocket;
using Discord;
using HtmlAgilityPack;
using System.Windows;
using Microsoft.Win32;

namespace Cerberus_GUI2
{

    public class GameServer
    {
        public string Name { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public string Status { get; set; }

        public string Time { get; set; }
    }

    public class UserStatusComparer : IComparer<UserStatus>
    {
        public int Compare(UserStatus x, UserStatus y)
        {
            if (x == y)
                return 0;
            if (x == UserStatus.Online && y != UserStatus.Online)
                return -1;
            if (x == UserStatus.Idle && y != UserStatus.Online)
                return -1;
            if (x == UserStatus.DoNotDisturb && (y != UserStatus.Online && y != UserStatus.Idle))
                return -1;
            if (x == UserStatus.AFK && (y == UserStatus.Offline || y == UserStatus.Invisible))
                return -1;
            if (x == UserStatus.Invisible && y == UserStatus.Offline)
                return -1;

            return 1;
        }
    }

    public class Utils
    {
        public static int CheckRegistry()
        {
            // Parse the application exe name to create the correct key names. 
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            int exePos = exePath.LastIndexOf(".");
            int slashPos = exePath.LastIndexOf(@"\") + 1;

            string appName = exePath.Substring(slashPos, exePos - slashPos);

            var key1 = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName + ".exe", null);
            var key2 = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName + ".vshost.exe", null);

            if (key1 == null || key2 == null)
            {
                AddIE11Registry(appName);
            }

            return 0;
        }

        public static int AddIE11Registry(string appName)
        {
            // Create keys in designated path.
            RegistryKey defaultKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
            RegistryKey vshostKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");

            // Set value to IE11 and set type to dword. 
            defaultKey.SetValue(appName + ".exe", 0x00002af9, RegistryValueKind.DWord);
            vshostKey.SetValue(appName + ".vshost.exe", 0x00002af9, RegistryValueKind.DWord);

            return 0;
        }

        public static List<GameServer> ParseServers(string s)
        {
            TcpClient tcpClient = new TcpClient();
            List<GameServer> gameServers = new List<GameServer>();

            string[] servers = s.Split( new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string server in servers)
            {
                var gameServer = new GameServer();

                try
                {
                    string[] info = server.Split(',');
                    gameServer.Name = info[0];

                    string[] info2 = info[1].Split(':');
                    gameServer.IP = info2[0];
                    gameServer.Port = Int32.Parse(info2[1]);

                    if (tcpClient.ConnectAsync(gameServer.IP, gameServer.Port).Wait(3500))
                    {
                        // Server online.
                        gameServer.Status = "Online";
                        gameServer.Time = DateTime.UtcNow.ToString();
                    }
                    else
                    {
                        // Server offline.
                        gameServer.Status = "Offline";
                        gameServer.Time = "na";
                    }
                }
                catch (Exception m)
                {
                    Console.WriteLine(m.Message);
                }

                gameServers.Add(gameServer);
            }

            return gameServers;
        }

        // Probably Obsolete
        public static string ServerStatus(string ServerIP, SocketTextChannel channel1)
        {
            Console.WriteLine("Checking severs...\n");

            TcpClient MinecraftServer = new TcpClient();
            TcpClient StarboundServer = new TcpClient();

            string ping = DateTime.Now.ToString();

            bool mOnline = false;
            bool sOnline = false;

            // Minecraft
            if (MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
            {
                mOnline = true;
                var pings = new IniFile("pings.ini");
                pings.Write("Minecraft", ping, "Pings");

                int BackupCounter = 0;

                string StartPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\1.9Tekxit2 Server";
                string CopyPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\1.9Tekxit2_Server_Backup";
                string ZipPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\TekxitBackup.zip";

                channel1.SendMessageAsync("Starting server backup...");

                // Zip server files for a backup. keep a few backups and time stamp them
                if (Directory.Exists(CopyPath))
                {
                    Directory.Delete(CopyPath, true);
                }

                while (File.Exists(ZipPath))
                {
                    BackupCounter++;
                    ZipPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\TekxitBackup(" + BackupCounter + ").zip";
                }

                DirectoryInfo diSource = new DirectoryInfo(StartPath);
                DirectoryInfo diTarget = new DirectoryInfo(CopyPath);

                Console.WriteLine("Copying server files...");
                CopyAll(diSource, diTarget);

                Console.WriteLine("Zipping backup folder...");

                ZipFile.CreateFromDirectory(CopyPath, ZipPath);

                Console.WriteLine("Backup Complete!\n");

                Directory.Delete(CopyPath, true);

                //minecraftChannel.SendMessage("Backup complete!");
            }

            // Starbound
            if (StarboundServer.ConnectAsync(ServerIP, 21025).Wait(3500))
            {
                sOnline = true;
                var pings = new IniFile("pings.ini");
                pings.Write("Starbound", ping, "Pings");
            }

            // Print status to console
            if (mOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Minecraft Server ONLINE  -  " + ServerIP + ":25565  -  " + ping);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Minecraft Server OFFLINE");
            }
            if (sOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Starbound Server ONLINE  -  " + ServerIP + ":21025  -  " + ping);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Starbound Server OFFLINE");
            }

            Console.ResetColor();
            Console.WriteLine();

            return ping;
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static string GetSearchHtmlCode(string s, bool safeSearch)
        {
            string url;
            string data = "";

            if (safeSearch)
                url = "https://www.google.com/search?q=" + s + "&safe=active&tbm=isch";
            else
                url = "https://www.google.com/search?q=" + s + "&tbm=isch";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

            var response = (HttpWebResponse)request.GetResponse();

            using (Stream dataStream = response.GetResponseStream())
            {
                if (dataStream == null)
                    return "";
                using (var sr = new StreamReader(dataStream))
                {
                    data = sr.ReadToEnd();
                }
            }
            return data;
        }

        public static List<string> GetSearchResultUrls(string html)
        {
            var urls = new List<string>();

            int ndx = html.IndexOf("\"ou\"", StringComparison.Ordinal);

            while (ndx >= 0)
            {
                ndx = html.IndexOf("\"", ndx + 4, StringComparison.Ordinal);
                ndx++;
                int ndx2 = html.IndexOf("\"", ndx, StringComparison.Ordinal);
                string url = html.Substring(ndx, ndx2 - ndx);
                urls.Add(url);
                ndx = html.IndexOf("\"ou\"", ndx2, StringComparison.Ordinal);
            }
            return urls;
        }

        public static async System.Threading.Tasks.Task<int> CheckPermissionAsync(SocketGuild guild, SocketUser user)
        {
            var gUser = guild.GetUser(user.Id);

            if (gUser == null)
            {
                // Author not member of guild.
                return -1;
            }

            IEnumerable<SocketRole> userRoles = gUser.Roles;

            foreach (SocketRole role in userRoles)
            {
                // Check for roles with permission.
                if (role.Name == "Mod" || role.Name == "Admin" || role.Name == "Bot")
                {
                    return 1;
                }
            }

            // Message author they don't have permission.
            await gUser.SendMessageAsync("You don't have permission to use that command!");

            return 0;
        }
    }
}
