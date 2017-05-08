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

        private static System.Timers.Timer kickTimer;
        private static bool kickTimerRunning = false;
        private static HashSet<string> votedUsers;

        private static System.Timers.Timer autoPingTimer;
        private static System.Timers.Timer spamTimer;

        private static int numUsers;
        private static int democracy;

        private static string[] kickMessage;
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

        private static SocketGuild selectedGuild;
        private static SocketChannel selectedChannel;
        private static SocketGuildUser selectedUser;
        private static RestDMChannel currentDM;
        private static SocketVoiceChannel jailChannel;

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

            ConsoleBox.Items.Clear();
            ConsoleBox.Items.Add("Creating Client");

            client = new DiscordSocketClient();

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
                string token = "MjA2OTU1MjcwMzIzMTc1NDI2.CzEqig.mX0SfHN3OqU4fjGsHx8nap59b0U";
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
            }
            catch (Exception m)
            {
                ConsoleBox.Items.Add(m.Message);
            }

            client.Ready += () =>
            {
                // Done!
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add("Client connected!");
                    ConsoleBox.Items.Add("--------------------");
                }));

                // Get text channels.
                threefourteen = client.GetGuild(97459030741508096);
                generalChannel = client.GetChannel(97459030741508096) as SocketTextChannel;
                minecraftChannel = client.GetChannel(206980643148529665) as SocketTextChannel;
                mod_chat = client.GetChannel(236670943387320333) as SocketTextChannel;
                devChannel = client.GetChannel(206951913789325312) as SocketTextChannel;
                jailChannel = client.GetChannel(207717697331527681) as SocketVoiceChannel;

                IEnumerable<SocketGuild> guilds = client.Guilds;

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    GuildsBox.Items.Clear();
                }));
                
                foreach (SocketGuild guild in guilds)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        GuildsBox.Items.Add(guild);
                    }));
                }

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
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add(arg1.Username + " joined " + arg3.VoiceChannel.Name + ".");
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
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            ConsoleBox.Items.Add("Added " + arg1.Username + " to HashSet");
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
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add(arg1.Username + " left.");
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
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add(arg1.Username + " went afk.");
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
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add(arg1.Username + " is no longer afk.");
                }));

                if (logChat)
                {
                    using (StreamWriter file = File.AppendText("chat_log.txt"))
                    {
                        file.WriteLine(arg1.Username + " is no longer afk.");
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task UserUpdated(SocketUser arg1, SocketUser arg2)
        {
            throw new NotImplementedException();

            // User was offline and came back online
            //(arg1.Status == UserStatus.Offline && arg2.Status == UserStatus.Online)
        }

        private async Task UserJoinedAsync(SocketGuildUser arg)
        {
            var DMChannel = await arg.CreateDMChannelAsync();
            await DMChannel.SendMessageAsync("Welcome, " + arg.Username + "!\nType '!help' for a list of available commands.");
            await DMChannel.CloseAsync();
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            var deleted = await arg1.GetOrDownloadAsync();

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                ConsoleBox.Items.Add("Deleted: " + deleted.Author.Username + ": " + deleted.Content);
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

                        var DMChannel = await message.Author.CreateDMChannelAsync();
                        await DMChannel.SendMessageAsync("Stop spamming!");
                        await DMChannel.CloseAsync();

                        spamTimer.Start();

                        return;
                    }
                }

                foreach (string s in userSpamQueue)
                {
                    if (s == message.Author.Username)
                    {
                        await message.DeleteAsync();

                        var DMChannel = await message.Author.CreateDMChannelAsync();
                        await DMChannel.SendMessageAsync("Stop spamming!");
                        await DMChannel.CloseAsync();

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
                var DMChannel = await message.Author.CreateDMChannelAsync();
                await DMChannel.SendMessageAsync("You have been black listed from using Cerberus commands. Sorry for the inconvenience!");
                await DMChannel.CloseAsync();
                return;
            }

            // Record to text file if not bot.
            if (!message.Author.IsBot || message.Content.Contains(errorMsg))
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (message.Attachments.Count > 0)
                        ConsoleBox.Items.Add(message.Author.Username + " [" + message.Channel.Name+ "]: [attachment] " + message.Content);
                    else
                        ConsoleBox.Items.Add(message.Author.Username + " [" + message.Channel.Name + "]: " + message.Content);
                }));

                if (logChat)
                {
                    if (!File.Exists("chat_log.txt"))
                    {
                        using (StreamWriter file = File.CreateText("chat_log.txt"))
                        {
                            if (message.Attachments.Count > 0)
                                file.WriteLine(message.Author.Username + " [" + message.Channel.Name + "]: [attachment] " + message.Content);
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
                "!find [search phrase] - random image from search phrase.\n\n" +
                "!kick [@mention] - initiate a vote to kick another user.\n" +
                "!yes - vote to kick user.\n\n" +
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
                int permission = await Utils.CheckPermissionAsync(threefourteen, message);
                
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

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        ConsoleBox.Items.Add("Member role given to all users!");
                    }));
                }
            }

            // Toggle spam control mode.
            if (!message.Author.IsBot && message.Content == "!spam")
            {
                int permission = await Utils.CheckPermissionAsync(threefourteen, message);

                if (permission == 1)
                {
                    if (antiSpam == false)
                    {
                        await message.Channel.SendMessageAsync("Spam control has been **enabled**.");

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            SpamControlCheckBox.IsChecked = true;
                            ConsoleBox.Items.Add(message.Author + " has enabled spam control.");
                        }));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Spam control has been **disabled**.");

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            SpamControlCheckBox.IsChecked = false;
                            ConsoleBox.Items.Add(message.Author + " has disabled spam control.");
                        }));
                    }
                }
            }

            // Check for blacklisted users.
            if (!message.Author.IsBot && message.Content == "!blacklist")
            {
                if (blackList.Count == 0)
                {
                    await message.Channel.SendMessageAsync("There are no currently blacklisted users.");
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
                int hasPermission = await Utils.CheckPermissionAsync(threefourteen, message);
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

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            ConsoleBox.Items.Add(user.ToString() + " has been blacklisted from Cerberus by " + message.Author.Username);
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
            if (!message.Author.IsBot && message.Content.Contains("!jail"))
            {
                int permission = await Utils.CheckPermissionAsync(threefourteen, message);
                var user = threefourteen.GetUser(message.MentionedUsers.First().Id);

                IEnumerable<IRole> userRoles = user.Roles;
                IEnumerable<IRole> guildRoles = user.Guild.Roles;

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
                            await user.RemoveRoleAsync(role);
                    }

                    // Apply the jail role to user being sent to jail.
                    await user.AddRoleAsync(jailrole);

                    // Move user to the jail voice channel.
                    await user.ModifyAsync(u => u.Channel = jailChannel);

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        ConsoleBox.Items.Add(user + " has been jailed by " + message.Author + "!");
                    }));
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

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        ConsoleBox.Items.Add(message.Author.Username + " initiated vote to kick " + tokick.Username + ".");
                    }));

                    voteKickInProgress = true;
                    lastchannel = message.Channel;
                    //numUsers =
                    democracy = 1; //(numUsers / 6);

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

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            ConsoleBox.Items.Add("Kicking " + tokick.Username + "...");
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
            if (!message.Author.IsBot && (message.Content.Contains("!find") || message.Content.Contains("!Find") || message.Content.Contains("!FIND") || message.Content.Contains("!search")))
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

                string html = Utils.GetHtmlCode(query.ToString(), safeSearch);
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

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add(message.Author.Username + " queried '" + query + "' in #" + message.Channel.Name + "\n" + luckyUrl);
                }));
                    
                using (var httpclient = new System.Net.Http.HttpClient())
                {
                    using (var stream = await httpclient.GetStreamAsync(luckyUrl))
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
                        
                }

                // Add message to messageSpamQueue.
                if (antiSpam && !message.Author.IsBot)
                {
                    string[] text = message.Content.Split(' ');

                    // If user is using the find or help command, ignore it. 
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
            };
        }

        // Vote to kick timer ended
        private void kickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            kickTimer.Stop();

            // Vote failed
            if (democracy > 0)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Add("Vote to kick " + tokick.Username + " failed.");
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
            if (currentDM != null)
            {
                currentDM.SendMessageAsync(s);
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
            ConsoleBox.Items.Add("Cerberus " + InputTextBox.Text);

            // Reset input text box.
            InputTextBox.Clear();

            if (currentDM != null)
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
            
            if (currentDM != null)
            {
                currentDM.CloseAsync();
                currentDM = null;
            }

            // Load new variables with selected content.
            selectedGuild = GuildsBox.SelectedItem as SocketGuild;

            InputTextBox.Clear();
            InputTextBox.Text = "[" + selectedGuild.DefaultChannel + "]: ";

            IEnumerable<SocketGuildChannel> channels = selectedGuild.Channels;
            IEnumerable<SocketGuildUser> users = selectedGuild.Users;

            // Load the channel box with channels.
            foreach (SocketGuildChannel channel in channels)
            {
                var ch = channel as SocketTextChannel;

                if (ch != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        ChannelsBox.Items.Add(channel);
                    }));
                }

            }

            // Load the user box with users.
            foreach (SocketGuildUser user in users)
            {
                if (!user.IsBot && user.Status != UserStatus.Offline)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        UsersBox.Items.Add(user);
                    }));
                }
            }
        }

        private async void ChannelsBox_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelsBox.SelectedIndex != -1)
            {
                if (currentDM != null)
                {
                    await currentDM.CloseAsync();
                    currentDM = null;
                }
                
                UsersBox.SelectedIndex = -1;
                selectedChannel = ChannelsBox.SelectedItem as SocketTextChannel;

                InputTextBox.Clear();
                InputTextBox.Text = "[" + selectedChannel + "]: ";
            }
        }

        private async void UsersBox_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            if (UsersBox.SelectedIndex != -1)
            {
                if (currentDM != null)
                {
                    await currentDM.CloseAsync();
                    currentDM = null;
                }

                ChannelsBox.SelectedIndex = -1;
                selectedUser = UsersBox.SelectedItem as SocketGuildUser;
                currentDM = await selectedUser.CreateDMChannelAsync();

                InputTextBox.Clear();
                InputTextBox.Text = "[" + selectedUser + "]: ";
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to clear the console?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ConsoleBox.Items.Clear();
                }));
            }
        }
    }
}
