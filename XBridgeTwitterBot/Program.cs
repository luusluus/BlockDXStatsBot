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

namespace XBridgeTwitterBot
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();

        const double interval = 1000 * 60 * 60 * 24;
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            Auth.SetUserCredentials(
                configuration.GetSection("consumerKey").Value, 
                configuration.GetSection("consumerSecret").Value, 
                configuration.GetSection("userAccessToken").Value, 
                configuration.GetSection("userAccessSecret").Value
            );

            client.BaseAddress = new Uri(configuration.GetSection("baseAddress").Value);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            //checkForTime_Elapsed();
            Timer checkForTime = new Timer(interval);
            checkForTime.Elapsed += new ElapsedEventHandler(CheckForTime_Elapsed);
            checkForTime.Enabled = true;

            Console.ReadKey();
        }

        private static async void CheckForTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var request = new XCloudServiceRequest
                {
                    Service = "xrs::dxGet24hrTradeSummary",
                    Parameters = new object[] { "0" },
                    NodeCount = 1
                };
                var tradeSummaryResponse = await ServiceAsync<XCloudServiceResponse<List<Dictionary<string, DXTradePair>>>>(request);
                var coins = new HashSet<string>();
                foreach (var item in tradeSummaryResponse.Reply.First().Keys.ToList())
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

                request = new XCloudServiceRequest
                {
                    Service = "xrs::CCMultiPrice",
                    Parameters = new object[] { string.Join(",", coins), string.Join(",",typeof(CoinPrice).GetProperties().Select(p => p.Name)) },
                    NodeCount = 1
                };
                var multiPriceResponse = await ServiceAsync<XCloudServiceResponse<Dictionary<string, CoinPrice>>>(request);

                // Output 
                // Total Volume in BLOCK
                decimal totalVolumeBLOCK = CalculateVolume("BLOCK", multiPriceResponse.Reply, tradeSummaryResponse.Reply.First());
                // Total Volume in BTC
                decimal totalVolumeBTC = CalculateVolume("BTC", multiPriceResponse.Reply, tradeSummaryResponse.Reply.First());
                // Total Volume in USD
                decimal totalVolumeUSD = CalculateVolume("USD", multiPriceResponse.Reply, tradeSummaryResponse.Reply.First());

                // Average Trade size?
                // Fees Collected?
                // Total number of trades
                int totalNumberOfTrades = tradeHistoryResponse.Reply.Count;

                string tweet = "24 Hour @BlockDXExchange Statistics " + DateTime.Now.ToUniversalTime().ToString("MMMM d yyyy") + " UTC"
                    + "\n\n$USD Volume: $" + totalVolumeUSD.ToString(CultureInfo.InvariantCulture)
                    + "\n$BLOCK Volume: " + totalVolumeBLOCK.ToString(CultureInfo.InvariantCulture) + " BLOCK"
                    + "\n$BTC Volume: " + totalVolumeBTC.ToString(CultureInfo.InvariantCulture) + " BTC"
                    + "\nNumber of Trades: " + totalNumberOfTrades;

                Console.WriteLine(tweet);
                Tweet.PublishTweet(tweet);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        static async Task<T> ServiceAsync<T>(XCloudServiceRequest request)
        {
            var content = JsonConvert.SerializeObject(request);
            HttpContent httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"api/blocknet/xrouter/service", httpContent);

            string result = await response.Content.ReadAsStringAsync();
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