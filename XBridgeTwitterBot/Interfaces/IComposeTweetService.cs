using System.Collections.Generic;
using System.Threading.Tasks;

namespace XBridgeTwitterBot.Interfaces
{
    public interface IComposeTweetService
    {
        Task<List<string>> ComposeOrdersAndActiveMarkets();
        Task<string> ComposeCompletedOrderTweet();
        Task<List<string>> ComposeVolumePerCoinTweets();
        Task<string> ComposeTotalVolumeTweet();
        string ComposeMoreDetailsTweet();
    }
}
