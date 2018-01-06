using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

// TODO:
// Finish server pinging. Probably should find a better way to streamline this process.
// Find a way to have a dynamic amount of timers in different situations.
// Implement 'Add Guild'.
// Jail timer and saving info.
// Decide wether or not to keep full fledged steam market scraping.
//  - Searching
//  - Comparing?
//  - Graphs?

namespace Cerberus_GUI2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static DiscordSocketClient client;

        private static string versionURL = "http://textuploader.com/drwm9/raw";

        private static ISocketMessageChannel lastchannel;

        private static string token = "TOKEN";
        private static string afkChannelName = "💤 AFK";
        private static string jailRoleName = "Inmate";
        private static string errorMsg = "Something went wrong :confused: Please try again!";

        private static System.Timers.Timer kickTimer;
        private static bool kickTimerRunning = false;
        private static HashSet<string> votedUsers;

        public static System.Timers.Timer autoPingTimer;
        public static System.Timers.Timer spamTimer;
        public static System.Timers.Timer jailCheckTimer;
        public static System.Timers.Timer gamescomTimer;

        private static int numUsers;
        private static int democracy;

        private static SocketGuildUser tokick;

        private static bool voteKickInProgress = false;

        public static bool logChat = false;
        public static bool serverPing = false;
        public static bool safeSearch = false;
        public static bool logUsers = false;
        public static bool antiSpam = false;
        public static bool ignoreBots = false;
        private static bool connected = false;

        private static HashSet<string> userSet;
        private static HashSet<string> blackList;

        private static LinkedList<string> messageSpamQueue = new LinkedList<string>();
        private static LinkedList<string> userSpamQueue = new LinkedList<string>();

        private static SocketGuild threefourteen;
        private static SocketTextChannel jailTextChannel;

        private static SocketGuild selectedGuild;
        private static SocketChannel selectedChannel;
        private static SocketGuildUser selectedUser;
        private static SocketVoiceChannel jailVoiceChannel;

        private static Brush defaultBrush;

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

            ConsoleBox.Items.Add("Loading settings");

            // Create settings file if it doesn't exist.
            // Load settings file otherwise. 
            if (!File.Exists("settings.ini"))
            {
                var settingsFile = new IniFile("settings.ini");
                settingsFile.Write("Version", AssemblyVer, "System");
                settingsFile.Write("Log Chat", "False", "Settings");
                settingsFile.Write("Log Users", "False", "Settings");
                settingsFile.Write("Ping Servers", "False", "Settings");
                settingsFile.Write("Safe Search", "False", "Settings");
                settingsFile.Write("Spam Control", "False", "Settings");
                settingsFile.Write("Ignore Bots", "False", "Settings");

                // Force user to enter a token in the settings window.
                while (!settingsFile.KeyExists("Token", "Settings") || token.Length < 8)
                {
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.SettingsTabs.SelectedIndex = 1;
                    settingsWindow.ShowDialog();
                    token = settingsFile.Read("Token", "Settings");
                }
            }
            else
            {
                var settingsFile = new IniFile("settings.ini");

                // Force user to enter a token in the settings window if ini key does not exist or the token length is too short.
                while (!settingsFile.KeyExists("Token", "Settings") || settingsFile.Read("Token", "Settings").Length < 8)
                {
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.SettingsTabs.SelectedIndex = 1;
                    settingsWindow.ShowDialog();
                    token = settingsFile.Read("Token", "Settings");
                }

                token = settingsFile.Read("Token", "Settings");
                logChat = Convert.ToBoolean(settingsFile.Read("Log Chat", "Settings"));
                logUsers = Convert.ToBoolean(settingsFile.Read("Log Users", "Settings"));
                serverPing = Convert.ToBoolean(settingsFile.Read("Ping Servers", "Settings"));
                safeSearch = Convert.ToBoolean(settingsFile.Read("Safe Search", "Settings"));
                antiSpam = Convert.ToBoolean(settingsFile.Read("Spam Control", "Settings"));
                ignoreBots = Convert.ToBoolean(settingsFile.Read("Ignore Bots", "Settings"));
            }

            // Make sure IE11 registry exists for some web checking commands.
            Utils.CheckRegistry();

            // Create event handler for when the enter key is pushed while the input text box is in focus. 
            InputTextBox.KeyDown += new KeyEventHandler(tb_KeyDown);

            // Set up the default brush color used for the console.
            defaultBrush = (Brush)new BrushConverter().ConvertFromString("#FFD1D1D1");

            // Connect the bot client.
            LoginClient();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async void LoginClient()
        {
            // Create new Discord client and initialize any remaining global variables. 
            ConsoleBox.Items.Add("Creating Client");

            client = new DiscordSocketClient();

            userSet = new HashSet<string>();
            blackList = new HashSet<string>();

            // Define Events
            ConsoleBox.Items.Add("Defining Events");

            client.MessageReceived += MessageRecieved;
            client.UserVoiceStateUpdated += UserVoiceStateUpdated;
            client.GuildMemberUpdated += GuildUserUpdated;
            client.Disconnected += ClientDisconnected;
            client.Connected += ClientConnected;
            client.Ready += ClientReady;
            client.ChannelUpdated += ChannelUpdated;
            client.ChannelCreated += ChannelCreated;
            client.ChannelDestroyed += ChannelDestroyed;
            //client.MessageDeleted += MessageDeleted;
            //client.UserJoined += UserJoinedAsync;

            // Connect bot and start timers
            ConsoleBox.Items.Add("Connecting...");

            try
            {
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
            }
            catch (Exception m)
            {
                ConsoleBox.Items.Add(setupItemContext(m.Message, Brushes.Red, new int[] { 1 }));
            }
        }

        private Task ClientConnected()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                ConsoleBox.Items.Add(setupItemContext("Client connected!", Brushes.LimeGreen, new int[] {}));
            }));

            return Task.CompletedTask;
        }

        private Task ClientReady()
        {
            if (connected == false)
            {
                connected = true;

                // Get important channels. Should probably not do this.
                threefourteen = client.GetGuild(97459030741508096);
                jailTextChannel = client.GetChannel(311291896733499403) as SocketTextChannel;
                jailVoiceChannel = client.GetChannel(207717697331527681) as SocketVoiceChannel;

                refreshGuilds();

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext("Ready!", defaultBrush, new int[] {}));
                    ConsoleBox.Items.Add("--------------------");
                }));
            }

            gamescomTimer = new System.Timers.Timer(1.8e+6); // 30 minutes
            gamescomTimer.Elapsed += GamescomTimer_ElapsedAsync;
            gamescomTimer.Start();

            return Task.CompletedTask;
        }

        private Task ClientDisconnected(Exception arg)
        {
            if (connected == true)
            {
                connected = false;

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (ConsoleBox.Items[ConsoleBox.Items.Count - 1].ToString() != "--------------------")
                        ConsoleBox.Items.Add("--------------------");

                    ConsoleBox.Items.Add(setupItemContext("Client disconnected.", Brushes.Red, new int[] {}));
                    ConsoleBox.Items.Add("--------------------");
                }));
            }
            
            return Task.CompletedTask;
        }

        private Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            // User joined a voice channel after not being previously connected to any.
            if (arg2.VoiceChannel == null && arg3.VoiceChannel != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".", Brushes.Gray, new int[] { 1 }));
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        file.WriteLine(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".\n");
                    }
                }

                if (logUsers)
                {
                    if (userSet.Add(arg1.Username))
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            ConsoleBox.Items.Add(setupItemContext("Added " + arg1.Username + " to HashSet", Brushes.Gray, new int[] { 1 }));
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
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " left.", Brushes.Gray, new int[] { 1 }));
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
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " went afk.", Brushes.Gray, new int[] { 1 }));
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
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " is no longer afk.", Brushes.Gray, new int[] { 1 }));
                    ConsoleBox.Items.Add(setupItemContext(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".", Brushes.Gray, new int[] { 1 }));
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

        private Task ChannelDestroyed(SocketChannel arg)
        {
            refreshChannels();

            return Task.CompletedTask;
        }

        private Task ChannelCreated(SocketChannel arg)
        {
            refreshChannels();

            return Task.CompletedTask;
        }

        private Task ChannelUpdated(SocketChannel arg1, SocketChannel arg2)
        {
            refreshChannels();

            return Task.CompletedTask;
        }

        // Refresh Guilds listbox.
        private int refreshGuilds()
        {
            IEnumerable<SocketGuild> guilds = client.Guilds;
            var guildList = guilds.ToList().OrderBy(i => i.Name);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                GuildsBox.Items.Clear();
                GuildsBox.SelectedIndex = -1;

                // Fill guild box with guilds from search.
                foreach (SocketGuild guild in guildList)
                {
                    if ((GuildsSearchBox.Text.Length > 0 && guild.Name.ToLower().Contains(GuildsSearchBox.Text.ToLower())) || GuildsSearchBox.Text.Length == 0)
                    {
                        GuildsBox.Items.Add(guild);
                    }      
                }
            }));

            return client.Guilds.Count();
        }

        // Refresh Channels list box.
        private int refreshChannels()
        {
            int counter = 0;

            IEnumerable<SocketGuildChannel> channels = selectedGuild.Channels;
            var channelList = channels.ToList().OrderBy(i => i.Name);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                ChannelsBox.Items.Clear();
                ChannelsBox.SelectedIndex = -1;

                foreach (SocketGuildChannel channel in channelList)
                {
                    var textChannel = channel as SocketTextChannel;

                    if (textChannel != null && (ChannelsSearchBox.Text.Length > 0 && textChannel.Name.ToLower().Contains(ChannelsSearchBox.Text.ToLower()) || ChannelsSearchBox.Text.Length == 0))
                    {
                        ChannelsBox.Items.Add(channel);
                        counter++;
                    }
                }
            }));

            return counter;
        }

        // Refresh Users list box.
        private int refreshUsers()
        {
            IEnumerable<SocketGuildUser> users = selectedGuild.Users;
            var userList = users.ToList().OrderBy(i => i.Status, new UserStatusComparer()).ThenBy(i => i.Username);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                UsersBox.Items.Clear();

                // Load the user box with users.
                foreach (SocketGuildUser user in userList)
                {
                    if (!user.IsBot)
                    {
                        if ((UsersSearchBox.Text.Length > 0 && user.Username.ToLower().Contains(UsersSearchBox.Text.ToLower())) || UsersSearchBox.Text.Length == 0)
                        {
                            if (user.Status == UserStatus.Offline)
                            {
                                UsersBox.Items.Add(setupUserItem(user, Brushes.Gray, new int[] { 2 }));
                            }
                            else if (user.Status == UserStatus.Idle)
                            {
                                UsersBox.Items.Add(setupUserItem(user, Brushes.Orange, new int[] { 2 }));
                            }
                            else if (user.Status == UserStatus.Online)
                            {
                                UsersBox.Items.Add(setupUserItem(user, defaultBrush, new int[] { 2 }));
                            }
                            else if (user.Status == UserStatus.DoNotDisturb)
                            {
                                UsersBox.Items.Add(setupUserItem(user, Brushes.Red, new int[] { 2 }));
                            }
                            else if (user.Status == UserStatus.AFK)
                            {
                                UsersBox.Items.Add(setupUserItem(user, Brushes.SteelBlue, new int[] { 2 }));
                            }
                            else if (user.Status == UserStatus.Invisible)
                            {
                                UsersBox.Items.Add(setupUserItem(user, Brushes.MediumPurple, new int[] { 2 }));
                            }
                        }
                    }
                }
            }));

            return users.Count();
        }

        // Refresh users list box if someone has changed online status in the selected guild.
        private Task GuildUserUpdated(SocketUser arg1, SocketUser arg2)
        {
            if (selectedGuild != null)
            {
                var user = selectedGuild.GetUser(arg1.Id);

                if (user != null && arg1.Status != arg2.Status)
                {
                    numUsers = refreshUsers();
                } 
            }

            return Task.CompletedTask;
        }

        private async Task UserJoinedAsync(SocketGuildUser arg)
        {
            await arg.SendMessageAsync("Welcome, " + arg.Username + "!\nType '!help' for a list of available commands.");
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            var deleted = await arg1.GetOrDownloadAsync() as SocketMessage;

            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    ConsoleBox.Items.Add(setupItemContext("Deleted: " + deleted.Author.Username + ": " + deleted.Content, Brushes.Orange, new int[] { 1 }));
                }
                catch (Exception m)
                {
                    ConsoleBox.Items.Add(m.Message);
                }
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

            // Record to console and/or text file if not bot.
            if (!message.Author.IsBot || (message.Author.IsBot && ignoreBots == false) /*|| message.Content.Contains(errorMsg)*/)
            {
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (message.Author.IsBot)
                        ConsoleBox.Items.Add(setupItemContext(message, (Brush)new BrushConverter().ConvertFromString("#9b59b6"), new int[] { 1, 2 }));
                    else
                        ConsoleBox.Items.Add(setupItemContext(message, Brushes.White, new int[] { 1, 2 }));
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        if (message.Attachments.Count > 0)
                            file.WriteLine(message.Author.Username + ": [attachment] " + message.Content);
                        else
                            file.WriteLine(message.Author.Username + ": " + message.Content);

                        file.Close();
                    }
                }
            }

            // Split message by spaces once here so we can use all of it's parts easier and more efficiently.
            string[] splitMessage = message.Content.ToLower().Split(' ');

            // If a command was used, it will be the fist string of the message.
            string command = splitMessage[0];
            bool isCommand = false;

            if (command[0] == '!' && !message.Author.IsBot)
                isCommand = true;

            // Display help menu.
            if (isCommand == true && command == "!help")
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
                "!starbound - starbound server status.\n"+
                "!pubg [player name] [mode] - Check the ranking of a player an a given game mode.\n\n" +
                "https://github.com/rex706/Cerberus");
            }

            // Give all users 'member' role.
            else if (isCommand == true && command == "!member")
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
                         ConsoleBox.Items.Add(setupItemContext("Member role given to all users!", defaultBrush, new int[] { 1 }));
                     }));
                }
            }

            // Toggle spam control mode.
            else if (isCommand == true && command == "!spam")
            {
                int permission = await Utils.CheckPermissionAsync(threefourteen, message.Author);

                if (permission == 1)
                {
                    if (antiSpam == false)
                    {
                        await message.Channel.SendMessageAsync("Spam control has been **enabled**.");

                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            antiSpam = true;
                            ConsoleBox.Items.Add(setupItemContext(message.Author.ToString() + " has enabled spam control.", defaultBrush, new int[] { 1 }));
                        }));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Spam control has been **disabled**.");

                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            antiSpam = false;
                            ConsoleBox.Items.Add(setupItemContext(message.Author.ToString() + " has disabled spam control.", defaultBrush, new int[] { 1 }));
                        }));
                    }
                }
            }

            // Check for blacklisted users.
            else if (isCommand == true && message.Content == "!blacklist")
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
            else if (isCommand == true && command == "!blacklist" && message.Content.Length > 14)
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
                            ConsoleBox.Items.Add(setupItemContext(user.ToString() + " has been blacklisted from Cerberus by " + message.Author.Username, Brushes.Orange, new int[] { 1 }));
                        }));
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("You do not have permission to use that command!");
                }
            }

            else if (isCommand == true && message.Content == "!dayz")
            {
                await message.Channel.SendMessageAsync("The current DayZ HQ is located at: **035 059** on public official server **DayZ IL 2-1 (Public/Veteran) - Hosted by GameServers.com**");
            }

            else if (isCommand == true && message.Content == "!tits")
            {
                using (var httpclient = new System.Net.Http.HttpClient())
                {
                    using (var stream = await httpclient.GetStreamAsync("https://upload.wikimedia.org/wikipedia/commons/8/86/GreatTit002.jpg"))
                        await message.Channel.SendFileAsync(stream, "image.png", "A nice pair of natural tits!");
                }
            }

            else if (isCommand == true && command == "!gamescom")
            {
                await SteamMarketScrape.GetGamescomInfoAsync(message.Channel, true);
            }

            //else if (isCommand == true && command == "!pricecheck" && splitMessage.Length > 1)
            //{
            //    StringBuilder builder = new StringBuilder();

            //    for(int i = 1; i < splitMessage.Length; i++)
            //    {
            //        if (i == splitMessage.Length - 1)
            //            builder.Append(splitMessage[i]);
            //        else
            //            builder.Append(splitMessage[i]).Append("+");
            //    }

            //    string[] info = Utils.SteamMarketListingInfo(builder.ToString());

            //    await message.Channel.SendMessageAsync(info[1] + " " + info[0] + "'s starting at " + info[2] + "\n" + info[3]);
            //}

            // Ping IP addresses with the given ports and see if it is accepting connections.
            else if (isCommand == true && command == "!ping")
            {
                // TODO:
                // Check if game server user wants to ping exists before pinging server(s). 

                var settingsFile = new IniFile("settings.ini");
                string s = settingsFile.Read("Servers", "Settings");

                // Load custom server information if there is any. 
                var servers = Utils.ParseServers(s);

                // User probably entered an IP address with a port
                if (splitMessage[1].Contains(":"))
                {
                    if (servers.Count > 0)
                    {
                        foreach (var server in servers)
                        {
                            if (splitMessage[1].Contains(server.IP))
                            {
                                //Utils.PingServer(server);
                            }
                        }
                    }
                    else
                    {

                    }
                }
                // else look up the name in the list and use that IP.
                else
                {
                    
                }
                
            }

            // Strip user roles and send them to jail. (mod only)
            else if (isCommand == true && command == "!jail")
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
                        ConsoleBox.Items.Add(setupItemContext(tojail + " has been jailed by " + message.Author + " for " + time + " " + length + "!", Brushes.Orange, new int[] { 1 }));
                    }));

                    await jailTextChannel.SendMessageAsync("Welcome to jail " + tojail.Mention + "!\n\nYour sentence is: " + time + " " + length + ".");
                }
            }

            // Make sure only one vote can be in progress at a time.
            else if (isCommand == true && command == "!kick" && voteKickInProgress == true)
            {
                await message.Channel.SendMessageAsync("Another vote is in progress! Please try again after voting has finished.");
            }

            // Vote to kick.
            else if (isCommand == true && command == "!kick" && voteKickInProgress == false)
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
                         ConsoleBox.Items.Add(setupItemContext(message.Author.Username + " initiated vote to kick " + tokick.Username + ".", defaultBrush, new int[] { 1 }));
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
            else if (isCommand == true && command == "!yes" && kickTimerRunning == true && !votedUsers.Contains(message.Author.Username))
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
                             ConsoleBox.Items.Add(setupItemContext("Kicking " + tokick.Username + "...", Brushes.Red, new int[] { 1 }));
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

            // Get PUBG rank in specified game mode.
            else if (isCommand == true && command == "!pubg")
            {
                string[] phrase = message.Content.Split(' ');

                if (phrase.Length < 3)
                {
                    await message.Channel.SendMessageAsync("Not enough arguments.");
                }

                string playerName = phrase[1];
                string gameModeInput1 = phrase[2].ToLower();
                string gameModeInput2 = null;

                if (phrase.Length > 3)
                {
                     gameModeInput2 = phrase[3].ToLower();
                }

                int gameMode = -1;

                if (gameModeInput1.Contains("solo") || (gameModeInput2 != null && gameModeInput2.Contains("solo")))
                {
                    if (gameModeInput1.Contains("fpp") || (gameModeInput2 != null && gameModeInput2.Contains("fpp")))
                    {
                        gameMode = 3;
                        gameModeInput1 = "solo fpp";
                    }
                    else
                    {
                        gameMode = 0;
                        gameModeInput1 = "solo";
                    }
                    
                }
                else if (gameModeInput1.Contains("duo") || (gameModeInput2 != null && gameModeInput2.Contains("duo")))
                {
                    if (gameModeInput1.Contains("fpp") || (gameModeInput2 != null && gameModeInput2.Contains("fpp")))
                    {
                        gameMode = 4;
                        gameModeInput1 = "duo fpp";
                    }
                    else
                    {
                        gameMode = 1;
                        gameModeInput1 = "duo";
                    }
                    
                }
                else if (gameModeInput1 == "squad" || (gameModeInput2 != null && gameModeInput2.Contains("squad")))
                {
                    if (gameModeInput1.Contains("fpp") || (gameModeInput2 != null && gameModeInput2.Contains("fpp")))
                    {
                        gameMode = 5;
                        gameModeInput1 = "squad fpp";
                    }
                    else
                    {
                        gameMode = 2;
                        gameModeInput1 = "squad";
                    }
                }

                if (gameMode == -1)
                {
                    await message.Channel.SendMessageAsync("Invalid game mode.");
                    return;
                }

                int rank = await PUBGTracker.getRankAsync(playerName, gameMode);

                if (rank == -1)
                {
                    await message.Channel.SendMessageAsync("Player not found.");
                }
                else if (rank == -2)
                {
                    await message.Channel.SendMessageAsync("Player has no stats.");
                }
                else if (rank == -3)
                {
                    await message.Channel.SendMessageAsync("Player has no rank in specified game mode.");
                }
                else
                {
                    await message.Channel.SendMessageAsync(playerName + "'s PUBG *" + gameModeInput1 + "* rank: **#" + rank.ToString() + "**");
                }
            }

            // Random image from search phrase
            else if (isCommand == true && command == "!find")
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

                // Override safe search for nsfw channel.
                if (message.Channel.IsNsfw == true)
                {
                    html = Utils.GetSearchHtmlCode(query.ToString(), false);
                }
                else
                {
                    html = Utils.GetSearchHtmlCode(query.ToString(), safeSearch);
                }

                List<string> urls = Utils.GetSearchResultUrls(html);
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
                     ConsoleBox.Items.Add(setupItemContext(message.Author.Username + " queried '" + query + "' in #" + message.Channel.Name + "\n" + luckyUrl, Brushes.Khaki, new int[] { 1 }));
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

        private ListBoxItem itemContext(ListBoxItem item, DockPanel itemContentPanel, string text, Brush textColor, int[] flags)
        {
            TextBlock itemText = new TextBlock();

            itemText.TextWrapping = TextWrapping.Wrap;
            itemText.Foreground = textColor;
            itemText.Text = text;

            itemContentPanel.Children.Add(itemText);

            item.Content = itemContentPanel;
            item.MaxWidth = ConsoleBox.Width;

            // Create new context menu and menu items.
            ContextMenu itemContext = new ContextMenu();
            MenuItem timeStamp = new MenuItem();
            MenuItem copy = new MenuItem();
            MenuItem delete = new MenuItem();

            timeStamp.Header = DateTime.Now;
            timeStamp.IsEnabled = false;

            copy.Header = "Copy";
            copy.Click += Copy_Click;

            delete.Header = "Delete";
            delete.Click += Delete_Click;

            itemContext.Items.Add(timeStamp);

            foreach (int flag in flags)
            {
                switch (flag)
                {
                    case 1:
                        itemContext.Items.Add(copy);
                        break;

                    case 2:
                        itemContext.Items.Add(delete);
                        break;

                    default:
                        break;
                }
            }

            // Add context menu to item.
            item.ContextMenu = itemContext;

            return item;
        }

        private ListBoxItem setupItemContext(SocketMessage message, Brush textColor, int[] flags)
        {
            ListBoxItem item = new ListBoxItem();
            DockPanel itemContentPanel = new DockPanel();
            TextBlock itemOrigin = new TextBlock();

            item.Tag = message;

            itemOrigin.Foreground = textColor;

            SocketGuildChannel c = message.Channel as SocketGuildChannel;
            SocketDMChannel dm = message.Channel as SocketDMChannel;

            if (c == null && dm != null)
            {
                itemOrigin.Text = "[DM] " + message.Author + ": ";
            }
            else if (c == null && dm == null)
            {
                // error
            }
            else
            {
                itemOrigin.Text = "[" + c.Guild.Name + " | " + c.Name + "] " + message.Author.Username + ": ";
            }

            itemContentPanel.Children.Add(itemOrigin);

            return itemContext(item, itemContentPanel, message.ToString(), defaultBrush, flags);
        }

        private ListBoxItem setupItemContext(string origin, string text, Brush textColor, int[] flags)
        {
            ListBoxItem item = new ListBoxItem();
            DockPanel itemContentPanel = new DockPanel();
            TextBlock itemOrigin = new TextBlock();

            itemOrigin.Foreground = textColor;
            itemOrigin.Text = origin;

            itemContentPanel.Children.Add(itemOrigin);

            return itemContext(item, itemContentPanel, text, defaultBrush, flags);
        }

        private ListBoxItem setupItemContext(string text, Brush textColor, int[] flags)
        {
            // Create new listbox item and assign text value.
            ListBoxItem item = new ListBoxItem();
            DockPanel itemContentPanel = new DockPanel();

            return itemContext(item, itemContentPanel, text, textColor, flags);
        }

        private ListBoxItem setupUserItem(SocketGuildUser user, Brush textColor, int[] flags)
        {
            // Create new listbox item and assign text value.
            ListBoxItem item = new ListBoxItem();
            item.Content = user;
            item.Tag = user;

            // Create new context menu and menu items.
            ContextMenu userContext = new ContextMenu();

            foreach (int flag in flags)
            {
                switch (flag)
                {
                    case 1:
                        MenuItem copy = new MenuItem();

                        copy.Header = "Copy";
                        copy.Click += Copy_Click;

                        userContext.Items.Add(copy);
                        break;

                    case 2:
                        MenuItem status = new MenuItem();
                        MenuItem playing = new MenuItem();

                        status.Header = user.Status.ToString();
                        status.IsEnabled = false;

                        playing.Header = user.Game.ToString();
                        playing.IsEnabled = false;

                        userContext.Items.Add(status);

                        if (user.Game.HasValue == true)
                        {
                            userContext.Items.Add(playing);
                        }

                        item.ContextMenu = userContext;
                        break;

                    default:
                        break;
                }
            }
           
            item.Foreground = textColor;

            return item;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            // Get the timestamp and text from the selected item.
            ListBoxItem item = (ListBoxItem)ConsoleBox.ItemContainerGenerator.ContainerFromItem(ConsoleBox.SelectedItem);

            var dp = item.Content as DockPanel;
            TextBlock otb = dp.Children[0] as TextBlock;
            TextBlock ctb = dp.Children[1] as TextBlock;

            string timeStamp = item.ContextMenu.Items[0].ToString();
            int start = timeStamp.IndexOf(":") + 1;
            int end = timeStamp.IndexOf("Items");
            int length = end - start;
            timeStamp = timeStamp.Substring(start, length);

            // Copy contents of the selected listbox item to the clipboard.
            Clipboard.SetText(timeStamp + " " + otb.Text + ctb.Text);
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            // Delete message associated with selected console item.
            ListBoxItem item = (ListBoxItem)ConsoleBox.ItemContainerGenerator.ContainerFromItem(ConsoleBox.SelectedItem);
            SocketMessage m = item.Tag as SocketMessage;
            await m.DeleteAsync();
            ConsoleBox.Items.Remove(item);
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
                    ConsoleBox.Items.Add(setupItemContext("Vote to kick " + tokick.Username + " failed.", Brushes.Yellow, new int[] { 1 }));
                }));

                lastchannel.SendMessageAsync("Kick failed. Not enough users voted.");
            }

            kickTimerRunning = false;
            voteKickInProgress = false;
            votedUsers = null;
        }

        // Ping servers after timer has elapsed
        public static void autoPingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Utils.ServerStatus(ServerIP, minecraftChannel);
        }

        public static void spamTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Dequeue user from userSpamQueue and message from messageSpamQueue
            if (userSpamQueue.Count > 0)
                userSpamQueue.RemoveLast();

            if (messageSpamQueue.Count > 0)
                messageSpamQueue.RemoveLast();
        }

        private async void GamescomTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            // Generate random time between 25-35 minutes to be a little more unpredictable.
            Random random = new Random();
            int randInt = random.Next(1500000, 2100000);
            gamescomTimer.Interval = randInt;
            await SteamMarketScrape.GetGamescomInfoAsync(threefourteen.DefaultChannel, false);
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
            // Send text from text box to Discord as a message from Cerberus.
            if (selectedUser != null)
            {
                selectedUser.SendMessageAsync(InputTextBox.Text);
            }
            else if (selectedChannel != null)
            {
                (selectedChannel as SocketTextChannel)?.SendMessageAsync(InputTextBox.Text);
            }
            else
            {
                return;
            }

            // Reset input text box.
            InputTextBox.Clear();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Show();
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

                MessageRecieverTextBox.Visibility = Visibility.Visible;
                MessageRecieverTextBox.Clear();
                MessageRecieverTextBox.Text = selectedGuild.Name + " | " + selectedGuild.DefaultChannel.ToString();

                refreshChannels();
                numUsers = refreshUsers();
            }
            else
            {
                MessageRecieverTextBox.Visibility = Visibility.Collapsed;
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

                MessageRecieverTextBox.Visibility = Visibility.Visible;
                MessageRecieverTextBox.Clear();
                MessageRecieverTextBox.Text = selectedGuild.Name + " | " + selectedChannel.ToString();
            }
        }

        private void UsersBox_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = null;

            if (UsersBox.SelectedIndex != -1)
            {
                ChannelsBox.SelectedIndex = -1;
                selectedChannel = null;
                ListBoxItem item = UsersBox.SelectedItem as ListBoxItem;
                selectedUser = item.Tag as SocketGuildUser;

                MessageRecieverTextBox.Visibility = Visibility.Visible;
                MessageRecieverTextBox.Clear();
                MessageRecieverTextBox.Text = selectedGuild.Name + " | " + selectedUser.ToString();
            }
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
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

        private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            InputTextBox.CaretIndex = Int32.MaxValue;
        }

        private void GuildsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            refreshGuilds(); 
        }

        private void ChannelsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GuildsBox.SelectedIndex > -1)
            {
                refreshChannels();
            }
        }

        private void UsersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GuildsBox.SelectedIndex > -1)
            {
                refreshUsers();
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (client.ConnectionState == ConnectionState.Disconnected)
            {
                LoginClient();
            }
            else
            {
                ConsoleBox.Items.Add(setupItemContext("Client is currently " + client.ConnectionState.ToString().ToLower() + ".", Brushes.Khaki, new int[] {}));
            }
        }

        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (client.ConnectionState == ConnectionState.Connected)
            {
                await client.LogoutAsync();

                UsersBox.Items.Clear();
                ChannelsBox.Items.Clear();
                GuildsBox.Items.Clear();
                MessageRecieverTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                ConsoleBox.Items.Add(setupItemContext("Client is currently " + client.ConnectionState.ToString().ToLower() + ".", Brushes.Khaki, new int[] {}));
            }
        }

        private async void StatusOnline_Click(object sender, RoutedEventArgs e)
        {
            await client.SetStatusAsync(UserStatus.Online);
        }

        private async void StatusIdle_Click(object sender, RoutedEventArgs e)
        {
            await client.SetStatusAsync(UserStatus.Idle);
        }

        private async void StatusBusy_Click(object sender, RoutedEventArgs e)
        {
            await client.SetStatusAsync(UserStatus.DoNotDisturb);
        }
    }
}
