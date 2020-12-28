using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using XBridgeTwitterBot.Interfaces;

namespace XBridgeTwitterBot.Services
{
    public class ComposeTweetService : IComposeTweetService
    {
        private readonly IDxDataService _dxDataService;
        private readonly IBlocknetApiService _blocknetApiService;

        static readonly List<string> units = new List<string>()
            {
                "BLOCK",
                "BTC",
                "USD"
            };

        public ComposeTweetService(IDxDataService dxDataService, IBlocknetApiService blocknetApiService)
        {
            _dxDataService = dxDataService;
            _blocknetApiService = blocknetApiService;
        }


        public async Task<List<string>> ComposeOrdersAndActiveMarkets()
        {
            var TWEET_MAX_CHARS = 280;

            var openOrders = await _blocknetApiService.DxGetOrders();

            var openOrdersPerMarket = await _dxDataService.GetOpenOrdersPerMarket();

            var tweets = new List<string>();

            var tweet = string.Empty;
            tweet += "Number of Open Orders: " + openOrders.Count();

            tweet += "\n\nActive Markets:"; //BLOCK/DASH DASH/LTC

            // 17 lines of open order pairs with above text is usually more than 280 characters

            var amountOfTweets = Math.Ceiling((decimal)openOrdersPerMarket.Count / 17);
            if (openOrdersPerMarket.Count >= 17)
            {
                tweet += " (1/" + amountOfTweets + ")";
            }

            tweet += "\n";

            var concatString = string.Empty;

            int idx = 2;
            openOrdersPerMarket.ForEach(am =>
            {
                concatString = "\n$" + am.Market.Maker + " / $" + am.Market.Taker + ": " + am.Count;
                if (tweet.Length + concatString.Length > TWEET_MAX_CHARS)
                {
                    tweets.Add(tweet);
                    tweet = "(" + idx + "/" + amountOfTweets + ")\n";
                    idx++;
                }

                tweet += concatString;
            });

            tweets.Add(tweet);

            return tweets;
        }

        public async Task<string> ComposeCompletedOrderTweet()
        {
            var completedOrders = await _dxDataService.GetOneDayCompletedOrders();

            if (completedOrders.Count.Equals(0))
                throw new Exception("No completed orders on the 24h on BlockDX");

            string tweet = "Completed Orders:\n\n";

            foreach (var order in completedOrders)
            {
                tweet += "$" + order.Coin + ": " + order.Count + "\n";
            }

            return tweet;
        }

        public async Task<List<string>> ComposeVolumePerCoinTweets()
        {
            var volumesPerCoin = await _dxDataService.GetOneDayTotalVolumePerCoin(string.Join(",", units));

            var childrenTweets = new List<string>();
            foreach (var coinVolume in volumesPerCoin)
            {
                if (coinVolume.TradeCount > 0)
                {
                    string tweet = "Trading Volume $" + coinVolume.Coin + ":"
                    + "\n";

                    foreach (var volume in coinVolume.Volumes.OrderByDescending(v => v.Unit))
                    {
                        string unit = "\n$";
                        if (volume.Unit.Equals("USD"))
                        {
                            unit += (volume.Unit + ": $" + volume.Volume.ToString("N2", CultureInfo.InvariantCulture));
                        }
                        else
                            unit += (volume.Unit + ": " + volume.Volume.ToString("N3", CultureInfo.InvariantCulture) + " " + volume.Unit);

                        tweet += unit;
                    }

                    tweet += "\n\nNumber of Trades: " + coinVolume.TradeCount;

                    childrenTweets.Add(tweet);
                }
            }
            return childrenTweets;
        }
        public async Task<string> ComposeTotalVolumeTweet()
        {
            var volumes = await _dxDataService.GetOneDayTotalVolume("0", string.Join(",", units));
            var totalTradeCount = await _dxDataService.GetOneDayTotalTradesCount();

            string tweet = "24 Hour @BlockDXExchange Statistics (" + DateTime.Now.ToUniversalTime().ToString("MMMM d yyyy") + " UTC)"
                + "\n\nTotal Trading Volume:"
                + "\n";

            if (volumes?.Any() != true)
            {
                throw new Exception("No 24h volume on the BlockDX.");
            }
            else
            {
                foreach (var volume in volumes.OrderByDescending(v => v.Unit))
                {
                    string unit = "\n$";
                    if (volume.Unit.Equals("USD"))
                    {
                        unit += (volume.Unit + ": $" + volume.Volume.ToString("N2", CultureInfo.InvariantCulture));
                    }
                    else
                        unit += (volume.Unit + ": " + volume.Volume.ToString("N3", CultureInfo.InvariantCulture) + " " + volume.Unit);

                    tweet += unit;
                }
            }

            tweet += "\n\nNumber of Trades: " + totalTradeCount;
            return tweet;
        }

        public string ComposeMoreDetailsTweet()
        {
            string tweet = string.Empty;

            tweet += "\n\nMore live statistics below 👇";

            tweet += "\n\nOfficial: https://blockdx.com/orders/";
            tweet += "\n\nCommunity: https://blockdx.co/orders";

            return tweet;
        }
    }
}
