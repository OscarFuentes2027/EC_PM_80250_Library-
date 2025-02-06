using System;
using System.Threading;
using EC_PM_80250_Library;

class Estado
{
    public static void MonitorearEstado()
    {
        EC_PM_80250 printer = new EC_PM_80250("COM6", 19200);

        PrinterStatus connectionStatus = printer.OpenConnection();

        if (connectionStatus == PrinterStatus.NotOpen || connectionStatus == PrinterStatus.Error)
        {
            Console.WriteLine($"Error: No se pudo conectar a la impresora. Estado: {connectionStatus}");
            return;
        }

        Console.WriteLine("📡 Monitoreando estado de la impresora...");
        Console.WriteLine("🖨️ Presiona 'I' para imprimir un ticket de prueba.");
        Console.WriteLine("❌ Presiona 'C' para salir.");

        while (true)
        {
            PrinterStatus status = printer.CheckPrinterStatus();
            Console.WriteLine($"🔍 Estado de la impresora: {(int)status} ({status})");

            switch (status)
            {
                case PrinterStatus.Normal:
                    Console.WriteLine("✅ Impresora lista.");
                    break;
                case PrinterStatus.CoverOpenOrPaperEmpty:
                    Console.WriteLine("⚠️ Papel agotado o esta la tapa abierta.");
                    break;
                case PrinterStatus.Printing:
                    Console.WriteLine("🖨️ Impresión en proceso...");
                    break;
                case PrinterStatus.Error:
                    Console.WriteLine("❌ Error en la impresora.");
                    break;
                case PrinterStatus.NotOpen:
                    Console.WriteLine("❌ La impresora no está conectada. Intentando reconectar...");
                    printer.Reconnect();
                    break;
                default:
                    Console.WriteLine("❓ Estado desconocido.");
                    break;
            }

            if (Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.C)
                {
                    Console.WriteLine("Saliendo del programa...");
                    break;
                }
                else if (key == ConsoleKey.I)
                {
                    Console.WriteLine("🔄 Enviando ticket de prueba...");
                    PrinterStatus printStatus = printer.TextConstructor(1, 1111111);

                    if (printStatus == PrinterStatus.Normal)
                    {
                        Console.WriteLine("✅ Impresión realizada con éxito.");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Error en la impresión: {printStatus}");
                    }
                }
            }

            Thread.Sleep(1000); // Espera 1 segundo antes de la próxima verificación
        }

        printer.CloseConnection();
    }
}
