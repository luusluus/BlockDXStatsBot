using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XBridgeTwitterBot.Entity;

namespace XBridgeTwitterBot.Interfaces
{
    public interface IDxDataService
    {
        Task<List<CompletedOrderCount>> GetOneDayCompletedOrders();

        Task<List<CoinTradeStatistics>> GetOneDayTotalVolumePerCoin(List<string> units);

        Task<List<CoinVolume>> GetOneDayTotalVolume(string coin, List<string> units);

        Task<int> GetOneDayTotalTradesCount();

        Task<List<OpenOrdersPerMarket>> GetOpenOrdersPerMarket();
    }
}
