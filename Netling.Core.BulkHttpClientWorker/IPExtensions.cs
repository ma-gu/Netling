using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Netling.Core.BulkHttpClientWorker
{
    public static class IPExtensions
    {
        public static IEnumerable<IPAddress> Range(this IPAddress start, int count)
        {
            var address = BigEndian.ToInt32(start.GetAddressBytes());
            for (int i = 0; i < count; i++)
                yield return new IPAddress(BigEndian.GetBytes(address + i));
        }

    }
}
