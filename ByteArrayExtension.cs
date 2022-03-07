using System;
namespace RFID
{
    public static class ByteArrayExtension
    {
        public static byte[] Concat(this byte[] former, byte[] latter)
        {
            var result = new byte[former.Length + latter.Length];
            former.CopyTo(result, 0);
            latter.CopyTo(result, former.Length);
            return result;
        }

        public static byte[] Concat(this byte[] former, byte latter)
        {
            var result = new byte[former.Length + 1];
            former.CopyTo(result, 0);
            result[former.Length] = latter;
            return result;
        }

    }
}