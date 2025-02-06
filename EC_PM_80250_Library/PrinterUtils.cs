using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EC_PM_80250_Library
{
    public static class PrinterUtils
    {
        public static byte[] ConvertImageToMonoBMP(string imagePath)
        {
            using (Bitmap originalBmp = new Bitmap(imagePath))
            {
                Console.WriteLine($"Formato original de la imagen: {originalBmp.PixelFormat}");

                // Convertir a formato 24bpp para evitar problemas con imágenes indexadas
                Bitmap convertedBmp = new Bitmap(originalBmp.Width, originalBmp.Height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(convertedBmp))
                {
                    g.DrawImage(originalBmp, 0, 0);
                }

                // Convertir manualmente la imagen a 1bpp sin usar Graphics.FromImage()
                Bitmap monoBmp = ConvertTo1Bpp(convertedBmp);

                using (MemoryStream ms = new MemoryStream())
                {
                    monoBmp.Save(ms, ImageFormat.Bmp);
                    return ms.ToArray();
                }
            }
        }

        public static byte[] ConvertToEscPosColumn(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            int bytesPerRow = (width + 7) / 8; // Cada byte representa 8 píxeles en horizontal
            List<byte> escposData = new List<byte>();

            for (int y = 0; y < height; y += 24) // Bloques de 24 píxeles de alto
            {
                // Comando ESC * para impresión de imagen en modo columna
                escposData.Add(0x1B); // ESC
                escposData.Add(0x2A); // '*'
                escposData.Add(33);   // Modo 24-dot (valores: 0 - 8-dot, 1 - 8-dot, 33 - 24-dot)
                escposData.Add((byte)(width / 8)); // Ancho en bytes
                escposData.Add(0x00); // Alto en bytes (no se usa en este caso)

                for (int x = 0; x < width; x += 8)
                {
                    for (int bitRow = 0; bitRow < 24; bitRow++)
                    {
                        byte pixelByte = 0;
                        int pixelY = y + bitRow;

                        if (pixelY < height)
                        {
                            for (int bit = 0; bit < 8; bit++)
                            {
                                int pixelX = x + bit;
                                if (pixelX < width)
                                {
                                    Color pixel = image.GetPixel(pixelX, pixelY);
                                    if (pixel.R < 128) // Si el píxel es oscuro, ponerlo en 1
                                    {
                                        pixelByte |= (byte)(1 << (7 - bit));
                                    }
                                }
                            }
                        }
                        escposData.Add(pixelByte);
                    }
                }

                escposData.Add(0x0A); // Salto de línea
            }

            return escposData.ToArray();
        }


        public static byte[] ConvertToEscPosRaster(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            int dataWidth = (width + 7) / 8; // Ancho en bytes
            List<byte> escposData = new List<byte>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < dataWidth; x++)
                {
                    byte pixelByte = 0;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        int pixelX = x * 8 + bit;
                        if (pixelX < width)
                        {
                            Color pixel = image.GetPixel(pixelX, y);
                            if (pixel.R < 128) // Si el píxel es oscuro, marcarlo como negro
                            {
                                pixelByte |= (byte)(1 << (7 - bit));
                            }
                        }
                    }
                    escposData.Add(pixelByte);
                }
            }

            return escposData.ToArray();
        }

        public static string Tiempo(int op)
        {
            if (op == 0)
            {
                string fecha = DateTime.Now.ToString("dd/MM/yyyy");
                return fecha;
            }
            else if (op == 1)
            {
                string fecha = DateTime.Now.ToString("HH:mm:ss");
                return fecha;
            }
            else if (op == 2)
            {
                string fecha = DateTime.Now.ToString("ddMMyy");
                return fecha;
            }
            else
            {
                string fecha = DateTime.Now.ToString("HHmm");
                return fecha;
            }
        }

        public static Bitmap ResizeImage(Bitmap originalImage, int maxWidth)
        {
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;
            int newWidth = Math.Min(originalWidth, maxWidth); // No superar el ancho máximo
            int newHeight = (originalHeight * newWidth) / originalWidth;

            Bitmap resizedImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            return resizedImage;
        }

        public static Bitmap ConvertTo1Bpp(Bitmap img)
        {
            int width = img.Width;
            int height = img.Height;
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format1bppIndexed);

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            BitmapData origData = img.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int stride = bmpData.Stride;
            byte[] pixelData = new byte[stride * height];
            byte[] origBytes = new byte[origData.Stride * height];

            System.Runtime.InteropServices.Marshal.Copy(origData.Scan0, origBytes, 0, origBytes.Length);

            int threshold = 128; // Umbral para convertir a blanco y negro
            for (int y = 0; y < height; y++)
            {
                byte currentByte = 0;
                int bitOffset = 7;
                for (int x = 0; x < width; x++)
                {
                    int index = (y * origData.Stride) + (x * 3);
                    byte r = origBytes[index];
                    byte g = origBytes[index + 1];
                    byte b = origBytes[index + 2];
                    byte gray = (byte)((r * 0.3) + (g * 0.59) + (b * 0.11));

                    // Invertir la condición: píxeles claros se convierten en negro
                    if (gray >= threshold) // Cambio aquí
                    {
                        currentByte |= (byte)(1 << bitOffset);
                    }

                    bitOffset--;
                    if (bitOffset < 0 || x == width - 1)
                    {
                        pixelData[y * stride + (x / 8)] = currentByte;
                        currentByte = 0;
                        bitOffset = 7;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
            bmp.UnlockBits(bmpData);
            img.UnlockBits(origData);

            return bmp;
        }

        public static int GetImageWidth(string imagePath)
        {
            using (Bitmap bmp = new Bitmap(imagePath))
            {
                return bmp.Width;
            }
        }

        public static int GetImageHeight(string imagePath)
        {
            using (Bitmap bmp = new Bitmap(imagePath))
            {
                return bmp.Height;
            }
        }



        public static byte[][] ConvertToEscPosRaster(byte[] monoBmpData)
        {
            return new byte[0][];
        }
    }
}

