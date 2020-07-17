using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XBridgeTwitterBot.Interfaces
{
    public interface IComposeTweetService
    {
        Task<string> ComposeMoreDetailsTweet();
        Task<string> ComposeCompletedOrderTweet();
        Task<List<string>> ComposeVolumePerCoinTweets();
        Task<string> ComposeTotalVolumeTweet();

    }
}
