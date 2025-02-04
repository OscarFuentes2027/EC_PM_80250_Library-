using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using EC_PM_80250_Library.EC_PM_80250_Library;
using NLog;
using System.Text.Json;
using System.Reflection;

namespace EC_PM_80250_Library
{
    public class EC_PM_80250
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private PrinterConnectionManager _connection;

        public EC_PM_80250(string portName, int baudRate)
        {

            _connection = new PrinterConnectionManager(portName, baudRate);
        }

        public bool OpenConnection() => _connection.OpenConnection();
        public void CloseConnection() => _connection.CloseConnection();
        public bool IsConnected() => _connection.IsConnected();

        public PrinterStatus CheckPrinterStatus()
        {
            SerialPort serialPort = _connection.GetSerialPort();
            int printerResponse = serialPort.BytesToRead > 0 ? 0 : 32;
            logger.Info($"Estado de la impresora: {(printerResponse == 0 ? "Normal" : "Error")}");
            return printerResponse == 0 ? PrinterStatus.Normal : PrinterStatus.Error;
        }

        public void TextConstructor(int op, int id)
        {
            string dia = PrinterUtils.Tiempo(0); // Obtener la fecha actual
            string hora = PrinterUtils.Tiempo(1); // Obtener la hora actual

            // Imprimir el encabezado del ticket
            PrintText("*****************************************", false, false, "center");
            PrintText("\nEstacionamiento CRC", true, false, "center", "double");
            PrintText("\nLic. Antonio del Moral 45, Morelia");
            PrintText($"\nFecha y hora de entrada: {dia} {hora}");
            PrintText("\nTarifa: $15.00 por hora", false, true);
            PrintText("\nZona: A1");

            if (op == 1)
            {
                PrintText("\n");
                // Obtener la fecha y hora con el formato para el codigo de barras
                dia = PrinterUtils.Tiempo(2);
                hora = PrinterUtils.Tiempo(3);

                // Generar el código de barras c
                string barcodeData = $"{dia}{hora}{id}";
                PrintBarcode(barcodeData);

                // Imprimir el código de barras como texto
                PrintText($"\n{barcodeData}",false, false, "center");
            } else if (op == 2) {
                saltoLinea();
                var jsonData = new
                {
                    id = id,
                    fecha_entrada = dia,
                    hora_entrada = hora,
                    lugar = "Lic. Antonio del Moral 45, Morelia",
                    zona = "A3",
                    tarifa = "15.00"
                };
                PrintQRCode(jsonData);
            }


            saltoLinea();
            PrintText("\n*****************************************", false, false, "center");
            saltoLinea();
            PrintText("\n ", false, false, "center");



            // Cerrar la conexión después de imprimir todo
            CloseConnection();
        }

        public void saltoLinea()
        {
            SerialPort serialPort = _connection.GetSerialPort();
            serialPort.Write(new byte[] { 0x0A }, 0, 1);
        }

        public void PrintText(string text, bool bold = false, bool underline = false, string align = "left", string size = "normal")
        {
            if (!IsConnected()) OpenConnection();

            SerialPort serialPort = _connection.GetSerialPort();
            logger.Info($"Enviando texto: '{text}' (Negrita: {bold}, Subrayado: {underline}, Alineación: {align}, Tamaño: {size})");

            byte[] init = { 0x1B, 0x40 }; // Reset de la impresora
            serialPort.Write(init, 0, init.Length);

            // Configurar alineación
            byte[] alignCommand;
            switch (align.ToLower())
            {
                case "center":
                    alignCommand = new byte[] { 0x1B, 0x61, 0x01 }; // Alinear al centro
                    break;
                case "right":
                    alignCommand = new byte[] { 0x1B, 0x61, 0x02 }; // Alinear a la derecha
                    break;
                default:
                    alignCommand = new byte[] { 0x1B, 0x61, 0x00 }; // Alinear a la izquierda (por defecto)
                    break;
            }
            serialPort.Write(alignCommand, 0, alignCommand.Length);

            // Configurar tamaño del texto
            byte[] sizeCommand;
            switch (size.ToLower())
            {
                case "double-width":
                    sizeCommand = new byte[] { 0x1B, 0x21, 0x10 }; // Doble ancho
                    break;
                case "double-height":
                    sizeCommand = new byte[] { 0x1B, 0x21, 0x20 }; // Doble alto
                    break;
                case "double":
                    sizeCommand = new byte[] { 0x1B, 0x21, 0x30 }; // Doble ancho y doble alto
                    break;
                default:
                    sizeCommand = new byte[] { 0x1B, 0x21, 0x00 }; // Tamaño normal
                    break;
            }
            serialPort.Write(sizeCommand, 0, sizeCommand.Length);

            // Configurar negrita y subrayado
            if (bold) serialPort.Write(new byte[] { 0x1B, 0x45, 0x01 }, 0, 3); // Negrita ON
            if (underline) serialPort.Write(new byte[] { 0x1B, 0x2D, 0x01 }, 0, 3); // Subrayado ON

            byte[] textBytes = Encoding.ASCII.GetBytes(text + "\n");
            serialPort.Write(textBytes, 0, textBytes.Length);

            // Resetear formato después de imprimir el texto
            byte[] reset = { 0x1B, 0x21, 0x00, 0x1B, 0x45, 0x00, 0x1B, 0x2D, 0x00 };
            serialPort.Write(reset, 0, reset.Length);


        }




