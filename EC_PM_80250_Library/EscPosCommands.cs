using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EC_PM_80250_Library
{
    namespace EC_PM_80250_Library
    {
        public static class EscPosCommands
        {
            public static readonly byte[] InitPrinter = { 0x1B, 0x40 };
            public static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
            public static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };
            public static readonly byte[] UnderlineOn = { 0x1B, 0x2D, 0x01 };
            public static readonly byte[] UnderlineOff = { 0x1B, 0x2D, 0x00 };
            public static readonly byte[] CutPaper = { 0x1D, 0x56, 0x00 };
        }
    }

}
