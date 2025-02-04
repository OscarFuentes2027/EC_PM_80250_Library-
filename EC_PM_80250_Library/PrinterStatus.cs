using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EC_PM_80250_Library
{
    public enum PrinterStatus
    {
        Normal = 0,
        PaperEmpty = 1,
        CoverOpen = 2,
        Printing = 16,
        Error = 32,
        NotOpen = 64,
        Unknown = 128
    }
}
