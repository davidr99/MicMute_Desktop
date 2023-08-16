using MicMute.Interfaces;
using MicMute.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MicMute.Events;
using HidSharp;

namespace MicMute.MuteDeviceDrivers
{
    internal class HIDMuteButtonDevice : IMuteButtonDriver
    {
        static List<(int vendorId, int productId)> usbIds = new List<(int, int)>()
        {
            (0x2E8A, 0x00C0),
            (0x239A,  0x8108)
        };

        LEDEnum lastStatus { get; set; } = 0;

        HidDevice _hidDevice = null!;
        HidStream _hidStream = null!;

        IAsyncResult _asyncReadResult = null;

        bool isConnected { get; set; }

        byte[] _usbReadBuffer = new byte[32];

        public event EventHandler<MuteButtonPressEvent>? ButtonPressEvent;
        public event EventHandler<HasErrorEvent>? HasErrorEvent;

        public (bool error, string errorMsg) Connect(IMuteButtonDeviceData device)
        {
            var devices = DeviceList.Local;


            foreach ((var vendorId, var productId) in usbIds)
            {
                if (devices.TryGetHidDevice(out _hidDevice, vendorId, productId))
                {
                    if (!_hidDevice.TryOpen(out _hidStream))
                    {
                        return (true, "Error opening USB device!");
                    }
                }
            }

            _asyncReadResult = _hidStream.BeginRead(_usbReadBuffer, 0, _usbReadBuffer.Length, new AsyncCallback(data_Ready), null);

            isConnected = true;

            return (false, String.Empty);
        }

        private void data_Ready(IAsyncResult ar)
        {
            try
            {
                int bytes = _hidStream.EndRead(_asyncReadResult);

                if (bytes > 0)
                {
                    if (_usbReadBuffer[2] == 1)
                    {
                        ButtonPressEvent?.Invoke(this, new MuteButtonPressEvent());
                    }
                }
            }
            catch (TimeoutException timeoutEx)
            {
                // This happends while waiting for data
            }
            catch(System.IO.IOException ex)
            {
                // We got disconnected
                isConnected = false;
            }
            catch(Exception ex)
            { 
                HasErrorEvent?.Invoke(this, new HasErrorEvent() {
                    Error = ex.Message,
                });
            }
            finally
            {
                if (isConnected)
                {
                    // Read next report!
                    _asyncReadResult = _hidStream.BeginRead(_usbReadBuffer, 0, _usbReadBuffer.Length, new AsyncCallback(data_Ready), null);
                }
            }
        }

        public bool WriteLED(LEDEnum ledStatus)
        {
            lastStatus = ledStatus;

            if (_hidStream != null)
            {
                byte[] buffer = new byte[32];

                buffer[0] = (byte) 0x02;            // Lenght
                buffer[1] = (byte) 0x02;            // Report Type 0x02 = LED Status
                buffer[2] = (byte) ledStatus;       // 0x01 = Red | 0x02 = Green | 0x04 = Blue

                try
                {
                    _hidStream.Write(buffer, 0, buffer.Length);
                }
                catch 
                { 
                    return false;
                }
            }

            return true;
        }

        public List<IMuteButtonDeviceData> GetDeviceList()
        {
            var devList = DeviceList.Local.GetAllDevices()
                                    .OfType<HidDevice>()
                                    .ToList<HidDevice>()
                                    .Where(d => usbIds.Select(v => v.productId).Contains(d.ProductID))
                                    .ToList();

            return devList.Select(d => new HIDMuteButtonDeviceData()
            {
                DisplayName = d.GetFriendlyName(),
                Value = d.DevicePath
            }).ToList<IMuteButtonDeviceData>();
        }

        public bool AutoConnect()
        {
            var devices = GetDeviceList();

            if (devices.Count == 1) 
            {
                (bool error, string errorMsg) = Connect(devices[0]);

                return !error;
            }

            return false;
        }

        public void Init()
        {

        }

        public void CloseDevice()
        {
            isConnected = false;

            if (_hidStream != null)
            {
                if (_asyncReadResult != null)
                {
                    try
                    {
                        _hidStream.EndRead(_asyncReadResult);
                    }
                    catch { }
                }

                _hidStream.Close();
            }
        }
    }
}
