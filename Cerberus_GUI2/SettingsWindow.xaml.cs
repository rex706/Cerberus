using System;
using System.Timers;
using System.Windows;

namespace Cerberus_GUI2
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static IniFile settingsFile;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            settingsFile = new IniFile("settings.ini");
            
            if (settingsFile.KeyExists("Token", "Settings"))
            {
                TokenTextBox.Text = settingsFile.Read("Token", "Settings");
            }
            if (settingsFile.KeyExists("Servers", "Settings"))
            {
                IPTextBox.Text = settingsFile.Read("Servers", "Settings");
            }

            LogChatCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("Log Chat", "Settings"));
            LogUsersCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("Log Users", "Settings"));
            PingServersCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("Ping Servers", "Settings"));
            SafeSearchCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("Safe Search", "Settings"));
            SpamControlTextBox.IsChecked = Convert.ToBoolean(settingsFile.Read("Spam Control", "Settings"));
            IgnoreBotsCheckBox.IsChecked = Convert.ToBoolean(settingsFile.Read("Ignore Bots", "Settings"));
        }

        private void Save_Token_Click(object sender, RoutedEventArgs e)
        {
            if (TokenTextBox.Text.Length > 5)
            {
                settingsFile.Write("Token", TokenTextBox.Text, "Settings");
                MessageBox.Show("Token saved!\nIf this it not the first time, please log out and back in.");
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid Token");
            }
        }

        private void LogChatCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.logChat = true;
            settingsFile.Write("Log Chat", "True", "Settings");
        }

        private void LogChatCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.logChat = false;
            settingsFile.Write("Log Chat", "False", "Settings");
        }

        private void LogUsersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.logUsers = true;
            settingsFile.Write("Log Users", "True", "Settings");
        }

        private void LogUsersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.logUsers = false;
            settingsFile.Write("Log Users", "False", "Settings");
        }

        private void PingServersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (IPTextBox.Text.Length > 0)
            {
                MainWindow.serverPing = true;
                settingsFile.Write("Ping Servers", "True", "Settings");
                settingsFile.Write("Servers", IPTextBox.Text, "Settings");

                MainWindow.autoPingTimer = new System.Timers.Timer(1800000); //600000ms = 10 min, 1200000 = 20 min, 1800000 = 30 min, 3600000 = 1 hr
                MainWindow.autoPingTimer.Elapsed += new ElapsedEventHandler(MainWindow.autoPingTimer_Elapsed);
                MainWindow.autoPingTimer.Start();

                string[] servers = IPTextBox.Text.Split('\n');

                
                //Utils.PingServers(Utils.ParseServers(IPTextBox.Text));
            }
            else
            {
                PingServersCheckBox.IsChecked = false;
            }
        }

        private void PingServersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.serverPing = false;
            settingsFile.Write("Ping Servers", "False", "Settings");

            MainWindow.autoPingTimer.Stop();
        }

        private void SafeSearchCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.safeSearch = true;
            settingsFile.Write("Safe Search", "True", "Settings");
        }

        private void SafeSearchCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.safeSearch = false;
            settingsFile.Write("Safe Search", "False", "Settings");
        }

        private void SpamControlTextBox_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.antiSpam = true;
            settingsFile.Write("Spam Control", "False", "Settings");

            MainWindow.spamTimer = new System.Timers.Timer(2000); //2000ms = 2s
            MainWindow.spamTimer.Elapsed += new ElapsedEventHandler(MainWindow.spamTimer_Elapsed);
            MainWindow.spamTimer.Start();
        }

        private void SpamControlTextBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.antiSpam = false;
            settingsFile.Write("Spam Control", "False", "Settings");
            MainWindow.spamTimer.Stop();
        }

        private void SaveServers_Click(object sender, RoutedEventArgs e)
        {
            settingsFile.Write("Servers", IPTextBox.Text, "Settings");
        }

        private void IgnoreBotsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.ignoreBots = true;
            settingsFile.Write("Ignore Bots", "True", "Settings");
        }

        private void IgnoreBotsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.ignoreBots = false;
            settingsFile.Write("Ignore Bots", "False", "Settings");
        }
    }
}