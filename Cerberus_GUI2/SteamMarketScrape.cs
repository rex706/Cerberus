using Discord.WebSocket;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Timers;

namespace Cerberus_GUI2
{
    class SteamMarketScrape
    {
        private static  System.Windows.Forms.WebBrowser browser;

        public static async Task GetGamescomInfoAsync(ISocketMessageChannel channel, bool respond)
        {
            try
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    browser = new System.Windows.Forms.WebBrowser();
                    browser.ScriptErrorsSuppressed = true;
                    //browser.

                    // Create timer to wait for scripts and other objects on the website to load fully before parsing the html.
                    Timer timer = new Timer(2000);
                    timer.Elapsed += (sender, e) => GamescomTimer_ElapsedAsync(sender, e, channel, respond);

                    browser.Navigate("http://steamcommunity.com/market/listings/578080/GAMESCOM%20INVITATIONAL%20CRATE");
                    timer.Start();
                });
            }
            catch (Exception m)
            {
               await channel.SendMessageAsync(m.Message);
            }
        }

        private static async void GamescomTimer_ElapsedAsync(object sender, ElapsedEventArgs e, ISocketMessageChannel channel, bool respond)
        {
            (sender as Timer).Stop();

            GamescomScrape(channel, respond);
        }

        private static async void GamescomScrape(ISocketMessageChannel channel, bool respond)
        {
            // Finished loading page.
            HtmlNode amountNode = null;
            HtmlNode priceNode = null;
            var doc = new HtmlDocument();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                var dd = (mshtml.IHTMLDocument2)browser.Document.DomDocument;
                StringReader sr = new StringReader(dd.body.parentElement.outerHTML);
                doc.Load(sr);

                // If these fail, make sure IE11 is being used by updating the registry. https://stackoverflow.com/a/38514446
                amountNode = doc.DocumentNode.SelectSingleNode("//*[@id='market_commodity_forsale']/span[1]");
                priceNode = doc.DocumentNode.SelectSingleNode("//*[@id='market_commodity_forsale']/span[2]");
            });

            if (amountNode == null || priceNode == null)
            {
                //await channel.SendMessageAsync("null");
                return;
            }

            var settingsFile = new IniFile("settings.ini");

            if (Double.Parse(priceNode.InnerText.Substring(1)) > 10 && settingsFile.KeyExists("Gamescom", "Settings") && settingsFile.Read("Gamescom", "Settings") == "True")
            {
                settingsFile.Write("False", "Gamescom", "Settings");
                await channel.SendMessageAsync(":rotating_light: :rotating_light: :rotating_light: ALERT: GAMESCOM CRATES STARTING AT **" + priceNode.InnerText + "** :rotating_light: :rotating_light: :rotating_light: ");
            }

            if (respond == true)
            {
                await channel.SendMessageAsync("There are **" + amountNode.InnerText + "** PUBG Gamescom Invitational Crates for sale starting at **" + priceNode.InnerText + "** as of " + DateTime.Now + " EST");
            }
        }

        public static string[] SteamMarketListingInfo(string query)
        {
            string url = "http://steamcommunity.com/market/search?q=" + query;

            string[] s = new string[4];

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                string itemUrl = null;
                string itemName = null;
                HtmlNode amountNode = null;
                HtmlNode priceNode = null;
                System.Windows.Forms.WebBrowser browser = new System.Windows.Forms.WebBrowser();

                browser.Navigate(url);

                while (itemUrl == null || itemName == null)
                {
                    while (browser.ReadyState != System.Windows.Forms.WebBrowserReadyState.Complete)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }

                    var doc = new HtmlDocument();
                    var docAsIHtmlDoc3 = (mshtml.IHTMLDocument3)browser.Document.DomDocument;
                    StringReader sr = new StringReader(docAsIHtmlDoc3.documentElement.outerHTML);
                    doc.Load(sr);

                    itemUrl = doc.DocumentNode.SelectSingleNode("//*[@id='resultlink_0']").Attributes["href"].Value;
                    itemName = doc.DocumentNode.SelectSingleNode("//*[@id='result_0_name']").InnerText;
                }

                browser.Navigate(itemUrl);

                while (amountNode == null || priceNode == null)
                {
                    while (browser.ReadyState != System.Windows.Forms.WebBrowserReadyState.Complete)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }

                    var doc = new HtmlDocument();
                    var docAsIHtmlDoc3 = (mshtml.IHTMLDocument3)browser.Document.DomDocument;
                    var sr = new StringReader(docAsIHtmlDoc3.documentElement.outerHTML);
                    doc.Load(sr);

                    amountNode = doc.DocumentNode.SelectSingleNode("//*[@id='market_commodity_forsale']/span[1]");
                    priceNode = doc.DocumentNode.SelectSingleNode("//*[@id='market_commodity_forsale']/span[2]");
                }

                s[0] = itemName;
                s[1] = amountNode.InnerText;
                s[2] = priceNode.InnerText;
                s[3] = itemUrl;
            });

            return s;
        }
    }
}
