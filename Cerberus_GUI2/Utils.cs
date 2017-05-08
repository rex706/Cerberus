using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using Discord.WebSocket;

namespace Cerberus_GUI2
{
    class Utils
    {
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

        public static string GetHtmlCode(string s, bool safeSearch)
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

        public static List<string> GetUrls(string html)
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
                if (role.Name == "Mod" || role.Name == "Admin")
                {
                    return 1;
                }
            }

            // Message author they don't have permission.
            var DMChannel = await gUser.CreateDMChannelAsync();
            await DMChannel.SendMessageAsync("You don't have permission to use that command!");
            await DMChannel.CloseAsync();

            return 0;
        }
    }
}
