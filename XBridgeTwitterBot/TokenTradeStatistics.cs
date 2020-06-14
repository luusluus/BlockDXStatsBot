using System;
using System.Collections.Generic;
using System.Text;

namespace XBridgeTwitterBot
{
    class TokenTradeStatistics
    {
        public string Token { get; set; }
        public List<TokenVolume> Volumes { get; set; }
        public int TradeCount { get; set; }
    }
}
