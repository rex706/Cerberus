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
        private static string lastSuccessfulPing = "never";

        private static System.Timers.Timer kickTimer;
        private static bool kickTimerRunning = false;
        private static HashSet<string> votedUsers;

        private static System.Timers.Timer autoPingTimer;

        private static int numUsers;
        private static int democracy;

        private static string[] kickMessage;
        private static User tokick;

        private static bool voteKickInProgress = false;

        private static string errorMsg = "Something went wrong :confused: Please try again!";
        private static string prevMsg;

        private static bool logChat = false;
        private static bool serverPing = false;
        private static bool safeSearch = true;
        private static bool logUsers = false;

        private static HashSet<string> userSet;
        private static HashSet<string> blackList;

        private static IEnumerable<Role> serverRoles;

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
                    }
                    if (arg == "-users")
                    {
                        logUsers = true;
                    }
                    if (arg == "-ping")
                    {
                        serverPing = true;
                    }
                    if (arg == "-safe")
                    {
                        safeSearch = true;
                    }
                }
            }

            if (logChat)
            {
                Console.WriteLine("[chat logging enabled]");
            }
            if (logUsers)
            {
                Console.WriteLine("[user logging enabled]");
            }
            if (serverPing)
            {
                Console.WriteLine("[server pinging enabled]");
            }
            if (safeSearch)
            {
                Console.WriteLine("[safe search enabled]");
            }

            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine("Creating Client");

            client = new DiscordClient();

            userSet = new HashSet<string>();
            blackList = new HashSet<string>();

            // Load user names text file
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
                file.Close();
            }

            // Load black listed users text file
            if (!File.Exists("black_list.txt"))
            {
                File.CreateText("black_list.txt");
            }
            else
            {
                StreamReader file = new StreamReader("black_list.txt");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    blackList.Add(line);
                }
                file.Close();
            }

            // Set up events
            Console.WriteLine("Defining Events");

            client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
            });

            client.MessageDeleted += (sender, e) =>
            {
                Console.Write("Deleted message: ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(e.Message.Text + "\n");
                Console.ResetColor();
            };

            client.MessageReceived += async (sender, e) => // Channel message has been received
            {
                // Check if user is on the Cerberus black list
                if (blackList.Contains(e.User.ToString()))
                {
                    await e.User.SendMessage("You have been black listed from using Cerberus commands. Sorry for the inconvenience!");
                    return;
                }

                if (!e.User.IsBot && e.Message.Text == "!blacklist")
                {
                    if (blackList.Count == 0)
                    {
                        await e.Channel.SendMessage("There are no currently blacklisted users.");
                    }
                    else
                    {
                        string blUsers = "";

                        foreach (string blUser in blackList)
                        {
                            blUsers += blUser + "   ";
                        }

                        await e.Channel.SendMessage("Blacklisted users: " + blUsers);
                    }
                }

                if (!e.User.IsBot && e.Message.Text.Contains("!blacklist") && e.Message.Text.Length > 14)
                {
                    IEnumerable<Role> userRoles = e.User.Roles;

                    bool hasPermission = false;
                    User toBlackList = null;

                    foreach (Role role in userRoles)
                    {
                        if (role.Name == "Mod" || role.Name == "Admin")
                        {
                            hasPermission = true;
                            break;
                        }
                    }

                    if (hasPermission)
                    {
                        string[] blackListUser = e.Message.Text.Split(' ');
                        try
                        {
                            toBlackList = e.Server.GetUser(blackListUser[1], ushort.Parse(blackListUser[2]));
                        }
                        catch
                        {
                        }
                        if (toBlackList == null)
                        {
                            await e.Channel.SendMessage("Invalid user!");
                            return;
                        }
                        else
                        {
                            if (!File.Exists("black_list.txt"))
                            {
                                File.CreateText("black_list.txt");
                            }

                            using (StreamWriter file = File.AppendText("black_list.txt"))
                            {
                                file.WriteLine(toBlackList.ToString());
                            }

                            blackList.Add(toBlackList.ToString());
                            await e.Channel.SendMessage(toBlackList.Mention + " has been blacklisted from Cerberus by " + e.User.Name);
                            Console.WriteLine(toBlackList.ToString() + " has been blacklisted from Cerberus by " + e.User.Name + "\n");
                        }
                    }
                    else
                    {
                        await e.User.SendMessage("You do not have permission to use that command!");
                    }
                }

                // GIVE user access to Muffin's CSGO channel
                if (!e.User.IsBot && e.Message.Text.Contains("!shield"))
                {
                    string [] message = e.Message.Text.Split(' ');
                    User newMember = null;

                    try { newMember = e.Server.GetUser(message[1], ushort.Parse(message[2])); }
                    catch { await e.User.SendMessage("Command format: !shield [username] [discriminator]"); }

                    if (newMember == null)
                    {
                        await e.Channel.SendMessage("Invalid user!\nCommand format: !shield [username] [discriminator]");
                        return;
                    }

                    IEnumerable<Role> serverRoles = e.Server.Roles;
                    IEnumerable<Role> userRoles = e.User.Roles;
                    Role[] newRoles = new Role[1];
                    Role shieldRole = null;
                    Role modRole = null;

                    // Get server roles
                    foreach (Role role in serverRoles)
                    {
                        if (role.Name == "Shield")
                        {
                            newRoles[0] = role;
                            shieldRole = role;
                        }
                        if (role.Name == "Mod")
                            modRole = role;
                    }

                    // Apply 'Shield' role to target user.
                    if (e.User.Name == "UselessMuffin" && e.User.Discriminator == 1335 || e.User.HasRole(shieldRole) || e.User.HasRole(modRole))
                    {
                        await newMember.AddRoles(newRoles);
                        await e.Channel.SendMessage("'Shield' role given to " + newMember.Name + "!\n "+ newMember.Name + " now has access to 'CSGO Shield Esports'.");
                        return;
                    }

                    await e.User.SendMessage("You do not have permission to use that command.");
                }

                // TAKE AWAY user access to Muffin's CSGO channel
                if (!e.User.IsBot && e.Message.Text.Contains("!rshield"))
                {
                    string[] message = e.Message.Text.Split(' ');
                    User oldMember = null;

                    try { oldMember = e.Server.GetUser(message[1], ushort.Parse(message[2])); }
                    catch { await e.User.SendMessage("Command format: !shield [username] [discriminator]"); }

                    if (oldMember == null)
                    {
                        await e.Channel.SendMessage("Invalid user!\nCommand format: !shield [username] [discriminator]");
                        return;
                    }

                    IEnumerable<Role> serverRoles = e.Server.Roles;
                    IEnumerable<Role> userRoles = e.User.Roles;
                    Role[] newRoles = new Role[1];
                    Role modRole = null;

                    // Get server roles
                    foreach (Role role in serverRoles)
                    {
                        if (role.Name == "Shield")
                            newRoles[0] = role;
                
                        if (role.Name == "Mod")
                            modRole = role;
                    }

                    // Remove 'Shield' role from target user.
                    if (e.User.Name == "UselessMuffin" && e.User.Discriminator == 1335 || e.User.HasRole(modRole))
                    {
                        await oldMember.RemoveRoles(newRoles);
                        await e.Channel.SendMessage("'Shield' role removed from " + oldMember.Name + ".\nAccess to 'CSGO Shield Esports' has been revoked.");
                        return;
                    }

                    await e.User.SendMessage("You do not have permission to use that command.");
                }

                // Record to text file if not bot.
                if (!e.User.IsBot || e.Message.Text.Contains(errorMsg))
                {
                    if (e.Message.Attachments.Length > 0)
                        Console.WriteLine(e.User.Name + ": [attachment] " + e.Message.Text + "\n");
                    else
                        Console.WriteLine(e.User.Name + ": " + e.Message.Text + "\n");

                    if (logChat)
                    {
                        if (!File.Exists("chat_log.txt"))
                        {
                            using (StreamWriter file = File.CreateText("chat_log.txt"))
                            {
                                if (e.Message.Attachments.Length > 0)
                                    file.WriteLine(e.User.Name + ": [attachment] " + e.Message.Text);
                                else
                                    file.WriteLine(e.User.Name + ": " + e.Message.Text);

                                file.Close();
                            }
                        }
                        else
                        {
                            using (StreamWriter file = File.AppendText("chat_log.txt"))
                            {
                                if (e.Message.Attachments.Length > 0)
                                    file.WriteLine(e.User.Name + ": [attachment] " + e.Message.Text);
                                else
                                    file.WriteLine(e.User.Name + ": " + e.Message.Text);

                                file.Close();
                            }
                        }
                    }
                }

                // Display help menu.
                if (e.Message.Text == "!help")
                {
                    await e.Channel.SendMessage("\n\n```css\n#UserCommands```\n" +
                    "!cat - random picture of a cat.\n" +
                    "!dog - random picture of a dog.\n" +
                    "!tits - show me the money!\n" +
                    "!find [search phrase] - random image from search phrase.\n" +
                    "!minecraft - minecraft server status.\n" +
                    "!starbound - starbound server status.\n" +
                    "!kick [username] [discriminator] - vote to kick another user.\n" +
                    "!blacklist - list the blacklisted users, if any.\n" +
                    "!blacklist [username] [discriminator] - blacklist a user from Cerberus. (mod only)\n" +
                    "!shield [username] [discriminator] - give user access to Shield Esports. (shield only)\n" +
                    "!rshield [username] [discriminator] - revoke access to Shield Esports. (muffin only)");
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

                        if (filetype.Contains(".gif"))
                        {
                            filename = "cat.gif";
                        }

                        string cat = s.Substring(pFrom, pTo - pFrom);
                        webclient.DownloadFile("http://random.cat/i/" + cat, filename);
                        await e.Channel.SendMessage("meow!");
                        await e.Channel.SendFile(filename);
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
                        await e.Channel.SendMessage("woof!");
                        await e.Channel.SendFile("dog.png");
                    }
                }
                if (e.Message.Text == "!tits")
                {
                    Thread t = new Thread(new ParameterizedThreadStart(randomcat));
                    t.Start(e.Channel);

                    using (WebClient webclient = new WebClient())
                    {
                        webclient.DownloadFile("https://upload.wikimedia.org/wikipedia/commons/8/86/GreatTit002.jpg", "tits.png");
                        await e.Channel.SendMessage("a nice natural pair of tits!");
                        await e.Channel.SendFile("tits.png");
                    }
                }
                if (e.Message.Text == "!minecarft")
                {
                    await e.Channel.SendMessage("Did you misspell 'minecraft'?");
                }

                // Ping minecraft server
                if (e.Message.Text == "!minecraft")
                {
                    if (File.Exists("pings.ini"))
                    {
                        var pings = new IniFile("pings.ini");
                        lastSuccessfulPing = pings.Read("Minecraft", "Pings");
                    }

                    TcpClient MinecraftServer = new TcpClient();


                    if (!MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
                    {
                        await e.Channel.SendMessage("Minecraft Server \n\n```css\n:OFFLINE``` \nLast successful ping: **" + lastSuccessfulPing + "**\n\nCould just be ping issue? Try again in a few seconds.\nIf connection continues to fail, try again later.");
                    }
                    else
                    {
                        await e.Channel.SendMessage("Minecraft Server \n\n```css\n.:ONLINE  -  " + ServerIP + ":25565```");

                        var pings = new IniFile("pings.ini");
                        lastSuccessfulPing = DateTime.Now.ToString();
                        pings.Write("Minecraft", lastSuccessfulPing, "Pings");
                    }
                }

                // Ping starbound server
                if (e.Message.Text == "!starbound")
                {
                    if (File.Exists("pings.ini"))
                    {
                        var pings = new IniFile("pings.ini");
                        lastSuccessfulPing = pings.Read("Starbound", "Pings");

                        if (lastSuccessfulPing == "")
                            lastSuccessfulPing = "never";
                    }

                    TcpClient StarboundServer = new TcpClient();

                    if (!StarboundServer.ConnectAsync(ServerIP, 21025).Wait(3500))
                    {
                        await e.Channel.SendMessage("Starbound Server \n\n```css\n:OFFLINE``` \nLast successful ping: **" + lastSuccessfulPing + "**\n\nCould just be ping issue? Try again in a few seconds.\nIf connection continues to fail, try again later.");
                    }
                    else
                    {
                        await e.Channel.SendMessage("Starbound Server \n\n```css\n.:ONLINE  -  " + ServerIP + ":21025```");

                        var pings = new IniFile("pings.ini");
                        lastSuccessfulPing = DateTime.Now.ToString();
                        pings.Write("Starbound", lastSuccessfulPing, "Pings");
                    }
                }

                // Make sure only one vote can be in progress at a time
                if (e.Message.Text.Contains("!kick") && !e.User.IsBot && voteKickInProgress == true)
                {
                    await e.Channel.SendMessage("Another vote is in progress! Please try again after voting has finished.");
                }

                // Vote to kick
                if (e.Message.Text.Contains("!kick") && !e.User.IsBot && voteKickInProgress == false)
                {

                    kickMessage = e.Message.Text.Split(' ');
                    try
                    {
                        tokick = e.Server.GetUser(kickMessage[1], ushort.Parse(kickMessage[2]));
                    }
                    catch
                    {
                    }
                    if (tokick == null)
                    {
                        await e.Channel.SendMessage("Invalid user!");
                    }
                    else
                    {
                        Console.WriteLine(e.User.Name + " initiated vote to kick " + tokick.Name);

                        voteKickInProgress = true;
                        lastchannel = e.Channel;
                        numUsers = e.Server.UserCount; //e.Sever.Users.Count();
                        democracy = 5; //(numUsers / 6);

                        if (democracy == 1)
                            await e.Channel.SendMessage("Vote to kick " + tokick.Mention + " initiated for 2 minutes! **" + democracy + "** vote required.\n\n```Type !yes to kick.```");
                        else
                            await e.Channel.SendMessage("Vote to kick " + tokick.Mention + " initiated for 2 minutes! **" + democracy + "** votes required.\n\n```Type !yes to kick.```");

                        kickTimer = new System.Timers.Timer(120000);
                        kickTimer.Elapsed += new ElapsedEventHandler(kickTimer_Elapsed);
                        kickTimer.Start();
                        kickTimerRunning = true;

                        votedUsers = new HashSet<string>();
                    }
                }

                // User voted yes to kick during timer.
                if (e.Message.Text == "!yes" && kickTimerRunning == true && !votedUsers.Contains(e.User.Name))
                {
                    democracy -= 1;

                    if (democracy == 1)
                    {
                        await e.Channel.SendMessage("Vote recieved from " + e.User.Name + "! **" + democracy + "** vote remaining. ");
                        votedUsers.Add(e.User.Name);
                    }
                    else
                    {
                        await e.Channel.SendMessage("Vote recieved from " + e.User.Name + "! **" + democracy + "** votes remaining.");
                        votedUsers.Add(e.User.Name);
                    }
                    if (democracy == 0)
                    {
                        // Kick user
                        if (kickTimerRunning == true)
                        {
                            Console.WriteLine("Kicking " + tokick.Name + "...");
                            await e.Channel.SendMessage("Vote passed! Kicking " + tokick.Name + "...  **democracy!**");
                            await tokick.Kick();

                            kickTimer.Stop();
                            kickTimerRunning = false;
                            votedUsers = null;

                            voteKickInProgress = false;
                        }
                    }
                }

                // Random image from search phrase
                if ((e.Message.Text.Contains("!find") || e.Message.Text.Contains("!Find") || e.Message.Text.Contains("!FIND") || e.Message.Text.Contains("!search") || e.Message.Text.Contains("!gimme")) && e.Message.Text[0].Equals('!') && !e.User.IsBot)
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

                    if (urls.Count == 0)
                    {
                        await e.Channel.SendMessage("I couldn't find '" + query + "' :confused:");
                        return;
                    }

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
                                await e.Channel.SendMessage("I found " + query + "!");
                            else if (query[0] == 'a' || query[0] == 'e' || query[0] == 'i' || query[0] == 'o' || query[0] == 'u'
                            || query[0] == 'A' || query[0] == 'E' || query[0] == 'I' || query[0] == 'O' || query[0] == 'U')
                            {
                                await e.Channel.SendMessage("I found an " + query + "!");
                            }
                            else
                                await e.Channel.SendMessage("I found a " + query + "!");

                            await e.Channel.SendFile("random" + fileType);

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
                                await e.Channel.SendMessage(errorMsg);
                        }
                    }
                }
                prevMsg = e.Message.Text.ToString();
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
                   Console.WriteLine(e.After.Name.ToString() + " joined " + e.After.VoiceChannel.Name.ToString() + ".\n");

                   if (logChat)
                   {
                       using (StreamWriter file = File.AppendText("chat_log.txt"))
                       {
                           file.WriteLine(e.After.Name.ToString() + " joined " + e.After.VoiceChannel.Name.ToString() + ".\n");
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

               if (e.Before.VoiceChannel != null && e.After.VoiceChannel != null && e.Before.VoiceChannel.Name != "AFK" && e.After.VoiceChannel.Name == "AFK")
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

               if (e.Before.VoiceChannel != null && e.After.VoiceChannel != null && e.Before.VoiceChannel.Name == "AFK" && e.After.VoiceChannel.Name != "AFK")
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
                    await client.Connect(BOT_TOKEN, TokenType.Bot);
                    client.SetGame(null);

                    // Done!
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nClient connected!");
                    Console.ResetColor();
                    Console.WriteLine(" \n-----------------\n");

                    if (serverPing)
                    {
                        // Start auto server ping/backup timer
                        autoPingTimer = new System.Timers.Timer(1800000); //600000ms = 10 min, 1200000 = 20 min, 1800000 = 30 min, 3600000 = 1 hr
                        autoPingTimer.Elapsed += new ElapsedEventHandler(autoPingTimer_Elapsed);
                        autoPingTimer.Start();

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
        private static void kickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            kickTimer.Stop();

            // Vote failed
            if (democracy > 0)
            {
                Console.WriteLine("Vote to kick " + tokick.Name + " failed.");
                lastchannel.SendMessage("Kick failed. Not enough users voted.");
            }

            kickTimerRunning = false;
            voteKickInProgress = false;
            votedUsers = null;
        }

        // Ping servers after timer has elapsed
        private static void autoPingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ServerStatus();
        }

        private static void ServerStatus()
        {
            Console.WriteLine("Checking severs...\n");

            TcpClient MinecraftServer = new TcpClient();
            TcpClient StarboundServer = new TcpClient();

            lastSuccessfulPing = DateTime.Now.ToString();

            bool mOnline = false;
            bool sOnline = false;

            // Minecraft
            if (MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
            {
                mOnline = true;
                var pings = new IniFile("pings.ini");
                pings.Write("Minecraft", lastSuccessfulPing, "Pings");

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
                pings.Write("Starbound", lastSuccessfulPing, "Pings");
            }

            // Print status to console
            if (mOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Minecraft Server ONLINE  -  " + ServerIP + ":25565  -  " + lastSuccessfulPing);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Minecraft Server OFFLINE");
            }
            if (sOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Starbound Server ONLINE  -  " + ServerIP + ":21025  -  " + lastSuccessfulPing);
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
            string url;
            string data = "";

            if (safeSearch)
            {
                url = "https://www.google.com/search?q=" + s + "&safe=active&tbm=isch";
            }
            else
            {
                url = "https://www.google.com/search?q=" + s + "&tbm=isch";
            }

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