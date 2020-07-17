using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XBridgeTwitterBot.Entity;

namespace XBridgeTwitterBot.Interfaces
{
    public interface IBlocknetApiService
    {
        Task<List<OpenOrder>> DxGetOrders();
    }
}