        public void PrintBarcode(string data)
        {
            if (!IsConnected()) OpenConnection();

            Console.WriteLine("Imprimiendo código de barras.");
            SerialPort serialPort = _connection.GetSerialPort();

            // Inicializar la impresora
            serialPort.Write(EscPosCommands.InitPrinter, 0, EscPosCommands.InitPrinter.Length);

            // Alinear al centro
            byte[] alignCenter = { 0x1B, 0x61, 0x01 }; // ESC a 1 (Centrar)
            serialPort.Write(alignCenter, 0, alignCenter.Length);

            // Configurar la altura del código de barras
            serialPort.Write(new byte[] { 0x1D, 0x68, 100 }, 0, 3); // Altura = 100

            // Configurar la posición del texto
            serialPort.Write(new byte[] { 0x1D, 0x48, 0x00 }, 0, 3); // Texto debajo del código de barras

            // Configurar el ancho del código de barras
            serialPort.Write(new byte[] { 0x1D, 0x77, 2 }, 0, 3); // Ancho = 2

            // Construir el comando para imprimir el código de barras
            List<byte> command = new List<byte> { 0x1D, 0x6B, 0x04 }; // CODE39
            command.AddRange(Encoding.ASCII.GetBytes(data)); // Agregar los datos del código de barras
            command.Add(0x00); // Terminador nulo

            // Enviar el comando a la impresora
            serialPort.Write(command.ToArray(), 0, command.Count);

        }


        public void PrintQRCode(object jsonData)
        {
            if (!IsConnected()) OpenConnection();

            SerialPort serialPort = _connection.GetSerialPort();

            // Convertir el objeto JSON en un string formateado
            string jsonString = JsonSerializer.Serialize(jsonData);

            byte[] init = { 0x1B, 0x40 }; // Reset de la impresora
            serialPort.Write(init, 0, init.Length);

            // Alinear al centro
            byte[] alignCenter = { 0x1B, 0x61, 0x01 }; // ESC a 1 (Centrar)
            serialPort.Write(alignCenter, 0, alignCenter.Length);

            // Tamaño del QR (1-16, donde 4 es un tamaño medio)
            byte[] qrSize = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x04 };
            serialPort.Write(qrSize, 0, qrSize.Length);

