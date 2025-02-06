using System;
using System.IO.Ports;
using System.Security.Cryptography;
using EC_PM_80250_Library;
using System.Text.Json;

class Program
{
    static void Main()
    {


        try
        {
            EC_PM_80250 printer = new EC_PM_80250("192.168.123.100", 9100);
            printer.OpenConnection();
            Console.WriteLine("Conexión establecida con la impresora.");

            printer.ResetPrinter();
         
            // Imprimir texto
            printer.TextConstructor(1, 1111111);
            printer.PrintText("\nEstacionamiento CRC", PrintTextCommands.Center);
            printer.PrintText("\nEstacionamiento CRC", PrintTextCommands.Double);
            /*
            EC_PM_80250 printer = new EC_PM_80250("COM6", 19200);
            printer.OpenConnection();
            printer.ResetPrinter();
            printer.PrintText("Hola Mundo", PrintTextCommands.Bold);
            printer.CloseConnection();

            EC_PM_80250 printer2 = new EC_PM_80250("192.168.123.100", 9100);
            printer2.OpenConnection();
            printer2.ResetPrinter();
            printer2.PrintText("Hola desde Ethernet", PrintTextCommands.Center);
            printer2.CloseConnection();

            */
            // Imprimir código de barras
            //Console.WriteLine("Imprimiendo código de barras...");
            //printer.PrintBarcode("1234567890", 0x04);

            // Imprimir código QR
            //Console.WriteLine("Imprimiendo código QR...");
            //printer.PrintQRCode("https://www.google.com");

            // Imprimir imagen
            //Console.WriteLine("Imprimiendo imagen...");
            //printer.PrintImageRaster("C:\\Users\\ofuentes\\source\\repos\\EC_PM_80250_Library\\Prueba\\Images\\Joker.bmp", 576); // Reemplaza con una imagen BMP válida

            printer.PrintText("\n ");
            printer.PrintText("\n ");


            // Cortar papel
            printer.CutPaper();

            printer.CloseConnection();
            Console.WriteLine("Impresión enviada correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        
    }
}
