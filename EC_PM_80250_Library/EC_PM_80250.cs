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

    public enum PrintTextCommands
    {
        // bool bold = false, bool underline = false, string align = "left", string size = "normal"
        Bold = 0,
        Underline = 1,
        Center = 2,
        Right = 3,
        DoubleWidth = 4,
        DoubleHeight = 5,
        Double = 6,
        Normal = 7
    }

    public class EC_PM_80250
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private PrinterConnectionManager _connection;

        public EC_PM_80250(string portOrIp, int baudOrPort)
        {
            _connection = new PrinterConnectionManager(portOrIp, baudOrPort);
        }

        public PrinterStatus OpenConnection() => _connection.OpenConnection();
        public PrinterStatus CloseConnection() => _connection.CloseConnection();
        public PrinterStatus IsConnected() => _connection.IsConnected();
        public PrinterStatus Reconnect()
        {
            return _connection.Reconnect();
        }

        public PrinterStatus CheckPrinterStatus()
        {
            try
            {
                // Si no hay conexión, regresamos NotOpen
                if (IsConnected() != PrinterStatus.Normal) return PrinterStatus.NotOpen;

                // Enviar comando ESC/POS para obtener estado
                byte[] statusCommand = { 0x10, 0x04, 0x01 };
                _connection.SendData(statusCommand);
                Thread.Sleep(200); // Esperar respuesta

                // Leer la respuesta
                byte[] response = _connection.ReceiveData();
                if (response.Length == 0)
                {
                    logger.Warn("No se recibió respuesta de la impresora.");
                    return PrinterStatus.Unknown;
                }

                byte statusByte = response[0];

                // Ignorar valores inválidos como 255 (posible error de comunicación)
                if (statusByte == 255) return PrinterStatus.Unknown;

                // Mapeo de códigos conocidos
                return statusByte switch
                {
                    22 => PrinterStatus.Normal,
                    30 => PrinterStatus.CoverOpenOrPaperEmpty,
                    16 => PrinterStatus.Printing,
                    32 => PrinterStatus.Error,
                    _ => PrinterStatus.Unknown
                };
            }
            catch (Exception ex)
            {
                logger.Error($"Error al obtener estado de la impresora: {ex.Message}");
                return PrinterStatus.NotOpen;  // ⬅️ Regresamos `NotOpen` en caso de fallo
            }
        }


        public PrinterStatus TextConstructor(int op, int id)
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                PrinterStatus status = CheckPrinterStatus();
                if (status != PrinterStatus.Normal) return status;

                string dia = PrinterUtils.Tiempo(0);
                string hora = PrinterUtils.Tiempo(1);

                PrintText("*****************************************", PrintTextCommands.Center);
                PrintText("\nEstacionamiento CRC", PrintTextCommands.Center, PrintTextCommands.Bold, PrintTextCommands.Double);
                PrintText("\nLic. Antonio del Moral 45, Morelia");
                PrintText($"\nFecha y hora de entrada: {dia} {hora}");
                PrintText("\nTarifa: $15.00 por hora", PrintTextCommands.Underline);
                PrintText("\nZona: A1");

                if (op == 1)
                {
                    PrintText("\n");
                    dia = PrinterUtils.Tiempo(2);
                    hora = PrinterUtils.Tiempo(3);
                    string barcodeData = $"{dia}{hora}{id}";
                    PrintBarcode(barcodeData);
                    PrintText($"\n{barcodeData}", PrintTextCommands.Center);
                }
                else if (op == 2)
                {
                    saltoLinea();
                    var jsonData = new
                    {
                        id,
                        fecha_entrada = dia,
                        hora_entrada = hora,
                        lugar = "Lic. Antonio del Moral 45, Morelia",
                        zona = "A3",
                        tarifa = "15.00"
                    };
                    PrintQRCode(jsonData);
                }

                saltoLinea();
                PrintText("\n*****************************************", PrintTextCommands.Center);
                saltoLinea();
                PrintText("\n ", PrintTextCommands.Center);

                // Esperar un momento antes de verificar el estado después de imprimir
                Thread.Sleep(2000);
                return CheckPrinterStatus();
            }
            catch (Exception ex)
            {
                logger.Error($"Error en TextConstructor: {ex.Message}");
                return PrinterStatus.Error;
            }
        }


        public void saltoLinea()
        {
            try
            {
                _connection.SendData(new byte[] { 0x0A }); // Comando ESC/POS para nueva línea
            }
            catch (Exception ex)
            {
                logger.Error($"Error en saltoLinea: {ex.Message}");
            }
        }

        public PrinterStatus PrintText(string text, params PrintTextCommands[] options)
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                logger.Info($"Imprimiendo texto: '{text}' con opciones [{string.Join(", ", options)}]");

                // Construir comando de inicialización ESC/POS
                List<byte> commandBuffer = new List<byte> { 0x1B, 0x40 }; // Reset de la impresora

                // Opciones predeterminadas
                bool bold = false;
                bool underline = false;
                byte align = 0x00; // Izquierda por defecto
                byte size = 0x00; // Tamaño normal

                // Evaluar las opciones enviadas
                foreach (var option in options)
                {
                    switch (option)
                    {
                        case PrintTextCommands.Bold:
                            bold = true;
                            break;
                        case PrintTextCommands.Underline:
                            underline = true;
                            break;
                        case PrintTextCommands.Center:
                            align = 0x01;
                            break;
                        case PrintTextCommands.Right:
                            align = 0x02;
                            break;
                        case PrintTextCommands.DoubleWidth:
                            size |= 0x10;
                            break;
                        case PrintTextCommands.DoubleHeight:
                            size |= 0x20;
                            break;
                        case PrintTextCommands.Double:
                            size = 0x30;
                            break;
                    }
                }

                // Aplicar alineación
                commandBuffer.AddRange(new byte[] { 0x1B, 0x61, align });

                // Aplicar tamaño del texto
                commandBuffer.AddRange(new byte[] { 0x1B, 0x21, size });

                // Aplicar negrita y subrayado si están activados
                if (bold) commandBuffer.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
                if (underline) commandBuffer.AddRange(new byte[] { 0x1B, 0x2D, 0x01 });

                // Agregar el texto a imprimir
                commandBuffer.AddRange(Encoding.ASCII.GetBytes(text + "\n"));

                // Reset de estilos después de imprimir
                commandBuffer.AddRange(new byte[] { 0x1B, 0x21, 0x00, 0x1B, 0x45, 0x00, 0x1B, 0x2D, 0x00 });

                // Enviar los datos a la impresora
                _connection.SendData(commandBuffer.ToArray());

                return PrinterStatus.Normal;
            }
            catch (Exception ex)
            {
                logger.Error($"Error en PrintText: {ex.Message}");
                return PrinterStatus.Error;
            }
        }



        public PrinterStatus PrintBarcode(string data)
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                List<byte> commandBuffer = new List<byte>();

                // Inicializar la impresora
                commandBuffer.AddRange(EscPosCommands.InitPrinter);

                // Alinear al centro
                commandBuffer.AddRange(new byte[] { 0x1B, 0x61, 0x01 });

                // Configurar altura y grosor del código de barras
                commandBuffer.AddRange(new byte[] { 0x1D, 0x68, 100 }); // Altura (100 píxeles)
                commandBuffer.AddRange(new byte[] { 0x1D, 0x77, 2 });   // Ancho de línea

                // Configurar posición del texto bajo el código de barras
                commandBuffer.AddRange(new byte[] { 0x1D, 0x48, 0x00 });

                // Definir tipo de código de barras (CODE128 = 0x04) y enviar datos
                commandBuffer.AddRange(new byte[] { 0x1D, 0x6B, 0x04 });
                commandBuffer.AddRange(Encoding.ASCII.GetBytes(data));
                commandBuffer.Add(0x00); // Fin de datos

                // Enviar el comando completo a la impresora
                _connection.SendData(commandBuffer.ToArray());

                return PrinterStatus.Normal;
            }
            catch (Exception ex)
            {
                logger.Error($"Error en PrintBarcode: {ex.Message}");
                return PrinterStatus.Error;
            }
        }



        public PrinterStatus PrintQRCode(object jsonData)
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                // Convertir el objeto JSON en string
                string jsonString = JsonSerializer.Serialize(jsonData);
                byte[] qrData = Encoding.UTF8.GetBytes(jsonString);
                int dataLength = qrData.Length + 3;
                byte pL = (byte)(dataLength % 256);
                byte pH = (byte)(dataLength / 256);

                // Construcción de comandos ESC/POS para QR Code
                List<byte> commandBuffer = new List<byte>
        {
            0x1B, 0x40, // Reset de la impresora
            0x1B, 0x61, 0x01, // Alinear al centro
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x04, // Tamaño del QR (4 = tamaño medio)
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30, // Nivel de corrección de error (L)
            0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 // Almacenar datos del QR
        };

                // Agregar los datos del QR a la lista de comandos
                commandBuffer.AddRange(qrData);

                // Comando para imprimir el QR almacenado
                commandBuffer.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });

                // Enviar el conjunto de comandos a la impresora
                _connection.SendData(commandBuffer.ToArray());

                logger.Info($"Código QR impreso con JSON: {jsonString}");

                return PrinterStatus.Normal;
            }
            catch (Exception ex)
            {
                logger.Error($"Error en PrintQRCode: {ex.Message}");
                return PrinterStatus.Error;
            }
        }




        public PrinterStatus PrintImageRaster(string imagePath, int maxWidth)
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                logger.Info($"Cargando imagen desde: {imagePath}");

                using (Bitmap originalBmp = new Bitmap(imagePath))
                {
                    // Redimensionar la imagen al ancho máximo permitido
                    Bitmap resizedBmp = PrinterUtils.ResizeImage(originalBmp, maxWidth);

                    // Convertir la imagen a monocromático (1 bit por píxel)
                    Bitmap monoBmp = PrinterUtils.ConvertTo1Bpp(resizedBmp);

                    int width = monoBmp.Width;
                    int height = monoBmp.Height;

                    // Ajustar el ancho para que sea múltiplo de 8
                    if (width % 8 != 0) width = (width / 8 + 1) * 8;

                    logger.Info($"Tamaño de la imagen ajustado: {width}x{height}");

                    // Convertir la imagen a datos ESC/POS raster
                    byte[] imageData = PrinterUtils.ConvertToEscPosRaster(monoBmp);

                    // Comando ESC/POS para impresión de imagen en modo raster
                    List<byte> commandBuffer = new List<byte>
            {
                0x1D, 0x76, 0x30, 0x00,  // Comando para impresión de imagen raster
                (byte)(width / 8), 0x00, // Ancho en bytes
                (byte)height, 0x00       // Alto en píxeles
            };

                    // Agregar la imagen rasterizada al buffer
                    commandBuffer.AddRange(imageData);

                    // Agregar salto de línea para finalizar la impresión
                    commandBuffer.Add(0x0A);

                    // Enviar la imagen a la impresora usando Ethernet o Serial
                    _connection.SendData(commandBuffer.ToArray());

                    logger.Info("Imagen enviada en modo Raster.");
                    return PrinterStatus.Normal;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error en PrintImageRaster: {ex.Message}");
                return PrinterStatus.Error;
            }
        }




        public PrinterStatus WaitForPrinter()
        {
            try
            {
                int timeout = 10000; // Tiempo máximo de espera en milisegundos
                int waitedTime = 0;

                logger.Info("Esperando a que la impresora termine...");

                while (waitedTime < timeout)
                {
                    System.Threading.Thread.Sleep(500);
                    waitedTime += 500;

                    byte[] statusCommand = { 0x1D, 0x72, 0x01 }; // Comando para obtener estado de impresión
                    _connection.SendData(statusCommand);
                    System.Threading.Thread.Sleep(200);

                    byte[] response = _connection.ReceiveData();

                    if (response.Length > 0 && (response[0] & 0x08) == 0) // Verifica si la impresión ha terminado
                    {
                        logger.Info("La impresora ha terminado de imprimir.");
                        return PrinterStatus.Normal;
                    }
                }

                logger.Warn("Tiempo de espera agotado. La impresora no respondió.");
                return PrinterStatus.Unknown;
            }
            catch (Exception ex)
            {
                logger.Error($"Error en WaitForPrinter: {ex.Message}");
                return PrinterStatus.Error;
            }
        }


        public PrinterStatus ResetPrinter()
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                byte[] resetCommand = { 0x1B, 0x40 }; // Comando ESC/POS para reiniciar la impresora
                _connection.SendData(resetCommand);

                logger.Info("Se ha enviado el comando de reinicio a la impresora.");
                System.Threading.Thread.Sleep(100); // Pequeña espera para procesar el reinicio
                return PrinterStatus.Normal;
            }
            catch (Exception ex)
            {
                logger.Error($"Error en ResetPrinter: {ex.Message}");
                return PrinterStatus.Error;
            }
        }


        public PrinterStatus CutPaper()
        {
            try
            {
                if (IsConnected() != PrinterStatus.Normal) OpenConnection();

                byte[] cutCommand = { 0x1D, 0x56, 0x00 }; // Comando ESC/POS para corte de papel
                _connection.SendData(cutCommand);

                logger.Info("Enviado comando de corte de papel.");
                return PrinterStatus.Normal;
            }
            catch (Exception ex)
            {
                logger.Error($"Error en CutPaper: {ex.Message}");
                return PrinterStatus.Error;
            }
        }

    }
}
