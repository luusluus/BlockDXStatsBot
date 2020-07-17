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
    public class BlocknetApiService : IBlocknetApiService
    {
        private readonly HttpClient _client;

        public BlocknetApiService(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<OpenOrder>> DxGetOrders()
        {
            var openOrdersResponse = await _client.GetAsync("dxgetorders");

            if (!openOrdersResponse.IsSuccessStatusCode) 
                throw new ApplicationException();

            string openOrdersResult = await openOrdersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<OpenOrder>>(openOrdersResult);
        }
    }
}
