using System;

namespace RFID
{
    public static class Commands
    {
        public const ushort QueryRep = 0x0;
        public const ushort ACK = 0x1;
        public const ushort Query = 0x8;
        public const ushort QueryAdjust = 0x9;
        public const ushort Select = 0xa;
        public const ushort Req_RN = 0xc1;
        public const ushort Read = 0xc2;
        public const ushort Write = 0xc3;
        public const ushort Kill = 0xc4;
        public const ushort Lock = 0xc5;
        public const ushort Access = 0xc6;
    }
}