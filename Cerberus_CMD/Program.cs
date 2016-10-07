using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Timers;
using Discord;
using Discord.Audio;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Cerberus_CMD
{ 
    class Program
    {
        private static DiscordClient client;

        private static Channel lastchannel;

        private static string ServerIP = "50.89.243.222";
        private static string LastSuccPing = "never";

        private static System.Timers.Timer KickTimer;
        private static bool KickTimerRunning = false;
        private static HashSet<string> VotedUsers;

        private static System.Timers.Timer AutoPingTimer;

        private static int NumUsers;
        private static int Democracy;

        private static string[] KickMessage;
        private static User tokick;

        private static bool VoteKickInProgress = false;

        private static string errorMsg = "Something went wrong :confused: Please try again!";

        private static bool logChat = false;
        private static bool serverPing = false;

        private static bool logUsers = false;
        private static HashSet<string> userSet;

        static void Main(string[] args)
        {
            // Cerberus logo and version
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("   ______          __\n" +
                              "  / ____/__  _____/ /_  ___  _______  _______\n" + 
                              " / /   / _ \\/ ___/ __ \\/ _ \\/ ___/ / / / ___/\n" +
                              "/ /___/  __/ /  / /_/ /  __/ /  / /_/ (_  _)\n"+
                              "\\____/\\___/_/  /_/___/\\___/_/   \\____/____/\n");
            Console.WriteLine("v"+ Assembly.GetExecutingAssembly().GetName().Version.ToString() +" \n");

            // Change color for command line arguemnts 
            Console.ForegroundColor = ConsoleColor.DarkCyan;

            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    if (arg == "-chat")
                    {
                        logChat = true;
                        Console.WriteLine("[chat logging enabled]");
                    }
                    if (arg == "-users")
                    {
                        logUsers = true;
                        Console.WriteLine("[user logging enabled]");
                    }
                    if (arg == "-ping")
                    {
                        serverPing = true;
                        Console.WriteLine("[server pinging enabled]");
                    }
                }
            }

            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine("Creating Client");

            client = new DiscordClient();

            // Set up events
            Console.WriteLine("Defining Events");

            userSet = new HashSet<string>();

            if (!File.Exists("user_names.txt"))
            {
                File.CreateText("user_names.txt");
            }
            else
            {
                StreamReader file = new StreamReader("user_names.txt");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    userSet.Add(line);
                }
            }

            client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
            });

            client.MessageReceived += (sender, e) => // Channel message has been received
            {
                if (!e.User.IsBot || e.Message.Text.Contains(errorMsg))
                {
                    if (e.Message.Attachments.Length > 0)
                    {
                        Console.WriteLine(e.User.Name + ": [attachment] " + e.Message.Text + "\n");
                    }
                    else
                    {
                        Console.WriteLine(e.User.Name + ": " + e.Message.Text + "\n");
                    }

                    if (logChat)
                    {
                        if (!File.Exists("chat_log.txt"))
                        {
                            using (StreamWriter file = File.CreateText("chat_log.txt"))
                            {
                                if (e.Message.Attachments.Length > 0)
                                {
                                    file.WriteLine(e.User.Name + ": [attachment] " + e.Message.Text);
                                }
                                else
                                {
                                    file.WriteLine(e.User.Name + ": " + e.Message.Text);
                                }
                            }
                        }
                        else
                        {
                            using (StreamWriter file = File.AppendText("chat_log.txt"))
                            {
                                if (e.Message.Attachments.Length > 0)
                                {
                                    file.WriteLine(e.User.Name + ": [attachment] " + e.Message.Text);
                                }
                                else
                                {
                                    file.WriteLine(e.User.Name + ": " + e.Message.Text);
                                }
                            }
                        }
                    }
                }
                
                if (e.Message.Text == "!help")
                {
                    e.Channel.SendMessage("\n\n```css\n#UserCommands```\n" +
                    "!cat -------- random cat picture.\n" +
                    "!dog -------- random dog picture.\n" +
                    "!tits -------- show me the money!\n" +
                    "!gimme\\!find [search phrase] - get random image from search phrase.\n" + 
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

                        if(filetype.Contains(".gif"))
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
                if (e.Message.Text.Contains("!kick") && !e.User.IsBot && VoteKickInProgress == true)
                {
                    e.Channel.SendMessage("Another vote is in progress! Please try again after voting has finished.");
                }
                
                // Vote to kick
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

                        VotedUsers = new HashSet<string>();
                    }
                }

                if (e.Message.Text == "!yes" && KickTimerRunning == true && !VotedUsers.Contains(e.User.Name))
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
                        // Kick user
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
                    }
                }

                // Random image from search phrase
                if ((e.Message.Text.Contains("!gimme") || e.Message.Text.Contains("!find") || e.Message.Text.Contains("!search")) && !e.User.IsBot)
                {
                    string[] phrase = e.Message.Text.Split(' ');
                    string query = null;
                    bool plural = false;

                    if (phrase.Length > 2)
                    {
                        for (int i = 1; i < phrase.Length; i++)
                            
                            if (i == phrase.Length - 1)
                                query += phrase[i];
                            else
                                query += phrase[i] + " ";

                        plural = true;
                    }
                    else
                    {
                        query = phrase[1];
                    }

                    if (query[query.Length - 1].Equals('s'))
                        plural = true;

                    string html = GetHtmlCode(query);
                    List<string> urls = GetUrls(html);
                    var rnd = new Random();

                    int random = rnd.Next(0, urls.Count - 1);
                    string luckyUrl = urls[random];
                    int dotIdx = luckyUrl.LastIndexOf(".");
                    string fileType = luckyUrl.Substring(dotIdx);

                    if (e.Message.Text.Contains(" gif"))
                    {
                        // Try new url until a gif is found
                        while (fileType != ".gif")
                        {
                            random = rnd.Next(0, urls.Count - 1);
                            luckyUrl = urls[random];
                            dotIdx = luckyUrl.LastIndexOf(".");
                            fileType = luckyUrl.Substring(dotIdx);
                        }
                    }
                    else
                    {
                        // Try new url until a supported filetype is found
                        while (fileType != ".gif" && fileType != ".png" && fileType != ".jpg" && fileType != ".jpeg")
                        {
                            random = rnd.Next(0, urls.Count - 1);
                            luckyUrl = urls[random];
                            dotIdx = luckyUrl.LastIndexOf(".");
                            fileType = luckyUrl.Substring(dotIdx);
                        }
                    }

                    WebClient webclient = new WebClient();

                    // Try to download random image 5 times before giving up
                    for (int attempt = 0; attempt < 5; attempt++)
                    {
                        try
                        {
                            webclient.DownloadFile(luckyUrl, "random" + fileType);

                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(e.User.Name);
                            Console.ResetColor();
                            Console.Write(" queried '" + query + "' in ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write("#" + e.Channel.Name + "\n");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine(luckyUrl + "\n");
                            Console.ResetColor();

                            if (plural)
                                e.Channel.SendMessage("I found " + query + "!");
                            else
                                e.Channel.SendMessage("I found a " + query + "!");

                            e.Channel.SendFile("random" + fileType);

                            break;
                        }
                        catch
                        {
                            if (e.Message.Text.Contains(" gif"))
                            {
                                // Try new url until a gif is found
                                while (fileType != ".gif")
                                {
                                    random = rnd.Next(0, urls.Count - 1);
                                    luckyUrl = urls[random];
                                    dotIdx = luckyUrl.LastIndexOf(".");
                                    fileType = luckyUrl.Substring(dotIdx);
                                }
                            }
                            else
                            {
                                // Try new url until a supported filetype is found
                                while (fileType != ".gif" && fileType != ".png" && fileType != ".jpg" && fileType != ".jpeg")
                                {
                                    random = rnd.Next(0, urls.Count - 1);
                                    luckyUrl = urls[random];
                                    dotIdx = luckyUrl.LastIndexOf(".");
                                    fileType = luckyUrl.Substring(dotIdx);
                                }
                            }

                            if (attempt == 4)
                                e.Channel.SendMessage(errorMsg);
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

            // When a new user joins the server, send a message to them.
            client.UserJoined += (sender, e) =>
            {
                e.User.SendMessage("Welcome, " + e.User.Name + "!\nType '!help' for a list of available commands.");
            };

           // Welcome a user when they come back online or join a voice channel after not have being connected previously.
           client.UserUpdated += (sender, e) =>
           {
               // User was offline and came back online
               // (e.Before.Status == UserStatus.Offline && e.After.Status == UserStatus.Online)

               // User joined a voice channel after not being previously connected to any.
               if (e.Before.VoiceChannel == null && e.After.VoiceChannel != null)
               {
                   Console.WriteLine(e.After.Name.ToString() + " joined.\n");

                   if (logChat)
                   {
                       using (StreamWriter file = File.AppendText("chat_log.txt"))
                       {
                           file.WriteLine(e.After.Name.ToString() + " joined.");
                       }
                   }
                   
                   //e.Server.DefaultChannel.SendMessage("Welcome back, " + e.After.Name + "!");

                   if (logUsers)
                   {
                       if (userSet.Add(e.After.Name.ToString()))
                       {
                           Console.WriteLine("Added " + e.After.Name.ToString() + " to HashSet\n");
                           using (StreamWriter file = File.AppendText("user_names.txt"))
                           {
                               file.WriteLine(e.After.Name.ToString());
                           }
                       }
                   }
               }
               if (e.Before.VoiceChannel != null && e.After.VoiceChannel == null)
               {
                   Console.WriteLine(e.After.Name.ToString() + " left.\n");

                   if (logChat)
                   {
                       using (StreamWriter file = File.AppendText("chat_log.txt"))
                       {
                           file.WriteLine(e.After.Name.ToString() + " left.\n");
                       }
                   }
               }

               if (e.After.VoiceChannel.Name == "AFK")
               {
                   Console.WriteLine(e.After.Name + " went afk.\n");

                   if (logChat)
                   {
                       using (StreamWriter file = File.AppendText("chat_log.txt"))
                       {
                           file.WriteLine(e.After.Name + " went afk.");
                       }
                   }
               }

               if (e.Before.VoiceChannel.Name == "AFK" && e.After.VoiceChannel.Name != "AFK" && e.After.VoiceChannel != null)
               {
                   Console.WriteLine(e.After.Name + " is no longer afk.\n");

                   if (logChat)
                   {
                       using (StreamWriter file = File.AppendText("chat_log.txt"))
                       {
                           file.WriteLine(e.After.Name + " is no longer afk.\n");
                       }
                   }
               }
           };

            // Prevents messages from being deleted
            //client.MessageDeleted += (sender, e) =>
            //{
            //    e.Channel.SendMessage("Removing messages has been disabled on this server!");
            //    e.Channel.SendMessage("<@" + e.Message.User.Id + "> sent: " +e.Message.Text);
            //};

            // Connect bot and start timers
            client.ExecuteAndWait(async () =>
            {
                Console.WriteLine("Connecting...");
                try
                {
                    // bot token
                    await client.Connect("BOT_TOKEN", TokenType.Bot);
                    client.SetGame(null);

                    // Done!
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nClient connected!");
                    Console.ResetColor();
                    Console.WriteLine(" \n-----------------\n");

                    if (serverPing)
                    {
                        // Start auto server ping/backup timer
                        AutoPingTimer = new System.Timers.Timer(1800000); //600000ms = 10 min, 1200000 = 20 min, 1800000 = 30 min, 3600000 = 1 hr
                        AutoPingTimer.Elapsed += new ElapsedEventHandler(AutoPingTimer_Elapsed);
                        AutoPingTimer.Start();

                        ServerStatus();
                    }
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("\nConnection failed! ");
                    Console.WriteLine("Is there a client already open?");
                    Console.ResetColor();
                    Console.ReadLine();
                }
            });
        }
        
        // Vote to kick timer ended
        private static void KickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            KickTimer.Stop();

            // Vote failed
            if (Democracy > 0)
            {
                Console.WriteLine("Vote to kick " + tokick.Name + " failed.");
                lastchannel.SendMessage("Kick failed. Not enough users voted.");
            }

            KickTimerRunning = false;
            VoteKickInProgress = false;
            VotedUsers = null;
        }

        // Ping servers after timer has elapsed
        private static void AutoPingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ServerStatus();
        }

        private static void ServerStatus()
        {
            Console.WriteLine("Checking severs...\n");

            TcpClient MinecraftServer = new TcpClient();
            TcpClient StarboundServer = new TcpClient();

            LastSuccPing = DateTime.Now.ToString();

            bool mOnline = false;
            bool sOnline = false;

            // Minecraft
            if (MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
            {
                mOnline = true;
                var pings = new IniFile("pings.ini");
                pings.Write("Minecraft", LastSuccPing, "Pings");

                int BackupCounter = 0;

                string StartPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\1.9Tekxit2 Server";
                string CopyPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\1.9Tekxit2_Server_Backup";
                string ZipPath = @"D:\Documents\Google Drive\Servers\1.9Tekxit2 Server\TekxitBackup.zip";

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
            }

            // Starbound
            if (StarboundServer.ConnectAsync(ServerIP, 21025).Wait(3500))
            {
                sOnline = true;
                var pings = new IniFile("pings.ini");
                pings.Write("Starbound", LastSuccPing, "Pings");
            }

            // Print status to console
            if (mOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Minecraft Server ONLINE  -  " + ServerIP + ":25565  -  " + LastSuccPing);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Minecraft Server OFFLINE");
            }
            if (sOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Starbound Server ONLINE  -  " + ServerIP + ":21025  -  " + LastSuccPing);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Starbound Server OFFLINE");
            }

            Console.ResetColor();
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

        private static string GetHtmlCode(string s)
        {
            string url = "https://www.google.com/search?q=" + s + "&tbm=isch";
            string data = "";

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
        private static List<string> GetUrls(string html)
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
    }
}