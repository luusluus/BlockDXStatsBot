using System;
using System.Collections.Generic;
using System.Text;

namespace XBridgeTwitterBot.Entity
{
    public class OpenOrdersPerMarket
    {
        public Market Market { get; set; }
        public int Count { get; set; }
    }
}
