using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace EC_PM_80250_Library
{
    public class PrinterConnectionManager
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private SerialPort _serialPort;

        public PrinterConnectionManager(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.Two);
            logger.Info($"Instanciada conexión con {portName} a {baudRate} baudios.");
            Console.WriteLine($"Instanciada conexión con {portName} a {baudRate} baudios.");
        }

        public bool OpenConnection()
        {
            try
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
                    return true;
                }
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                logger.Error("Error: El puerto está en uso por otro proceso.");
                Console.WriteLine("Error: El puerto está en uso por otro proceso.");
                return false;
            }
            catch (IOException)
            {
                logger.Error("Error: No se puede acceder al puerto, puede estar desconectado.");
                Console.WriteLine("Error: No se puede acceder al puerto, puede estar desconectado.");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error($"Error desconocido: {ex.Message}");
                Console.WriteLine($"Error desconocido: {ex.Message}");
                return false;
            }
        }

        public void CloseConnection()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                logger.Info("Conexión cerrada correctamente.");
                Console.WriteLine("Conexión cerrada correctamente.");
            }
        }

        public bool IsConnected()
        {
            return _serialPort != null && _serialPort.IsOpen;
        }

        public void Reconnect()
        {
            logger.Warn("Intentando reconectar...");
            Console.WriteLine("Reconectando...");
            CloseConnection();
            OpenConnection();
        }

        public SerialPort GetSerialPort()
        {
            return _serialPort;
        }
    }
    }

