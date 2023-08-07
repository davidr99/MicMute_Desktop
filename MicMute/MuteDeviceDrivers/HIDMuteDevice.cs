using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using LibUsbDotNet;
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

namespace MicMute.MuteDeviceDrivers
{
    internal class HIDMuteDevice : IMuteDriver
    {
        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x2E8A, 0x00C0);

        UsbDevice usbDevice = null!;
        UsbEndpointReader reader = null!;
        UsbEndpointWriter writer = null!;

        LEDEnum lastStatus { get; set; } = 0;

        public event EventHandler<MuteButtonPressEvent> ButtonPressEvent;

        public (bool error, string errorMsg) Connect(IMuteDevice device)
        {
            var selectedDevice = LibUsbDevice.AllDevices.Where(d => d.DevicePath == device.Value).FirstOrDefault()!;

            if (selectedDevice != null && !selectedDevice.Open(out usbDevice))
            {
                return (true, "Error opening USB device!");
            }
            else if (selectedDevice == null)
            {
                return (true, "Error Could not find device!");
            }

            if (usbDevice != null && usbDevice.IsOpen)
            {
                ((IUsbDevice)usbDevice).SetConfiguration(1);
                ((IUsbDevice)usbDevice).ClaimInterface(0);

                writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
                reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                reader.ReadFlush();

                reader.DataReceivedEnabled = true;
                reader.DataReceived += reader_DataReceived;

                WriteLED(lastStatus);
            }

            return (false, String.Empty);
        }

        public void WriteLED(LEDEnum ledStatus)
        {
            lastStatus = ledStatus;

            if (usbDevice != null && usbDevice.IsOpen && !writer.IsDisposed)
            {
                byte[] buffer = writeBuffer<USBLEDStatus>(new USBLEDStatus()
                {
                    ReportID = 0x05,
                    Status = ledStatus
                });

                writer.SubmitAsyncTransfer(buffer, 0, 2, 1000, out UsbTransfer usbTransfer);
            }
        }

        public List<IMuteDevice> GetDeviceList()
        {
            var devList = LibUsbDevice.AllDevices.Where(d => d is LibUsbRegistry && MyUsbFinder.Check(d));

            return devList.Select(d => new HIDMuteDeviceData()
            {
                DisplayName = d.Name,
                Value = d.DevicePath
            }).ToList<IMuteDevice>();
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
            if (usbDevice != null)
            {
                usbDevice.Close();
            }
        }

        private void reader_DataReceived(object? sender, EndpointDataEventArgs e)
        {
            var keyboardMessage = readBuffer<USBKeyboardMessage>(e.Buffer);
            if (keyboardMessage.ReportId == 0x01 && keyboardMessage.Key == 0x10)
            {
                ButtonPressEvent?.Invoke(this, new MuteButtonPressEvent());
            }
        }

        private static T readBuffer<T>(byte[] data) where T : struct
        {
            T dataObject;
            int size = Marshal.SizeOf<T>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, ptr, size);
            dataObject = (T)Marshal.PtrToStructure(ptr, typeof(T))!;
            Marshal.FreeHGlobal(ptr);

            return dataObject;
        }

        private static byte[] writeBuffer<T>(T status) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] packet = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(status));
            Marshal.StructureToPtr<T>(status, ptr, true);
            Marshal.Copy(ptr, packet, 0, size);
            Marshal.FreeHGlobal(ptr);

            return packet;
        }
    }
}
