using System;
using System.Collections.Generic;
using System.Text;

namespace XBridgeTwitterBot.Entity
{
    public class CoinTradeStatistics
    {
        public string Coin { get; set; }
        public List<CoinVolume> Volumes { get; set; }
        public int TradeCount { get; set; }
    }
}
