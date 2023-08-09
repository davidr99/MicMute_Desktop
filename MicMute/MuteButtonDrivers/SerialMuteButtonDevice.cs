using MicMute.Events;
using MicMute.Interfaces;
using MicMute.Objects;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicMute.MuteDeviceDrivers
{
    internal class SerialMuteButtonDevice : IMuteButtonDriver
    {
        private SerialPort? _port;

        public event EventHandler<MuteButtonPressEvent>? ButtonPressEvent;

        public void CloseDevice()
        {
            if (_port?.IsOpen ?? false) 
            { 
                _port?.Close();
            }
        }

        public (bool error, string errorMsg) Connect(IMuteButtonDeviceData device)
        {
            CloseDevice();

            if (!TestPort(device.Value))
            {
                return (true, "Could not communicate with device!");
            }

            _port = new SerialPort(device.Value, 9600, Parity.None, 8, StopBits.One);

            _port.Open();
            _port.DtrEnable = true;
            _port.WriteTimeout = 1000;
            _port.ReadTimeout = 1000;

            _port.DataReceived += port_DataReceived;

            return (false, String.Empty);
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port?.ReadByte() == 'P')
            {
                ButtonPressEvent?.Invoke(this, new MuteButtonPressEvent());
            }
        }

        public List<IMuteButtonDeviceData> GetDeviceList()
        {
            var ports = SerialPorts();

            return ports.Select(p => new SerialMuteButtonDeviceData()
            {
                DisplayName = p,
                Value = p
            }).ToList<IMuteButtonDeviceData>();
        }

        public bool AutoConnect()
        {
            var ports = SerialPorts();

            var port = AutoDetect(ports);

            if (port != null)
            {
                (bool error, string errorMsg) = Connect(new SerialMuteButtonDeviceData()
                {
                    DisplayName = port,
                    Value = port
                });

                return !error;
            }

            return false;
        }

        public void Init()
        {

        }

        public bool WriteLED(LEDEnum ledStatus)
        {
            if (_port?.IsOpen ?? false)
            {
                try
                {
                    byte[] data = { (byte)'L', (byte)ledStatus };
                    _port.Write(data, 0, 2);
                }
                catch(Exception ex)
                {
                    return false;
                }
            }

            return true;
        }

        private List<String> SerialPorts()
        {
            return SerialPort.GetPortNames().Distinct().ToList();
        }

        private string? AutoDetect(List<String> ports)
        {
            foreach (var port in ports)
            {
                if (TestPort(port))
                {
                    return port;
                }
            }

            return null;
        }

        private bool TestPort(string port)
        {
            SerialPort sPort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);

            sPort.DtrEnable = true;
            sPort.ReadTimeout = 1000;
            sPort.WriteTimeout = 1000;

            try
            {
                Console.WriteLine("Trying: " + port);

                sPort.Open();

                sPort.Write("~");
                Thread.Sleep(1);

                char c = ' ';
                StringBuilder line = new StringBuilder();
                while (sPort.BytesToRead > 0)
                {
                    c = (char)sPort.ReadChar();
                    line.Append(c);
                }

                if (line.ToString().StartsWith("MUTE_MIC"))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                sPort.Close();
            }

            return false;
        }
    }
}