            // Error correction level (L = 48, M = 49, Q = 50, H = 51)
            byte[] qrErrorCorrection = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30 };
            serialPort.Write(qrErrorCorrection, 0, qrErrorCorrection.Length);

            // Almacenar datos del QR
            byte[] qrData = Encoding.UTF8.GetBytes(jsonString);
            int len = qrData.Length + 3;
            byte pL = (byte)(len % 256);
            byte pH = (byte)(len / 256);

            List<byte> storeQr = new List<byte> { 0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 };
            storeQr.AddRange(qrData);
            serialPort.Write(storeQr.ToArray(), 0, storeQr.Count);

            // Imprimir el QR almacenado
            byte[] printQr = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 };
            serialPort.Write(printQr, 0, printQr.Length);
            logger.Info($"Código QR impreso con JSON: {jsonString}");
        }



        public void PrintImageRaster(string imagePath, int maxWidth)
        {
            if (!IsConnected()) OpenConnection();

            SerialPort serialPort = _connection.GetSerialPort();
            logger.Info($"Cargando imagen desde: {imagePath}");

            using (Bitmap originalBmp = new Bitmap(imagePath))
            {
                // Redimensionar la imagen al ancho máximo
                Bitmap resizedBmp = PrinterUtils.ResizeImage(originalBmp, maxWidth);

                // Convertir a 1 bpp
                Bitmap monoBmp = PrinterUtils.ConvertTo1Bpp(resizedBmp);

                int width = monoBmp.Width;
                int height = monoBmp.Height;

                // Ajustar el ancho para que sea múltiplo de 8
                if (width % 8 != 0) width = (width / 8 + 1) * 8;

                logger.Info($"Tamaño de la imagen ajustado: {width}x{height}");

                // Convertir la imagen a datos ESC/POS raster
                byte[] imageData = PrinterUtils.ConvertToEscPosRaster(monoBmp);

                // Calcular el tamaño de los datos en bytes
                int dataWidth = width / 8;
                int dataHeight = height;
                int totalBytes = dataWidth * dataHeight;

                // Comando ESC/POS para impresión de imagen en modo raster
                byte[] startCommand = {
            0x1D, 0x76, 0x30, 0x00, // GS v 0 (raster mode)
            (byte)(dataWidth & 0xFF), (byte)((dataWidth >> 8) & 0xFF), // Ancho en bytes (little-endian)
            (byte)(dataHeight & 0xFF), (byte)((dataHeight >> 8) & 0xFF) // Alto en píxeles (little-endian)
        };

                // Enviar el comando de inicio
                serialPort.Write(startCommand, 0, startCommand.Length);

                // Enviar los datos de la imagen en chunks
                int chunkSize = 512; // Tamaño del chunk (ajusta según sea necesario)
                for (int i = 0; i < imageData.Length; i += chunkSize)
                {
                    int length = Math.Min(chunkSize, imageData.Length - i);
                    serialPort.Write(imageData, i, length);
                    System.Threading.Thread.Sleep(50); // Pequeña pausa entre chunks
                }

                // Enviar un salto de línea para finalizar
                serialPort.Write(new byte[] { 0x0A }, 0, 1);
                logger.Info("Imagen enviada en modo Raster.");
            }

            WaitForPrinter();
        }


        public void WaitForPrinter()
        {
            SerialPort serialPort = _connection.GetSerialPort();

  
            int timeout = 10000;
            int waitedTime = 0;

            logger.Info("Esperando a que la impresora termine...");

            while (waitedTime < timeout)
            {
                System.Threading.Thread.Sleep(500);
                waitedTime += 500;

                // Verificar si la impresora aún está imprimiendo (puede no funcionar en todas las impresoras)
                byte[] statusCommand = { 0x1D, 0x72, 0x01 }; // Comando ESC/POS para obtener estado
                serialPort.Write(statusCommand, 0, statusCommand.Length);
                System.Threading.Thread.Sleep(200); // Pequeña espera para recibir datos

                if (serialPort.BytesToRead > 0)
                {
                    byte[] response = new byte[serialPort.BytesToRead];
                    serialPort.Read(response, 0, response.Length);

                    if ((response[0] & 0x08) == 0) // Si el bit 3 es 0, la impresora está lista
                    {
                        logger.Info("La impresora ha terminado de imprimir.");
                        return;
                    }
                }
            }

            logger.Warn("Tiempo de espera agotado. La impresora no respondió.");
        }

        public void ResetPrinter()
        {
            if (!IsConnected()) OpenConnection();

            SerialPort serialPort = _connection.GetSerialPort();
            byte[] resetCommand = { 0x1B, 0x40 }; // Comando ESC/POS para reset de la impresora
            serialPort.Write(resetCommand, 0, resetCommand.Length);

            logger.Info("Se ha enviado el comando de reinicio a la impresora.");

            System.Threading.Thread.Sleep(100); // Pequeña espera para estabilidad
        }

        public void CutPaper()
        {
            if (!IsConnected()) OpenConnection();

            SerialPort serialPort = _connection.GetSerialPort();
            byte[] cutCommand = { 0x1D, 0x56, 0x00 };
            serialPort.Write(cutCommand, 0, cutCommand.Length);
            logger.Info("Enviado comando de corte de papel.");
            CloseConnection();
        }
    }
}
