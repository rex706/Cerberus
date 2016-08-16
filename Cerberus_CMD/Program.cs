using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Timers;
using Discord; //discord.net library
using Discord.Audio;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;

namespace Cerberus_CMD
{ 
    class Program
    {
        private static DiscordClient client;

        private static Channel lastchannel;

        private static string ServerIP = "50.89.243.222";
        private static string LastSuccPing = "never";

        private static System.Timers.Timer KickTimer;
        private static System.Timers.Timer JailTimer;
        private static bool KickTimerRunning = false;
        private static bool JailTimerRunning = false;
        private static List<string> VotedUsers;

        private static System.Timers.Timer AutoPingTimer;
        private static System.Timers.Timer RoleAdjustTimer;

        private static int NumUsers;
        private static int Democracy;

        private static string[] KickMessage;
        private static User tokick;
        private static User tojail;

        private static Role[] _roles;
        private static int noobidx;

        private static bool VoteKickInProgress = false;
        private static bool VoteJailInProgress = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Creating Client");

            client = new DiscordClient();

            // Set up events
            Console.WriteLine("Defining Events");

            client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
            });

            client.MessageReceived += (sender, e) => // Channel message has been received
            {
                if (e.Message.Text == "!help")
                {
                    e.Channel.SendMessage("\n\n```css\n#UserCommands```\n" +
                    "!cat -------- random cat picture.\n" +
                    "!dog -------- random dog picture.\n" +
                    "!tits -------- show me the money!\n" +
                    "!region ----- current Discord region.\n" +
                    "!minecraft - minecraft server status.\n" +
                    "!starbound - starbound server status.\n" +
                    "!kick [username] [discriminator] - vote to kick another user.");

                    // Because this is a public message, the bot should send a message to the channel the message was received.
                }
                if (e.Message.Text == "!cat")
                {
                    Thread t = new Thread(new ParameterizedThreadStart(randomcat));
                    t.Start(e.Channel);
                    string s;
                    using (WebClient webclient = new WebClient())
                    {
                        s = webclient.DownloadString("http://random.cat/meow");
                        int pFrom = s.IndexOf("\\/i\\/") + "\\/i\\/".Length;
                        int pTo = s.LastIndexOf("\"}");
                        int pIdx = s.LastIndexOf(".");
                        string filetype = s.Substring(pIdx);
                        string filename = "cat.png";

                        if(filetype.Contains("gif"))
                        {
                            filename = "cat.gif";
                        }

                        string cat = s.Substring(pFrom, pTo - pFrom);
                        webclient.DownloadFile("http://random.cat/i/" + cat, filename);
                        e.Channel.SendMessage("meow!");
                        e.Channel.SendFile(filename);
                    }
                }
                if (e.Message.Text == "!dog")
                {
                    Thread t = new Thread(new ParameterizedThreadStart(randomcat));
                    t.Start(e.Channel);
                    string s;
                    using (WebClient webclient = new WebClient())
                    {
                        s = webclient.DownloadString("http://random.dog/woof");
                        string dog = s;
                        webclient.DownloadFile("http://random.dog/" + dog, "dog.png");
                        e.Channel.SendMessage("woof!");
                        e.Channel.SendFile("dog.png");
                    }
                }
                if (e.Message.Text == "!tits")
                {
                    Thread t = new Thread(new ParameterizedThreadStart(randomcat));
                    t.Start(e.Channel);

                    using (WebClient webclient = new WebClient())
                    {
                        webclient.DownloadFile("https://upload.wikimedia.org/wikipedia/commons/8/86/GreatTit002.jpg", "tits.png");
                        e.Channel.SendMessage("a nice natural pair of tits!");
                        e.Channel.SendFile("tits.png");
                    }
                }
                if (e.Message.Text == "!minecarft")
                {
                    e.Channel.SendMessage("Did you misspell 'minecraft'?");
                }

                // Ping minecraft server
                if (e.Message.Text == "!minecraft")
                {
                    if (File.Exists("pings.ini"))
                    {
                        var pings = new IniFile("pings.ini");
                        LastSuccPing = pings.Read("Minecraft", "Pings");
                    }

                    TcpClient MinecraftServer = new TcpClient();


                    if (!MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
                    {
                        e.Channel.SendMessage("Minecraft Server \n\n```css\n:OFFLINE``` \nLast successful ping: **" + LastSuccPing + "**\n\nCould just be ping issue? Try again in a few seconds.\nIf connection continues to fail, try again later.");
                    }
                    else
                    {
                        e.Channel.SendMessage("Minecraft Server \n\n```css\n.:ONLINE  -  " + ServerIP + ":25565```");

                        var pings = new IniFile("pings.ini");
                        LastSuccPing = DateTime.Now.ToString();
                        pings.Write("Minecraft", LastSuccPing, "Pings");
                    }
                }

                // Ping starbound server
                if (e.Message.Text == "!starbound")
                {
                    if (File.Exists("pings.ini"))
                    {
                        var pings = new IniFile("pings.ini");
                        LastSuccPing = pings.Read("Starbound", "Pings");

                        if (LastSuccPing == "")
                            LastSuccPing = "never";
                    }

                    TcpClient StarboundServer = new TcpClient();

                    if (!StarboundServer.ConnectAsync(ServerIP, 21025).Wait(3500))
                    {
                        e.Channel.SendMessage("Starbound Server \n\n```css\n:OFFLINE``` \nLast successful ping: **" + LastSuccPing + "**\n\nCould just be ping issue? Try again in a few seconds.\nIf connection continues to fail, try again later.");
                    }
                    else
                    {
                        e.Channel.SendMessage("Starbound Server \n\n```css\n.:ONLINE  -  " + ServerIP + ":21025```");

                        var pings = new IniFile("pings.ini");
                        LastSuccPing = DateTime.Now.ToString();
                        pings.Write("Starbound", LastSuccPing, "Pings");
                    }
                }

                // Make sure only one vote can be in progress at a time
                if ((e.Message.Text.Contains("!jail") || e.Message.Text.Contains("!kick")) && !e.User.IsBot && (VoteKickInProgress == true || VoteJailInProgress == true))
                {
                    e.Channel.SendMessage("Another vote is in progress! Please try again after voting has finished.");
                }

                //currently not working - jail user to specific voice channel by removing all roles and applying the 'noob' role.
                //need to fix removal of roles, adding noob role is easy
                if (e.Message.Text.Contains("!jail") && !e.User.IsBot && VoteJailInProgress == false)
                {
                    KickMessage = e.Message.Text.Split(' ');

                    try
                    {
                        tojail = e.Server.GetUser(KickMessage[1], ushort.Parse(KickMessage[2]));
                    }
                    catch
                    {
                    }
                    if (tojail == null)
                    {
                        e.Channel.SendMessage("Invalid user!");
                    }
                    else
                    {
                        Console.WriteLine(e.User.Name + " initiated vote to jail " + tojail.Name);

                        VoteJailInProgress = true;
                        lastchannel = e.Channel;
                        NumUsers = e.Server.UserCount; //e.Sever.Users.Count();
                        Democracy = 1; //(NumUsers / 6);

                        if (Democracy == 1)
                            e.Channel.SendMessage("Vote to jail " + tojail.Mention + " initiated for 2 minutes! **" + Democracy + "** vote required.\n\n```Type !yes to jail.```");
                        else
                            e.Channel.SendMessage("Vote to jail " + tojail.Mention + " initiated for 2 minutes! **" + Democracy + "** votes required.\n\n```Type !yes to jail.```");

                        JailTimer = new System.Timers.Timer(120000);
                        JailTimer.Elapsed += new ElapsedEventHandler(KickTimer_Elapsed);
                        JailTimer.Start();
                        JailTimerRunning = true;

                        VotedUsers = new List<string>();
                    }
                }

                //vote to kick
                if (e.Message.Text.Contains("!kick") && !e.User.IsBot && VoteKickInProgress == false)
                {

                    KickMessage = e.Message.Text.Split(' ');
                    try
                    {
                        tokick = e.Server.GetUser(KickMessage[1], ushort.Parse(KickMessage[2]));
                    }
                    catch
                    {
                    }
                    if (tokick == null)
                    {
                        e.Channel.SendMessage("Invalid user!");
                    }
                    else
                    {
                        Console.WriteLine(e.User.Name + " initiated vote to kick " + tokick.Name);

                        VoteKickInProgress = true;
                        lastchannel = e.Channel;
                        NumUsers = e.Server.UserCount; //e.Sever.Users.Count();
                        Democracy = 5; //(NumUsers / 6);

                        if (Democracy == 1)
                            e.Channel.SendMessage("Vote to kick " + tokick.Mention + " initiated for 2 minutes! **" + Democracy + "** vote required.\n\n```Type !yes to kick.```");
                        else
                            e.Channel.SendMessage("Vote to kick " + tokick.Mention + " initiated for 2 minutes! **" + Democracy + "** votes required.\n\n```Type !yes to kick.```");

                        KickTimer = new System.Timers.Timer(120000);
                        KickTimer.Elapsed += new ElapsedEventHandler(KickTimer_Elapsed);
                        KickTimer.Start();
                        KickTimerRunning = true;

                        VotedUsers = new List<string>();
                    }

                }
                if (e.Message.Text == "!yes" && (KickTimerRunning == true || JailTimerRunning == true) && !VotedUsers.Contains(e.User.Name))
                {
                    Democracy -= 1;

                    if (Democracy == 1)
                    {
                        e.Channel.SendMessage("Vote recieved from " + e.User.Name + "! **" + Democracy + "** vote remaining. ");
                        VotedUsers.Add(e.User.Name);
                    }
                    else
                    {
                        e.Channel.SendMessage("Vote recieved from " + e.User.Name + "! **" + Democracy + "** votes remaining.");
                        VotedUsers.Add(e.User.Name);
                    }
                    if (Democracy == 0)
                    {
                        //kick user
                        if (KickTimerRunning == true)
                        {
                            Console.WriteLine("Kicking " + tokick.Name + "...");
                            e.Channel.SendMessage("Vote passed! Kicking " + tokick.Name + "...  **Democracy!**");
                            tokick.Kick();

                            KickTimer.Stop();
                            KickTimerRunning = false;
                            VotedUsers = null;

                            VoteKickInProgress = false;
                        }
                        //jail user
                        else if(JailTimerRunning == true)
                        {
                            Console.WriteLine("Sending " + tojail.Name + " to jail!");
                            e.Channel.SendMessage("Vote passed! Adjusting roles...");

                            int i = 0;
                            noobidx = 0;

                            IEnumerable<Role> roles = e.Server.Roles;
                            _roles = new Role[e.Server.RoleCount];

                            foreach (Role role in roles)
                            {
                                _roles[i] = role;
                                i++;
                            }

                            for(i = 0; i < e.Server.RoleCount; i++)
                            {
                                if (_roles[i].Name.Contains("noob"))
                                {
                                    noobidx = i;
                                }
                                else if (_roles[i].Name.Contains("@everyone"))
                                {

                                }
                                else
                                {
                                    //sometimes wont work if there is only one of these
                                    //I put a bunch just to be sure
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                    tojail.RemoveRoles(_roles[i]);
                                }
                            }

                            //wait 5 seconds to allow role deletion to update with discord servers before adding the noob role to prevent resetting the whole process
                            RoleAdjustTimer = new System.Timers.Timer(5000);
                            RoleAdjustTimer.Elapsed += new ElapsedEventHandler(RoleAdjustTimer_Elapsed);
                            RoleAdjustTimer.Start();

                            JailTimer.Stop();
                            JailTimerRunning = false;
                            VotedUsers = null;

                            VoteJailInProgress = false;
                        }
                    }
                }

                //apply the member role to all guild users, but only if user has admin role
                if(e.Message.Text == "!member")
                {
                    IEnumerable<Role> roles = e.User.Roles;
                    foreach (Role role in roles)
                    {
                        if (role.Name.Contains("Admin"))
                        {
                            //loop through all users and apply 'Member' role;
                            IEnumerable<User> users = e.Server.Users;
                            foreach (User user in users)
                            {
                                //add member role, may have to just cycle through and save the noob role and member role as individual global variables
                                //this will make it way easier to remove or apply them whenever, where ever
                                user.AddRoles(); 

                            }
                        }
                    }
                }

                // Echo current discord region
                if (e.Message.Text == "!region")
                {
                    e.Channel.SendMessage("Current Discord region set to `" + e.Server.Region.Name + "`");
                }
            };

            // This sends a message to every new channel on the server
            client.ChannelCreated += (sender, e) =>
            {
                if (e.Channel.Type == ChannelType.Text)
                {
                    e.Channel.SendMessage("Nice! A new channel has been created!");
                }
            };

            //When a new user joins the server, send a message to them.
            client.UserJoined += (sender, e) =>
            {
                e.User.SendMessage("Welcome, " + e.User.Name + "!\nType '!help' for a list of available commands.");
            };

            // Welcome a user when they come back online or join a voice channel after not have being connected previously.
            //client.UserUpdated += (sender, e) =>
            //{
            //    //user was offline and came back online
            //    if ((e.Before.Status == UserStatus.Offline && e.After.Status == UserStatus.Online) || (e.Before.VoiceChannel == null && e.After.VoiceChannel != null))
            //    {
            //        e.Server.DefaultChannel.SendMessage("Welcome back, " + e.After.Name + "!");
            //    }
            //};

            // Prevents messages from being deleted
            //client.MessageDeleted += (sender, e) =>
            //{
            //    e.Channel.SendMessage("Removing messages has been disabled on this server!");
            //    e.Channel.SendMessage("<@" + e.Message.User.Id + "> sent: " +e.Message.Text);
            //};

            // Connect bot and start timers
            client.ExecuteAndWait(async () =>
            {
                // bot token
                await client.Connect("BOT_TOKEN");
                client.SetGame(null);

                // Done!
                Console.WriteLine("Client connected!\n\n-----------------\n");

                // Start auto server ping/backup timer
                AutoPingTimer = new System.Timers.Timer(1800000); //600000ms = 10 min, 1200000 = 20 min, 1800000 = 30 min, 3600000 = 1 hr
                AutoPingTimer.Elapsed += new ElapsedEventHandler(AutoPingTimer_Elapsed);
                AutoPingTimer.Start();

                ServerStatus();
            });
        }
        private static void JailTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            JailTimer.Stop();

            //vote failed
            if (Democracy > 0)
            {
                Console.WriteLine("Vote to jail " + tokick.Name + " failed.");
                lastchannel.SendMessage("Arrest failed. Not enough users cared.");
            }

            JailTimerRunning = false;
            VoteKickInProgress = false;
            VotedUsers = null;
        }

        // Vote to kick timer ended
        private static void KickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            KickTimer.Stop();

            //vote failed
            if (Democracy > 0)
            {
                Console.WriteLine("Vote to kick " + tokick.Name + " failed.");
                lastchannel.SendMessage("Kick failed. Not enough users voted.");
            }

            KickTimerRunning = false;
            VoteKickInProgress = false;
            VotedUsers = null;
        }

        // Role adjust timer ended
        private static void RoleAdjustTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var voiceChannel = client.FindServers("|🚷|");

            //apply the noob role and stop timer
            tojail.AddRoles(_roles[noobidx]);
            RoleAdjustTimer.Stop();
        }

        // Ping servers after timer has elapsed
        private static void AutoPingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ServerStatus();
        }

        private static void ServerStatus()
        {
            TcpClient MinecraftServer = new TcpClient();
            TcpClient StarboundServer = new TcpClient();
            LastSuccPing = DateTime.Now.ToString();

            //minecraft
            if (!MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Minecraft Server OFFLINE");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Minecraft Server ONLINE  -  " + ServerIP + ":25565  -  " + LastSuccPing + "\n");
                Console.ResetColor();

                var pings = new IniFile("pings.ini");
                pings.Write("Minecraft", LastSuccPing, "Pings");

                int BackupCounter = 0;

                string StartPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\1.9Tekxit2 Server";
                string CopyPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\1.9Tekxit2_Server_Backup";
                string ZipPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\TekxitBackup.zip";

                //zip server files for a backup. keep a few backups and time stamp them
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
            }

            //starbound
            if (!StarboundServer.ConnectAsync(ServerIP, 21025).Wait(3500))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Starbound Server OFFLINE");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Starbound Server ONLINE  -  " + ServerIP + ":21025  -  " + LastSuccPing);
                Console.ResetColor();

                var pings = new IniFile("pings.ini");
                pings.Write("Starbound", LastSuccPing, "Pings");
            }

            Console.WriteLine();
        }
        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
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
        private static void randomcat(object channel)
        {

        }
    }
}