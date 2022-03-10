using System;

namespace RFID
{
    public static class Rand
    {
        private static Random _random = new Random();
        public static ushort U16()
        {
            return (ushort) _random.Next();
        }

        public static ushort U16(int min, int max)
        {
            return (ushort) _random.Next(min, max);
        }
    }
}