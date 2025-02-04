using System;
using System.IO.Ports;
using EC_PM_80250_Library;

class Program
{
    static void Main()
    {
        try
        {
            EC_PM_80250 printer = new EC_PM_80250("COM6", 19200);
            printer.OpenConnection();
            Console.WriteLine("Conexión establecida con la impresora.");

            printer.ResetPrinter();
            Thread.Sleep(500); // Espera para que el reset se complete

            // Imprimir texto
            printer.TextConstructor(1,1111111);
            //Thread.Sleep(1000); // Espera para evitar solapamiento

            // Imprimir código de barras
            //Console.WriteLine("Imprimiendo código de barras...");
            //printer.PrintBarcode("1234567890", 0x04);
            //Thread.Sleep(2000); // Espera para asegurar que termine la impresión del código de barras

            // Imprimir código QR
            //Console.WriteLine("Imprimiendo código QR...");
            //printer.PrintQRCode("https://www.google.com");
            //Thread.Sleep(3000); // Espera para que el QR termine de imprimirse

            // Imprimir imagen
            //Console.WriteLine("Imprimiendo imagen...");
            //printer.PrintImageRaster("C:\\Users\\ofuentes\\source\\repos\\EC_PM_80250_Library\\Prueba\\Images\\Joker.bmp", 576); // Reemplaza con una imagen BMP válida
            //Thread.Sleep(4000); // Espera para que la imagen termine de imprimirse

            printer.PrintText("\n ");
            printer.PrintText("\n ");


            // Cortar papel
            printer.CutPaper();
            //Thread.Sleep(500); // Espera para asegurar el corte

            printer.CloseConnection();
            Console.WriteLine("Impresión enviada correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
