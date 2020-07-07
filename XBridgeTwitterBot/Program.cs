using System;
using Tweetinvi;
using System.Timers;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using System.Dynamic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Web.Http;
using System.Net;
using Discord;
using Discord.WebSocket;

namespace XBridgeTwitterBot
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private DiscordSocketClient _discordSocketClient;

        const double interval = 1000 * 60 * 60 * 24;

        IOptions<DiscordSettings> discordSettings;
        public static void Main(string[] args)
                => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            var hostBuilder = new HostBuilder();

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environmentName}.json", true)
                .AddEnvironmentVariables()
                .Build();

            services.AddOptions();
            services.Configure<ApiSettings>(configuration.GetSection("Api"));
            services.Configure<TwitterSettings>(configuration.GetSection("Twitter"));
            services.Configure<DiscordSettings>(configuration.GetSection("Discord"));

            var serviceProvider = services.BuildServiceProvider();

            var twitterSettings = serviceProvider
                .GetService<IOptions<TwitterSettings>>();

            Auth.SetUserCredentials(
                twitterSettings.Value.ConsumerKey,
                twitterSettings.Value.ConsumerSecret,
                twitterSettings.Value.UserAccessToken,
                twitterSettings.Value.UserAccessSecret
            );

            discordSettings = serviceProvider.GetService<IOptions<DiscordSettings>>();

            _discordSocketClient = new DiscordSocketClient();
            _discordSocketClient.Log += Log;


            await _discordSocketClient.LoginAsync(TokenType.Bot, discordSettings.Value.Token);

            await _discordSocketClient.StartAsync();

            var apiSettings = serviceProvider
                .GetService<IOptions<ApiSettings>>();

            _httpClient.BaseAddress = new Uri(apiSettings.Value.BaseAddress + "/api/dx/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("BlockDX Twitter Bot Running...");

            try
            {
                await Publish();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            Timer checkForTime = new Timer(interval);
            checkForTime.Elapsed += new ElapsedEventHandler(CheckForTime_Elapsed);
            checkForTime.Enabled = true;

            await hostBuilder.RunConsoleAsync();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async void CheckForTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await Publish();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private async Task Publish()
        {
            var tweetText = await ComposeParentTweet();
            if (!string.IsNullOrEmpty(tweetText))
            {
                Console.WriteLine(tweetText);
                var childrenTweets = await ComposeChildrenTweets();
                childrenTweets.ForEach(ct => Console.WriteLine(ct));
                var parentTweet = Tweet.PublishTweet(tweetText);

                Tweetinvi.Models.ITweet prevTweet = parentTweet;
                Tweetinvi.Models.ITweet currTweet;
                foreach (var childTweet in childrenTweets)
                {
                    currTweet = Tweet.PublishTweetInReplyTo(childTweet, prevTweet);
                    prevTweet = currTweet;
                };

                var discordChannel = _discordSocketClient.GetChannel(discordSettings.Value.ChannelId) as IMessageChannel;
                await discordChannel.SendMessageAsync(parentTweet.Url);
            }
            
        }

        private static async Task<List<string>> ComposeChildrenTweets()
        {
            var totalVolumePerTradedCoinResponse = await _httpClient.GetAsync("GetOneDayTotalVolumePerCoin?units=BLOCK&units=BTC&units=USD");

            string totalVolumePerTradedCoinResult = await totalVolumePerTradedCoinResponse.Content.ReadAsStringAsync();

            var childrenTweets = new List<string>();
            if (!string.IsNullOrEmpty(totalVolumePerTradedCoinResult))
            {
                var volumesPerCoin = JsonConvert.DeserializeObject<List<TokenTradeStatistics>>(totalVolumePerTradedCoinResult);

                foreach (var coinVolume in volumesPerCoin)
                {
                    string tweet = "Trading Volume $" + coinVolume.Token + ":"
                        + "\n";

                    foreach (var volume in coinVolume.Volumes.OrderByDescending(v => v.Unit))
                    {
                        string unit = "\n$";
                        if (volume.Unit.Equals("USD"))
                        {
                            unit += (volume.Unit + ": $" + volume.Volume.ToString("N3", CultureInfo.InvariantCulture));
                        }
                        else
                            unit += (volume.Unit + ": " + volume.Volume.ToString("N3", CultureInfo.InvariantCulture) + " " + volume.Unit);

                        tweet += unit;
                    }

                    tweet += "\n\nCompleted Orders: " + coinVolume.TradeCount;

                    childrenTweets.Add(tweet);
                }
            }
            return childrenTweets;
        }



        private static async Task<string> ComposeParentTweet()
        {
            var totalVolumeResponse = await _httpClient.GetAsync("GetOneDayTotalVolume?token=0&units=BLOCK&units=BTC&units=USD");

            string totalVolumeResult = await totalVolumeResponse.Content.ReadAsStringAsync();

            var volumes = JsonConvert.DeserializeObject<List<TokenVolume>>(totalVolumeResult);

            var totalTradeCountResponse = await _httpClient.GetAsync("GetOneDayTotalTradesCount");

            string totalTradeCountResult = await totalTradeCountResponse.Content.ReadAsStringAsync();

            var totalTradeCount = JsonConvert.DeserializeObject<int>(totalTradeCountResult);

            // Average Trade size?
            // Fees Collected?
            // Total number of trades

            string tweet = "24 Hour @BlockDXExchange Statistics (" + DateTime.Now.ToUniversalTime().ToString("MMMM d yyyy") + " UTC)"
                + "\n\nTotal Trading Volume:"
                + "\n";

            if (volumes?.Any() != true)
            {
                //tweet += "\n";
                //tweet += "$USD: $0.000\n";
                //tweet += "$BTC: 0.000 BTC\n";
                //tweet += "$BLOCK: 0.000 BLOCK";
                return string.Empty;
            } 
            else
            {
                foreach (var volume in volumes.OrderByDescending(v => v.Unit))
                {
                    string unit = "\n$";
                    if (volume.Unit.Equals("USD"))
                    {
                        unit += (volume.Unit + ": $" + volume.Volume.ToString("N3", CultureInfo.InvariantCulture));
                    }
                    else
                        unit += (volume.Unit + ": " + volume.Volume.ToString("N3", CultureInfo.InvariantCulture) + " " + volume.Unit);

                    tweet += unit;
                }
            }
            
            tweet += "\n\nNumber of Trades: " + totalTradeCount;
            

            return tweet;
        }

    }
}