using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;

namespace EC_PM_80250_Library
{
    public class PrinterConnectionManager
    {



        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private SerialPort _serialPort;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        private readonly string _ipAddress;
        private readonly int _tcpPort;
        private readonly bool _useEthernet;

        public PrinterConnectionManager(string portOrIp, int baudOrPort)
        {
            // Si el parámetro parece ser un puerto COM (Ej: "COM6")
            if (Regex.IsMatch(portOrIp, @"^COM\d+$", RegexOptions.IgnoreCase))
            {
                _useEthernet = false;
                _serialPort = new SerialPort(portOrIp, baudOrPort, Parity.None, 8, StopBits.One);
                logger.Info($"Instanciada conexión Serial en {portOrIp} a {baudOrPort} baudios.");
            }
            else // Si el parámetro parece ser una dirección IP
            {
                _useEthernet = true;
                _ipAddress = portOrIp;
                _tcpPort = baudOrPort;
                logger.Info($"Instanciada conexión Ethernet a {_ipAddress}:{_tcpPort}.");
            }
        }



        public PrinterStatus OpenConnection()
        {
            try
            {
                if (_useEthernet)
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(_ipAddress, _tcpPort);
                    _networkStream = _tcpClient.GetStream();

                    logger.Info($"Conexión Ethernet establecida en {_ipAddress}:{_tcpPort}");
                    Console.WriteLine($"Conexión Ethernet establecida en {_ipAddress}:{_tcpPort}");
                    return PrinterStatus.Normal;
                }
                else
                {
                    if (_serialPort == null)
                    {
                        Console.WriteLine("El puerto no está inicializado.");
                        throw new InvalidOperationException("El puerto no está inicializado.");
                    }

                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                        logger.Info($"Conexión establecida en {_serialPort.PortName}");
                        Console.WriteLine($"Conexión establecida en {_serialPort.PortName}");
                        return PrinterStatus.Normal;
                    }
                }

                return PrinterStatus.Normal;
            }
            catch (UnauthorizedAccessException)
            {
                logger.Error("Error: El puerto está en uso por otro proceso.");
                Console.WriteLine("Error: El puerto está en uso por otro proceso.");
                return PrinterStatus.Error;
            }
            catch (IOException)
            {
                logger.Error("Error: No se puede acceder al puerto, puede estar desconectado.");
                Console.WriteLine("Error: No se puede acceder al puerto, puede estar desconectado.");
                return PrinterStatus.NotOpen;
            }
            catch (Exception ex)
            {
                logger.Error($"Error desconocido: {ex.Message}");
                Console.WriteLine($"Error desconocido: {ex.Message}");
                return PrinterStatus.Unknown;
            }
        }


        public PrinterStatus CloseConnection()
        {
            if (_useEthernet)
            {
                if (_tcpClient != null && _tcpClient.Connected)
                {
                    _networkStream?.Close();
                    _tcpClient?.Close();
                    _networkStream = null;
                    _tcpClient = null;
                    logger.Info("Conexión Ethernet cerrada correctamente.");
                    return PrinterStatus.NotOpen;
                }
            }
            else
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    logger.Info("Conexión Serial cerrada correctamente.");
                    return PrinterStatus.NotOpen;
                }
            }
            return PrinterStatus.Error;
        }



        public PrinterStatus IsConnected()
        {
            if (_useEthernet)
            {
                return (_tcpClient != null && _tcpClient.Connected) ? PrinterStatus.Normal : PrinterStatus.NotOpen;
            }
            else
            {
                return _serialPort != null && _serialPort.IsOpen ? PrinterStatus.Normal : PrinterStatus.NotOpen;
            }
        }


        public PrinterStatus Reconnect()
        {
            logger.Warn("Intentando reconectar...");
            Console.WriteLine("Reconectando...");

            CloseConnection();
            return OpenConnection();
        }

        public void SendData(byte[] data)
        {
            if (_useEthernet)
            {
                _networkStream?.Write(data, 0, data.Length);
            }
            else
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Write(data, 0, data.Length);
                }
            }
        }

        public byte[] ReceiveData()
        {
            if (_useEthernet)
            {
                if (_networkStream != null && _tcpClient.Connected)
                {
                    _networkStream.ReadTimeout = 1000; // Espera hasta 1 segundo
                    byte[] buffer = new byte[256];

                    try
                    {
                        int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                        return buffer[..bytesRead];
                    }
                    catch (IOException)
                    {
                        return Array.Empty<byte>(); // Si no hay datos, regresa un array vacío
                    }
                }
            }
            else
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    int timeout = 1000; // Máximo 1 segundo de espera
                    int elapsed = 0;
                    while (_serialPort.BytesToRead == 0 && elapsed < timeout)
                    {
                        System.Threading.Thread.Sleep(100);
                        elapsed += 100;
                    }

                    if (_serialPort.BytesToRead > 0)
                    {
                        byte[] buffer = new byte[_serialPort.BytesToRead];
                        _serialPort.Read(buffer, 0, buffer.Length);
                        return buffer;
                    }
                }
            }
            return Array.Empty<byte>();
        }


        public SerialPort GetSerialPort()
        {
            return _useEthernet ? null : _serialPort;
        }

    }
}
