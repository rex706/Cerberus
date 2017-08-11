using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Cerberus_GUI2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DiscordSocketClient client;

        private static string versionURL = "http://textuploader.com/drwm9/raw";

        private static ISocketMessageChannel lastchannel;

        private static string ServerIP = "50.89.243.222";
        private static string lastSuccessfulPing = "never";
        private static string afkChannelName = "💤 AFK";
        private static string jailRoleName = "Inmate";
        private static string errorMsg = "Something went wrong :confused: Please try again!";

        private static Timer kickTimer;
        private static bool kickTimerRunning = false;
        private static HashSet<string> votedUsers;

        private static Timer autoPingTimer;
        private static Timer spamTimer;
        private static Timer jailCheckTimer;

        private static int numUsers;
        private static int democracy;

        private static SocketGuildUser tokick;

        private static bool voteKickInProgress = false;

        private static bool logChat = false;
        private static bool serverPing = false;
        private static bool safeSearch = false;
        private static bool logUsers = false;
        private static bool antiSpam = false;

        private static HashSet<string> userSet;
        private static HashSet<string> blackList;

        private static LinkedList<string> messageSpamQueue = new LinkedList<string>();
        private static LinkedList<string> userSpamQueue = new LinkedList<string>();

        private static SocketGuild threefourteen;
        private static SocketTextChannel generalChannel;
        private static SocketTextChannel minecraftChannel;
        private static SocketTextChannel mod_chat;
        private static SocketTextChannel devChannel;
        private static SocketTextChannel jailTextChannel;

        private static SocketGuild selectedGuild;
        private static SocketChannel selectedChannel;
        private static SocketGuildUser selectedUser;
        private static SocketVoiceChannel jailVoiceChannel;

        private static Brush defaultBrush;

        private static int connectionCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_Loaded(object _sender, RoutedEventArgs _e)
        {
            // Verion number from assembly
            var AssemblyVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            MenuItem ver = new MenuItem();
            MenuItem newExistMenuItem = (MenuItem)this.FileMenu.Items[2];
            ver.Header = "v" + AssemblyVer;
            VersionBox.Text = "v" + AssemblyVer;
            ver.IsEnabled = false;
            newExistMenuItem.Items.Add(ver);

            // Check for a new version.
            if (await UpdateCheck.CheckForUpdate(versionURL) == 1)
            {
                // An update is available, but user has chosen not to update.
                ver.Header = "Update Available!";
                ver.Click += Ver_Click;
                ver.IsEnabled = true;
            }

            InputTextBox.KeyDown += new KeyEventHandler(tb_KeyDown);

            // Set up the default brush color used for the console.
            var converter = new System.Windows.Media.BrushConverter();
            defaultBrush = (Brush)converter.ConvertFromString("#FFD1D1D1");

            ConsoleBox.Items.Add("Creating Client");

            client = new DiscordSocketClient();
            
            userSet = new HashSet<string>();
            blackList = new HashSet<string>();
            connectionCount = 0;

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

            // Define Events
            ConsoleBox.Items.Add("Defining Events");
            client.MessageReceived += MessageRecieved;
            client.UserUpdated += UserUpdated;
            client.MessageDeleted += MessageDeleted;
            client.UserJoined += UserJoinedAsync;
            client.UserVoiceStateUpdated += UserVoiceStateUpdated;

            // Connect bot and start timers
            ConsoleBox.Items.Add("Connecting...");

            try
            {
                // bot token
                string token = "MjA2OTU1MjcwMzIzMTc1NDI2.C_JuKg.QQvyRkMFHDKoICJq7VweQ7mZYTU";
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
            }
            catch (Exception m)
            {
                ConsoleBox.Items.Add(setupItemContext(m.Message, Brushes.Red, 1));
            }

            client.Ready += () =>
            {
                // Done!
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (connectionCount == 0)
                    {
                        ConsoleBox.Items.Add(setupItemContext("Client connected!", Brushes.LimeGreen, 0));
                    }
                    else
                    {
                        ConsoleBox.Items.Add("--------------------");
                        ConsoleBox.Items.Add(setupItemContext("Client reconnected!", Brushes.LimeGreen, 0));
                    }
                    
                    ConsoleBox.Items.Add("--------------------");

                    connectionCount++;
                }));

                // Get important channels.
                threefourteen = client.GetGuild(97459030741508096);
                generalChannel = client.GetChannel(97459030741508096) as SocketTextChannel;
                minecraftChannel = client.GetChannel(206980643148529665) as SocketTextChannel;
                mod_chat = client.GetChannel(236670943387320333) as SocketTextChannel;
                devChannel = client.GetChannel(206951913789325312) as SocketTextChannel;
                jailTextChannel = client.GetChannel(311291896733499403) as SocketTextChannel;
                jailVoiceChannel = client.GetChannel(207717697331527681) as SocketVoiceChannel;

                IEnumerable<SocketGuild> guilds = client.Guilds;
                var guildList = guilds.ToList().OrderBy(i => i.Name);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    GuildsBox.Items.Clear();
                    GuildsBox.SelectedIndex = -1;

                    ChannelsBox.Items.Clear();
                    ChannelsBox.SelectedIndex = -1;

                    UsersBox.Items.Clear();
                    UsersBox.SelectedIndex = -1;

                    // Fill guild box with guilds.
                    foreach (SocketGuild guild in guildList)
                    {
                        GuildsBox.Items.Add(guild);
                    }
                }));

                return Task.CompletedTask;
            };

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            // User joined a voice channel after not being previously connected to any.
            if (arg2.VoiceChannel == null && arg3.VoiceChannel != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".", Brushes.Gray, 1));
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        file.WriteLine(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".\n");
                    }
                }

                //threefourteen.DefaultChannel.SendMessageAsync("Welcome back, " + arg1.Username + "!");

                if (logUsers)
                {
                    if (userSet.Add(arg1.Username))
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            ConsoleBox.Items.Add(setupItemContext("Added " + arg1.Username + " to HashSet", Brushes.Gray, 1));
                        }));
                        
                        using (StreamWriter file = File.AppendText("user_names.txt"))
                        {
                            file.WriteLine(arg1.Username);
                        }
                    }
                }
            }

            // User disconnected.
            if (arg2.VoiceChannel != null && arg3.VoiceChannel == null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " left.", Brushes.Gray, 1));
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        file.WriteLine(arg1.Username + " left.");
                    }
                }
            }

            // User went AFK.
            if (arg2.VoiceChannel != null && arg3.VoiceChannel != null && arg2.VoiceChannel.Name != afkChannelName && arg3.VoiceChannel.Name == afkChannelName)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " went afk.", Brushes.Gray, 1));
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        file.WriteLine(arg1.Username + " went afk.");
                    }
                }
            }

            // User came back from being AFK.
            if (arg2.VoiceChannel != null && arg3.VoiceChannel != null && arg2.VoiceChannel.Name == afkChannelName && arg3.VoiceChannel.Name != afkChannelName)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " is no longer afk.", Brushes.Gray, 1));
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".", Brushes.Gray, 1));
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        file.WriteLine(arg1.Username + " is no longer afk.");
                        file.WriteLine(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".\n");
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task UserUpdated(SocketUser arg1, SocketUser arg2)
        {
            numUsers = 0;

            // Refresh users list box if someone has come online or gone offline.
            if ((arg1.Status == UserStatus.Offline && arg2.Status == UserStatus.Online) || (arg1.Status == UserStatus.Online && arg2.Status == UserStatus.Offline))
            {
                IEnumerable<SocketGuildUser> users = selectedGuild.Users;

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    UsersBox.Items.Clear();

                    // Load the user box with users.
                    foreach (SocketGuildUser user in users)
                    {
                        if (!user.IsBot && user.Status != UserStatus.Offline)
                        {
                            UsersBox.Items.Add(user);
                            numUsers++;
                        }
                    }
                }));
            }

            return Task.CompletedTask;
        }

        private async Task UserJoinedAsync(SocketGuildUser arg)
        {
            await arg.SendMessageAsync("Welcome, " + arg.Username + "!\nType '!help' for a list of available commands.");
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            var deleted = await arg1.GetOrDownloadAsync();

            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
             {
                 ConsoleBox.Items.Add(setupItemContext("Deleted: " + deleted.Author.Username + ": " + deleted.Content, defaultBrush, 1));
             }));

            if (logChat)
            {
                using (StreamWriter file = File.AppendText("chat_log.txt"))
                {
                    file.WriteLine("Deleted: " + deleted.Author.Username + ": " + deleted.Content + "\n");
                }
            }
        }

        private async Task MessageRecieved(SocketMessage message)
        {
            // Check for spam.
            if (antiSpam && !message.Author.IsBot)
            {
                foreach (string s in messageSpamQueue)
                {
                    if (s == message.Content)
                    {
                        await message.DeleteAsync();
                        await message.Author.SendMessageAsync("Stop spamming!");

                        spamTimer.Start();

                        return;
                    }
                }

                foreach (string s in userSpamQueue)
                {
                    if (s == message.Author.Username)
                    {
                        await message.DeleteAsync();
                        await message.Author.SendMessageAsync("Stop spamming!");

                        spamTimer.Start();

                        return;
                    }
                }

                userSpamQueue.AddFirst(message.Author.Username);
            }

            // Check if user is on the Cerberus black list
            if (blackList.Contains(message.Author.ToString()))
            {
                await message.DeleteAsync();
                await message.Author.SendMessageAsync("You have been black listed from using Cerberus commands. Sorry for the inconvenience!");
                return;
            }

            // Record to text file if not bot.
            if (!message.Author.IsBot || message.Content.Contains(errorMsg))
            {
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                 {
                     if (message.Attachments.Count > 0)
                         ConsoleBox.Items.Add(setupItemContext(message.Author.Username + " [" + message.Channel.Name + "]: (attachment) " + message.Content, defaultBrush, 1));
                     else
                         ConsoleBox.Items.Add(setupItemContext(message.Author.Username + " [" + message.Channel.Name + "]: " + message.Content, defaultBrush, 1));
                 }));

                if (logChat)
                {
                    if (!File.Exists("chat_log.txt"))
                    {
                        using (StreamWriter file = File.CreateText("chat_log.txt"))
                        {
                            if (message.Attachments.Count > 0)
                                file.WriteLine(message.Author.Username + " [" + message.Channel.Name + "]: (attachment) " + message.Content);
                            else
                                file.WriteLine(message.Author.Username + " [" + message.Channel.Name + "]: " + message.Content);

                            file.Close();
                        }
                    }
                    else
                    {
                        using (StreamWriter file = File.AppendText("chat_log.txt"))
                        {
                            if (message.Attachments.Count > 0)
                                file.WriteLine(message.Author.Username + " [" + message.Channel.Name + "]: [attachment] " + message.Content);
                            else
                                file.WriteLine(message.Author.Username + " [" + message.Channel.Name + "]: " + message.Content);

                            file.Close();
                        }
                    }
                }
            }

            // Display help menu.
            if (!message.Author.IsBot && message.Content == "!help")
            {
                await message.Channel.SendMessageAsync("\n\n```css\n#UserCommands```\n" +
                "!help - help menu. (you are here)\n\n" +
                "!tits - show me the money!\n\n" +
                "!find [search phrase] - random image from search phrase.\n" +
                "!find [search phrase] gif - random gif from search phrase.\n\n" +
                "!kick [@mention] - initiate a vote to kick another user.\n" +
                "!yes - vote to kick user.\n\n" +
                "!jail [@mention] - strip all user roles and send user to jail. (mod only)\n\n" +
                "!blacklist - list the blacklisted users, if any.\n" +
                "!blacklist [@mention] - blacklist a user from Cerberus. (mod only)\n\n" +
                "!spam - enable/disable spam control. (mod only) \n\n" +
                "!member - grant all users 'Member' role. (mod only) \n\n" +
                "!minecraft - minecraft server status.\n" +
                "!starbound - starbound server status.\n\n" +
                "https://github.com/rex706/Cerberus");
            }

            // Give all users 'member' role.
            if (!message.Author.IsBot && message.Content == "!member")
            {
                int permission = await Utils.CheckPermissionAsync(threefourteen, message.Author);
                
                if (permission == 1)
                {
                    IEnumerable<SocketGuildUser> users = threefourteen.Users;
                    IEnumerable<IRole> roles = threefourteen.Roles;
                    IRole member = null;

                    foreach (IRole role in roles)
                    {
                        if (role.Name == "Member")
                            member = role;
                    }

                    foreach(SocketGuildUser user in users)
                    {
                        await user.AddRoleAsync(member);
                    }

                    await message.Channel.SendMessageAsync("Member role given to all users!");

                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                     {
                         ConsoleBox.Items.Add(setupItemContext("Member role given to all users!", defaultBrush, 1));
                     }));
                }
            }

            // Toggle spam control mode.
            if (!message.Author.IsBot && message.Content == "!spam")
            {
                int permission = await Utils.CheckPermissionAsync(threefourteen, message.Author);

                if (permission == 1)
                {
                    if (antiSpam == false)
                    {
                        await message.Channel.SendMessageAsync("Spam control has been **enabled**.");

                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            SpamControlCheckBox.IsChecked = true;
                            ConsoleBox.Items.Add(setupItemContext(message.Author + " has enabled spam control.", defaultBrush, 1));
                        }));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Spam control has been **disabled**.");

                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            SpamControlCheckBox.IsChecked = false;
                            ConsoleBox.Items.Add(setupItemContext(message.Author + " has disabled spam control.", defaultBrush, 1));
                        }));
                    }
                }
            }

            // Check for blacklisted users.
            if (!message.Author.IsBot && message.Content == "!blacklist")
            {
                if (blackList.Count == 0)
                {
                    await message.Channel.SendMessageAsync("There are currently 0 blacklisted users.");
                }
                else
                {
                    StringBuilder blUsers = new StringBuilder();

                    foreach (string blUser in blackList)
                        blUsers.Append(blUser + "\n");

                    await message.Channel.SendMessageAsync("Blacklisted users: " + blackList.Count + " \n\n" + blUsers.ToString());
                }
            }

            // Blacklist a user from using Cerberus commands. (Mod only)
            if (!message.Author.IsBot && message.Content.Contains("!blacklist") && message.Content.Length > 14)
            {
                int hasPermission = await Utils.CheckPermissionAsync(threefourteen, message.Author);
                var mentionedUsers = message.MentionedUsers;

                if (hasPermission == 1)
                {
                    foreach(SocketUser user in mentionedUsers)
                    {
                        if (!File.Exists("black_list.txt"))
                        {
                            File.CreateText("black_list.txt");
                        }

                        using (StreamWriter file = File.AppendText("black_list.txt"))
                        {
                            file.WriteLine(user.ToString());
                        }

                        blackList.Add(user.ToString());
                        await message.Channel.SendMessageAsync(user.Mention + " has been blacklisted from Cerberus by " + message.Author.Username);

                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            ConsoleBox.Items.Add(setupItemContext(user.ToString() + " has been blacklisted from Cerberus by " + message.Author.Username, Brushes.Orange, 1));
                        }));
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("You do not have permission to use that command!");
                }
            }

            if (!message.Author.IsBot && message.Content == "!tits")
            {
                using (var httpclient = new System.Net.Http.HttpClient())
                {
                    using (var stream = await httpclient.GetStreamAsync("https://upload.wikimedia.org/wikipedia/commons/8/86/GreatTit002.jpg"))
                        await message.Channel.SendFileAsync(stream, "image.png", "A nice pair of natural tits!");
                }
            }

            // Ping minecraft server
            if (!message.Author.IsBot && message.Content == "!minecraft")
            {
                if (File.Exists("data.ini"))
                {
                    var data = new IniFile("data.ini");
                    lastSuccessfulPing = data.Read("Minecraft", "data");
                }

                TcpClient MinecraftServer = new TcpClient();


                if (!MinecraftServer.ConnectAsync(ServerIP, 25565).Wait(3500))
                {
                    await message.Channel.SendMessageAsync("Minecraft Server \n\n```css\n:OFFLINE``` \nLast successful ping: **" + lastSuccessfulPing +
                        "**\n\nCould just be ping issue? Try again in a few seconds.\nIf connection continues to fail, try again later.");
                }
                else
                {
                    await message.Channel.SendMessageAsync("Minecraft Server \n\n```css\n.:ONLINE  -  " + ServerIP + ":25565```");

                    var data = new IniFile("data.ini");
                    lastSuccessfulPing = DateTime.Now.ToString();
                    data.Write("Minecraft", lastSuccessfulPing, "data");
                }
            }

            // Ping starbound server
            if (!message.Author.IsBot && message.Content == "!starbound")
            {
                if (File.Exists("data.ini"))
                {
                    var data = new IniFile("data.ini");
                    lastSuccessfulPing = data.Read("Starbound", "data");

                    if (lastSuccessfulPing == "")
                        lastSuccessfulPing = "never";
                }

                TcpClient StarboundServer = new TcpClient();

                if (!StarboundServer.ConnectAsync(ServerIP, 21025).Wait(3500))
                {
                    await message.Channel.SendMessageAsync("Starbound Server \n\n```css\n:OFFLINE``` \nLast successful ping: **" + lastSuccessfulPing +
                        "**\n\nCould just be ping issue? Try again in a few seconds.\nIf connection continues to fail, try again later.");
                }
                else
                {
                    await message.Channel.SendMessageAsync("Starbound Server \n\n```css\n.:ONLINE  -  " + ServerIP + ":21025```");

                    var data = new IniFile("data.ini");
                    lastSuccessfulPing = DateTime.Now.ToString();
                    data.Write("Starbound", lastSuccessfulPing, "data");
                }
            }

            // Strip user roles and send them to jail. (mod only)
            if (!message.Author.IsBot && message.Content.Split(' ')[0] == "!jail")
            {
                string[] s = message.Content.Split(' ');
                double time = 600; // 600s = 10 minutes default.
                double seconds = 0;
                string length = "seconds";

                if (s.Count() == 3)
                {
                    try
                    {
                        time = Int32.Parse(s[2]);
                    }
                    catch (Exception m)
                    {
                        await message.Channel.SendMessageAsync(m.Message);
                        return;
                    }
                }
                else if (s.Count() == 4)
                {
                    try
                    {
                        time = Int32.Parse(s[2]);
                        length = s[3];
                    }
                    catch (Exception m)
                    {
                        await message.Channel.SendMessageAsync(m.Message);
                        return;
                    }

                    if (length[0] == 'd')
                    {
                        seconds = time * 86400;
                    }
                    else if (length[0] == 'h')
                    {
                        seconds = time * 3600;
                    }
                    else if (length[0] == 'm')
                    {
                        seconds = time * 60;
                    }
                    else if (length[0] == 's')
                    {
                        seconds = time;
                    }
                }

                int permission = await Utils.CheckPermissionAsync(threefourteen, message.Author);
                var tojail = threefourteen.GetUser(message.MentionedUsers.First().Id);

                // Prevent a user with higher level roles from being sent to jail.
                if (await Utils.CheckPermissionAsync(threefourteen, tojail) == 1)
                {
                    await message.Channel.SendMessageAsync("You can't send " + tojail.Username + " to jail!");
                    return;
                }

                IEnumerable<IRole> userRoles = tojail.Roles;
                IEnumerable<IRole> guildRoles = tojail.Guild.Roles;

                IRole jailrole = null;

                if (permission == 1)
                {
                    // Get jail role.
                    foreach(IRole role in guildRoles)
                    {
                        if (role.Name == jailRoleName)
                            jailrole = role;
                    }

                    // Strip all roles from user being sent to jail.
                    foreach (IRole role in userRoles)
                    {
                        if (role.Name != "@everyone")
                            await tojail.RemoveRoleAsync(role);
                    }

                    // Apply the jail role to user being sent to jail.
                    await tojail.AddRoleAsync(jailrole);

                    // Move user to the jail voice channel.
                    await tojail.ModifyAsync(u => u.Channel = jailVoiceChannel);

                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        ConsoleBox.Items.Add(setupItemContext(tojail + " has been jailed by " + message.Author + " for " + time + " " + length + "!", Brushes.Orange, 1));
                    }));

                    await jailTextChannel.SendMessageAsync("Welcome to jail " + tojail.Mention + "!\n\nYour sentence is: " + time + " " + length + ".");
                }
            }

            // Make sure only one vote can be in progress at a time.
            if (message.Content.Contains("!kick") && !message.Author.IsBot && voteKickInProgress == true)
            {
                await message.Channel.SendMessageAsync("Another vote is in progress! Please try again after voting has finished.");
            }

            // Vote to kick.
            if (message.Content.Contains("!kick") && !message.Author.IsBot && voteKickInProgress == false)
            {
                IEnumerable<SocketUser> mentionedUser = message.MentionedUsers;
                tokick = threefourteen.GetUser(mentionedUser.First().Id);

                if (tokick == null)
                {
                    await message.Channel.SendMessageAsync("Invalid user!");
                }
                else
                {
                    if (await Utils.CheckPermissionAsync(threefourteen, tokick) == 1)
                    {
                        await message.Channel.SendMessageAsync("You cannot kick " + tokick.Username + "!");
                        return;
                    }

                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                     {
                         ConsoleBox.Items.Add(setupItemContext(message.Author.Username + " initiated vote to kick " + tokick.Username + ".", defaultBrush, 1));
                     }));

                    voteKickInProgress = true;
                    lastchannel = message.Channel;

                    if (numUsers / 2 >= 5)
                    {
                        democracy = numUsers / 2;
                    }
                    else
                    {
                        democracy = 5;
                    }

                    if (democracy == 1)
                        await message.Channel.SendMessageAsync("Vote to kick " + tokick.Mention + " initiated for 2 minutes! **" + democracy + "** vote required.\n\n```Type !yes to kick.```");
                    else
                        await message.Channel.SendMessageAsync("Vote to kick " + tokick.Mention + " initiated for 2 minutes! **" + democracy + "** votes required.\n\n```Type !yes to kick.```");

                    kickTimer = new System.Timers.Timer(120000);
                    kickTimer.Elapsed += new ElapsedEventHandler(kickTimer_Elapsed);
                    kickTimer.Start();
                    kickTimerRunning = true;

                    votedUsers = new HashSet<string>();
                }
            }

            // User voted yes to kick during timer.
            if (!message.Author.IsBot && message.Content == "!yes" && kickTimerRunning == true && !votedUsers.Contains(message.Author.Username))
            {
                democracy -= 1;

                if (democracy == 1)
                {
                    await message.Channel.SendMessageAsync("Vote recieved from " + message.Author.Username + "! **" + democracy + "** vote remaining. ");
                    votedUsers.Add(message.Author.Username);
                }
                else
                {
                    await message.Channel.SendMessageAsync("Vote recieved from " + message.Author.Username + "! **" + democracy + "** votes remaining.");
                    votedUsers.Add(message.Author.Username);
                }
                if (democracy == 0)
                {
                    // Kick user
                    if (kickTimerRunning == true)
                    {

                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                         {
                             ConsoleBox.Items.Add(setupItemContext("Kicking " + tokick.Username + "...", Brushes.Red, 1));
                         }));
                        
                        await message.Channel.SendMessageAsync("Vote passed! Kicking " + tokick.Username + "...  **democracy!**");
                        await tokick.KickAsync();

                        kickTimer.Stop();
                        kickTimerRunning = false;
                        votedUsers = null;

                        voteKickInProgress = false;
                    }
                }
            }

            // Random image from search phrase
            if (!message.Author.IsBot && message.Content.Split(' ')[0] == "!find")
            {
                string[] phrase = message.Content.Split(' ');
                bool plural = false;

                StringBuilder query = new StringBuilder();

                if (phrase.Length > 2)
                {
                    for (int i = 1; i < phrase.Length; i++)
                    {
                        // Delete all @ symbols.
                        phrase[i] = phrase[i].Replace("@", "");

                        if (i == phrase.Length - 1)
                            query.Append(phrase[i]);
                        else
                            query.Append(phrase[i]).Append(" ");
                    }

                    plural = true;
                }
                else
                {
                    // Delete all @ symbols.
                    phrase[1] = phrase[1].Replace("@", "");
                    query.Append(phrase[1]);
                }

                if (query[query.Length - 1].Equals('s'))
                    plural = true;

                string html = null;

                // Ignore safe search for nsfw channel.
                if (message.Channel.Name == "nsfw" || message.Channel.Name == "dev")
                {
                    html = Utils.GetHtmlCode(query.ToString(), false);
                }
                else
                {
                    html = Utils.GetHtmlCode(query.ToString(), safeSearch);
                }

                List<string> urls = Utils.GetUrls(html);
                var rnd = new Random();

                if (urls.Count == 0)
                {
                    await message.Channel.SendMessageAsync("I couldn't find '" + query + "' :confused:");
                    return;
                }

                int random = rnd.Next(0, urls.Count - 1);
                string luckyUrl = urls[random];
                int dotIdx = luckyUrl.LastIndexOf(".");
                string fileType = luckyUrl.Substring(dotIdx);

                if (message.Content.Contains(" gif"))
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

                 await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                 {
                     ConsoleBox.Items.Add(setupItemContext(message.Author.Username + " queried '" + query + "' in #" + message.Channel.Name + "\n" + luckyUrl, Brushes.Khaki, 1));
                 }));
                    
                using (var httpclient = new System.Net.Http.HttpClient())
                {
                    using (var stream = await httpclient.GetStreamAsync(luckyUrl))
                    {
                        try
                        {
                            if (plural)
                            {
                                await message.Channel.SendFileAsync(stream, "image" + fileType, "I found " + query + "!");
                            }
                            else if (query[0] == 'a' || query[0] == 'e' || query[0] == 'i' || query[0] == 'o' || query[0] == 'u'
                            || query[0] == 'A' || query[0] == 'E' || query[0] == 'I' || query[0] == 'O' || query[0] == 'U')
                            {
                                await message.Channel.SendFileAsync(stream, "image" + fileType, "I found an " + query + "!");
                            }
                            else
                            {
                                await message.Channel.SendFileAsync(stream, "image" + fileType, "I found a " + query + "!");
                            }
                        }
                        catch (Exception m)
                        {
                            await message.Channel.SendMessageAsync(m.Message);
                        }
                    }
                        
                }

                // Add message to messageSpamQueue.
                if (antiSpam && !message.Author.IsBot)
                {
                    string[] text = message.Content.Split(' ');

                    // If user is using the find or help command, ignore it. (Might be dangerous)
                    if (text[0] == "!find" || text[0] == "!help")
                    {
                        return;
                    }

                    // If the queue is not full, add message.
                    if (messageSpamQueue.Count < 3)
                    {
                        messageSpamQueue.AddFirst(message.Content);
                    }
                    // If queue is full, dequeue last item and add new message to the beginning.
                    else
                    {
                        messageSpamQueue.RemoveLast();
                        messageSpamQueue.AddFirst(message.Content);
                    }
                }
            }
        }

        // Set up timestamp and copy to clipboard context menu items.
        private ListBoxItem setupItemContext(string text, Brush textColor, int flag)
        {
            // Create new listbox item and assign text value.
            ListBoxItem item = new ListBoxItem();
            item.Content = text;

            // Create new context menu and menu items.
            ContextMenu itemContext = new ContextMenu();
            MenuItem timeStamp = new MenuItem();
            MenuItem copy = new MenuItem();

            timeStamp.Header = DateTime.Now;
            timeStamp.IsEnabled = false;

            copy.Header = "Copy";
            copy.Click += Copy_Click;

            itemContext.Items.Add(timeStamp);

            if (flag == 1)
                itemContext.Items.Add(copy);

            // Add context menu to item.
            item.ContextMenu = itemContext;
            item.Foreground = textColor;

            return item;
        }

        private ListBoxItem setupItemColor(string text, Brush textColor, int flag)
        {
            // Create new listbox item and assign text value.
            ListBoxItem item = new ListBoxItem();
            item.Content = text;

            if (flag == 1)
            {
                // Create new context menu and menu items.
                ContextMenu itemContext = new ContextMenu();
                MenuItem copy = new MenuItem();
                copy.Header = "Copy";
                copy.Click += Copy_Click;
                itemContext.Items.Add(copy);
                item.ContextMenu = itemContext;
            }

            item.Foreground = textColor;

            return item;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            // Get the timestamp from the selected item.
            ListBoxItem item = (ListBoxItem)ConsoleBox.ItemContainerGenerator.ContainerFromItem(ConsoleBox.SelectedItem);

            string timeStamp = item.ContextMenu.Items[0].ToString();
            int start = timeStamp.IndexOf(":") + 1;
            int end = timeStamp.IndexOf("Items");
            int length = end - start;
            timeStamp = timeStamp.Substring(start, length);

            // Copy contents of the selected listbox item to the clipboard.
            Clipboard.SetText(timeStamp + " " + item.Content.ToString());
        }

        // Vote to kick timer ended
        private void kickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            kickTimer.Stop();

            // Vote failed
            if (democracy > 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext("Vote to kick " + tokick.Username + " failed.", Brushes.Yellow, 1));
                }));

                lastchannel.SendMessageAsync("Kick failed. Not enough users voted.");
            }

            kickTimerRunning = false;
            voteKickInProgress = false;
            votedUsers = null;
        }

        // Ping servers after timer has elapsed
        private void autoPingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Utils.ServerStatus(ServerIP, minecraftChannel);
        }

        private void spamTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Dequeue user from userSpamQueue and message from messageSpamQueue
            if (userSpamQueue.Count > 0)
                userSpamQueue.RemoveLast();

            if (messageSpamQueue.Count > 0)
                messageSpamQueue.RemoveLast();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (InputTextBox.Text.Length > 0)
            {
                SendAsBot();
            }
        }

        private void SendAsBot()
        {
            string s = InputTextBox.Text.Substring(InputTextBox.Text.IndexOf(':') + 1);

            // Send text from text box to Discord as a message from Cerberus.
            if (selectedUser != null)
            {
                selectedUser.SendMessageAsync(s);
            }
            else if (selectedChannel == null)
            {
                threefourteen.DefaultChannel.SendMessageAsync(s);
            }
            else
            {
                (selectedChannel as SocketTextChannel)?.SendMessageAsync(s);
            }

            // Regurgitate output to GUI console.
            ConsoleBox.Items.Add(setupItemContext("Cerberus " + InputTextBox.Text, defaultBrush, 1));

            // Reset input text box.
            InputTextBox.Clear();

            if (selectedUser != null)
            {
                InputTextBox.Text = "[" + selectedUser + "]: ";
            }
            else if (selectedChannel != null)
            {
                InputTextBox.Text = "[" + selectedChannel + "]: ";
            }
            else if (selectedGuild != null)
            {
                InputTextBox.Text = "[" + selectedGuild.DefaultChannel + "]: ";
            }
        }

        private void ChatLogCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            logChat = true;
        }

        private void ChatLogCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            logChat = false;
        }

        private void LogUsersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            logUsers = true;
        }

        private void LogUsersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            logUsers = false;
        }

        private void PingServersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            serverPing = true;
            autoPingTimer = new System.Timers.Timer(1800000); //600000ms = 10 min, 1200000 = 20 min, 1800000 = 30 min, 3600000 = 1 hr
            autoPingTimer.Elapsed += new ElapsedEventHandler(autoPingTimer_Elapsed);
            autoPingTimer.Start();

            Utils.ServerStatus(ServerIP, minecraftChannel);
        }

        private void PingServersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            serverPing = false;
            autoPingTimer.Stop();
        }

        private void SafeSearchCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            safeSearch = true;
        }

        private void SafeSearchCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            safeSearch = false;
        }

        private void SpamControlCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            antiSpam = true;
            spamTimer = new System.Timers.Timer(2000); //2000ms = 2s
            spamTimer.Elapsed += new ElapsedEventHandler(spamTimer_Elapsed);
            spamTimer.Start();
        }

        private void SpamControlCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            antiSpam = false;
            spamTimer.Stop();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/rex706/Cerberus");
        }

        private async void Ver_Click(object sender, RoutedEventArgs e)
        {
            await UpdateCheck.CheckForUpdate(versionURL);
        }

        private void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && InputTextBox.Text.Length > 0)
            {
                SendAsBot();
            }
        }

        private void GuildsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset all variables for newly selected guild.
            ChannelsBox.Items.Clear();
            UsersBox.Items.Clear();

            UsersBox.SelectedIndex = -1;
            ChannelsBox.SelectedIndex = -1;

            selectedUser = null;
            selectedChannel = null;
            selectedGuild = null;

            if (GuildsBox.SelectedIndex != -1)
            {
                // Load new variables with selected content.
                selectedGuild = GuildsBox.SelectedItem as SocketGuild;

                InputTextBox.Clear();
                InputTextBox.Text = "[" + selectedGuild.DefaultChannel + "]: ";

                IEnumerable<SocketGuildChannel> channels = selectedGuild.Channels;
                IEnumerable<SocketGuildUser> users = selectedGuild.Users;

                var channelList = channels.ToList().OrderBy(i => i.Name);

                // Load the channel box with channels.
                foreach (SocketGuildChannel channel in channelList)
                {
                    var ch = channel as SocketTextChannel;

                    if (ch != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            ChannelsBox.Items.Add(channel);
                        }));
                    }

                }

                numUsers = 0;
                var userList = users.ToList().OrderBy(i => i.Status, new UserStatusComparer()).ThenBy(i => i.Username);

                // Load the user box with users.
                foreach (SocketGuildUser user in userList)
                {
                    if (!user.IsBot /* && user.Status != UserStatus.Offline */)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (user.Status == UserStatus.Offline)
                            {
                                UsersBox.Items.Add(setupItemColor(user.ToString(), Brushes.Gray, 0));
                            }
                            else if (user.Status == UserStatus.Idle)
                            {
                                UsersBox.Items.Add(setupItemColor(user.ToString(), Brushes.Orange, 0));
                            }
                            else if (user.Status == UserStatus.Online)
                            {
                                UsersBox.Items.Add(setupItemColor(user.ToString(), defaultBrush, 0));
                            }
                            else if (user.Status == UserStatus.DoNotDisturb)
                            {
                                UsersBox.Items.Add(setupItemColor(user.ToString(), Brushes.Red, 0));
                            }
                            else if (user.Status == UserStatus.AFK)
                            {
                                UsersBox.Items.Add(setupItemColor(user.ToString(), Brushes.SteelBlue, 0));
                            }
                            else if (user.Status == UserStatus.Invisible)
                            {
                                UsersBox.Items.Add(setupItemColor(user.ToString(), Brushes.MediumPurple, 0));
                            }
                        }));

                        numUsers++;
                    }
                }
            }
        }

        private void ChannelsBox_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            selectedChannel = null;

            if (ChannelsBox.SelectedIndex != -1)
            {              
                UsersBox.SelectedIndex = -1;
                selectedUser = null;
                selectedChannel = ChannelsBox.SelectedItem as SocketTextChannel;

                InputTextBox.Clear();
                InputTextBox.Text = "[" + selectedChannel + "]: ";
            }
        }

        private void UsersBox_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = null;

            if (UsersBox.SelectedIndex != -1)
            {
                ChannelsBox.SelectedIndex = -1;
                selectedChannel = null;
                selectedUser = UsersBox.SelectedItem as SocketGuildUser;

                InputTextBox.Clear();
                InputTextBox.Text = "[" + selectedUser + "]: ";
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to clear the console?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Clear();
                }));
            }
        }
    }
}
