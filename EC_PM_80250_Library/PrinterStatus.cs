using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EC_PM_80250_Library
{
    public enum PrinterStatus
    {
        Normal = 22,                //  Impresora lista
        CoverOpenOrPaperEmpty = 30, //  Tapa abierta y/o sin papel
        Printing = 16,              //  En impresión
        Error = 32,                 //  Error
        NotOpen = 64,               //  No conectada
        Unknown = 128               //  Estado desconocido
    }


}
