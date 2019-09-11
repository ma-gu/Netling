using System;
using System.Linq;

namespace Netling.Core.BulkHttpClientWorker
{
    public static class BigEndian
    {
        public static byte[] GetBytes(int value) =>
            ! BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(value)
            : BitConverter.GetBytes(value).Reverse().ToArray();

        public static int ToInt32(byte[] array) =>
            !BitConverter.IsLittleEndian
            ? BitConverter.ToInt32(array, 0)
            : BitConverter.ToInt32(array.Reverse().ToArray(), 0);
    }
}
