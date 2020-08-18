using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XBridgeTwitterBot.Entity;
using XBridgeTwitterBot.Interfaces;

namespace XBridgeTwitterBot.Services
{
    public class DxDataService : IDxDataService
    {
        private readonly HttpClient _client;

        public DxDataService(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<OpenOrdersPerMarket>> GetOpenOrdersPerMarket()
        {
            var openOrdersPerMarketResponse = await _client.GetAsync("GetOpenOrdersPerMarket");

            if (!openOrdersPerMarketResponse.IsSuccessStatusCode)
                throw new ApplicationException();

            string openOrdersPerMarketResult = await openOrdersPerMarketResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<OpenOrdersPerMarket>>(openOrdersPerMarketResult);
        }

        public async Task<List<CompletedOrderCount>> GetOneDayCompletedOrders()
        {
            var completedOrdersResponse = await _client.GetAsync("GetOneDayCompletedOrders");

            if (!completedOrdersResponse.IsSuccessStatusCode)
                throw new ApplicationException();

            string completedOrdersResponseResult = await completedOrdersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<CompletedOrderCount>>(completedOrdersResponseResult);
        }

        public async Task<int> GetOneDayTotalTradesCount()
        {
            var totalTradeCountResponse = await _client.GetAsync("GetOneDayTotalTradesCount");

            if (!totalTradeCountResponse.IsSuccessStatusCode)
                throw new ApplicationException();

            string totalTradeCountResult = await totalTradeCountResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<int>(totalTradeCountResult);
        }

        public async Task<List<CoinVolume>> GetOneDayTotalVolume(string coin, string units)
        {
            string queryString = "GetOneDayTotalVolume?coin=" + coin + "&units=" + units;

            var totalVolumeResponse = await _client.GetAsync(queryString);

            if (!totalVolumeResponse.IsSuccessStatusCode)
                throw new ApplicationException();

            string totalVolumeResult = await totalVolumeResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<CoinVolume>>(totalVolumeResult);
        }

        public async Task<List<CoinTradeStatistics>> GetOneDayTotalVolumePerCoin(string units)
        {
            string queryString = "GetOneDayTotalVolumePerCoin?units=" + units;

            var totalVolumePerTradedCoinResponse = await _client.GetAsync(queryString);

            if (!totalVolumePerTradedCoinResponse.IsSuccessStatusCode)
                throw new ApplicationException();

            string totalVolumePerTradedCoinResult = await totalVolumePerTradedCoinResponse.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<List<CoinTradeStatistics>>(totalVolumePerTradedCoinResult);
        }
    }
}
