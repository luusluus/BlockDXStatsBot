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

namespace XBridgeTwitterBot
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        const double interval = 1000 * 60 * 60 * 24;
        //const double interval = 10000;
        static async Task Main(string[] args)
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

            var serviceProvider = services.BuildServiceProvider();

            var apiSettings = serviceProvider
                .GetService<IOptions<ApiSettings>>();

            var twitterSettings = serviceProvider
                .GetService<IOptions<TwitterSettings>>();

            Auth.SetUserCredentials(
                twitterSettings.Value.ConsumerKey,
                twitterSettings.Value.ConsumerSecret,
                twitterSettings.Value.UserAccessToken,
                twitterSettings.Value.UserAccessSecret
            );

            client.BaseAddress = new Uri(apiSettings.Value.BaseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("BlockDX Twitter Bot Running...");

            try
            {
                var tweet = await ComposeTweet();
                //Tweet.PublishTweet(tweet);
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

        private static async void CheckForTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var tweet = await ComposeTweet();
                Tweet.PublishTweet(tweet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private static async Task<string> ComposeTweet()
        {
            string baseUrl = "https://raw.githubusercontent.com/blocknetdx/blockchain-configuration-files/master/manifest-latest.json";

            var getAssetsTask = client.GetStringAsync(baseUrl);

            var assetWhiteList = JsonConvert.DeserializeObject<List<Asset>>(await getAssetsTask);

            var request = new XCloudServiceRequest
            {
                Service = "xrs::dxGet24hrTradeSummary",
                Parameters = new object[] { "0" },
                NodeCount = 1
            };
            var tradeSummaryResponse = await ServiceAsync<XCloudServiceResponse<List<Dictionary<string, DXTradePair>>>>(request);

            if (tradeSummaryResponse.Reply == null)
            {
                var responseMessage = new HttpResponseMessage();
                responseMessage.Content = new StringContent("Error at XCloud Service " + request.Service + ". Parameters: " + request.Parameters + ".");
                responseMessage.StatusCode = HttpStatusCode.InternalServerError;
                throw new HttpResponseException(responseMessage);
            }
            var trades = tradeSummaryResponse.Reply.First()
                .Where(p => 
                {
                    var tradedAssets = p.Key.Split("-").ToList();
                    var result = tradedAssets.All(ta => assetWhiteList.Select(awl => awl.Ticker).Contains(ta));
                    return result;
                })
                .ToDictionary(p => p.Key, p => p.Value);
            
            var coins = new HashSet<string>();
            foreach (var item in trades.Keys.ToList())
            {
                coins.Add(item.Split('-')[0]);
            }

            request = new XCloudServiceRequest
            {
                Service = "xrs::dxGet24hrTradeHistory",
                Parameters = new object[] { "0" },
                NodeCount = 1
            };

            var tradeHistoryResponse = await ServiceAsync<XCloudServiceResponse<List<DXAtomicSwap>>>(request);

            var tradeHistories = tradeHistoryResponse.Reply
                .Where(p =>
                {
                    var trade = new List<string> { p.From, p.To };
                    var result = trade.All(ta => assetWhiteList.Select(awl => awl.Ticker).Contains(ta));
                    return result;
                })
                .ToList();
            
            request = new XCloudServiceRequest
            {
                Service = "xrs::CCMultiPrice",
                Parameters = new object[] { string.Join(",", coins), string.Join(",", typeof(CoinPrice).GetProperties().Select(p => p.Name)) },
                NodeCount = 1
            };
            var multiPriceResponse = await ServiceAsync<XCloudServiceResponse<Dictionary<string, CoinPrice>>>(request);

            // Output 
            // Total Volume in BLOCK
            decimal totalVolumeBLOCK = CalculateVolume("BLOCK", multiPriceResponse.Reply, trades);
            // Total Volume in BTC
            decimal totalVolumeBTC = CalculateVolume("BTC", multiPriceResponse.Reply, trades);
            // Total Volume in USD
            decimal totalVolumeUSD = CalculateVolume("USD", multiPriceResponse.Reply, trades);

            // Average Trade size?
            // Fees Collected?
            // Total number of trades
            int totalNumberOfTrades = tradeHistories.Count;

            string tweet = "24 Hour @BlockDXExchange Statistics (" + DateTime.Now.ToUniversalTime().ToString("MMMM d yyyy") + " UTC)"
                + "\n\nTrading Volume:"
                + "\n\n$USD: $" + totalVolumeUSD.ToString("N3", CultureInfo.InvariantCulture)
                + "\n$BLOCK: " + totalVolumeBLOCK.ToString("N3", CultureInfo.InvariantCulture) + " BLOCK"
                + "\n$BTC: " + totalVolumeBTC.ToString("N3", CultureInfo.InvariantCulture) + " BTC"
                + "\n\nNumber of Trades: " + totalNumberOfTrades;

            return tweet;
        }

        static async Task<T> ServiceAsync<T>(XCloudServiceRequest request)
        {
            var content = JsonConvert.SerializeObject(request);
            HttpContent httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"/api/xrs/Service", httpContent);

            string result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
            return JsonConvert.DeserializeObject<T>(result);
        }

        static decimal CalculateVolume(string coin, Dictionary<string, CoinPrice> coinPrices, Dictionary<string, DXTradePair> tradepairs)
        {
            decimal volume = 0;
            foreach (KeyValuePair<string, DXTradePair> entry in tradepairs)
            {
                var coinOne = entry.Key.Split("-")[0];
                var coinTwo = entry.Key.Split("-")[1];
                
                var coinPrice = coinPrices[coinOne];
                Type myType = coinPrice.GetType();

                decimal price = (decimal) myType.GetProperty(coin).GetValue(coinPrice);
              
                volume += entry.Value.Volume * price;
               
            }
            return volume;
        }

    }

    public class DXTradePair
    {
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Timestamp { get; set; }
    }

    public class DXAtomicSwap
    {
        public long Timestamp { get; set; }
        public string TxId { get; set; }
        public string To { get; set; }
        public string XId { get; set; }
        public string From { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
    }

    public class CoinPrice
    {
        public decimal BTC { get; set; }
        public decimal BLOCK { get; set; }
        public decimal USD { get; set; }
    }

    public class XCloudServiceRequest
    {
        public string Service { get; set; }
        public object[] Parameters { get; set; }
        public int NodeCount { get; set; }
    }

    public class XCloudServiceResponse<T>
    {
        [JsonProperty(PropertyName = "reply")]
        public T Reply { get; set; }

        [JsonProperty(PropertyName = "uuid")]
        public string UUID { get; set; }
    }
}