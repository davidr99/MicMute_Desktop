using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using LibUsbDotNet;
using MicMute.Models;
using System.ComponentModel;
using MicMute.Objects;
using CoreAudio;

namespace MicMute
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x2E8A, 0x00C0);

        UsbDevice usbDevice = null!;
        UsbEndpointReader reader = null!;
        UsbEndpointWriter writer = null!;

        bool muted = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnLoadDevices_Click(object sender, RoutedEventArgs e)
        {
            deviceList.Items.Clear();

            LibUsbDotNet.LibUsb.LibUsbDevice.AllDevices.Where(d=>d is LibUsbRegistry && MyUsbFinder.Check(d)).Select(d => new DeviceModel()
            {
                Name = d.Name,
                Path = d.DevicePath
            }).ToList().ForEach(d => deviceList.Items.Add(d));

            var devices = new MMDeviceEnumerator();
        }

        private void btnSelectDevice_Click(object sender, RoutedEventArgs e)
        {
            if (deviceList.SelectedItem != null)
            {
                DeviceModel deviceModel = (deviceList.SelectedItem as DeviceModel)!;
                var selectedDevice = LibUsbDevice.AllDevices.Where(d => d.DevicePath == deviceModel.Path).FirstOrDefault()!;

                if (!selectedDevice.Open(out usbDevice))
                {
                    MessageBox.Show("Error opening USB device!");
                }
            }

            if (usbDevice != null && usbDevice.IsOpen)
            {
                ((IUsbDevice)usbDevice).SetConfiguration(1);
                ((IUsbDevice)usbDevice).ClaimInterface(0);

                writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
                reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                reader.ReadFlush();

                reader.DataReceivedEnabled = true;
                reader.DataReceived += Reader_DataReceived;

                UpdateLED();
            }
        }

        private void Reader_DataReceived(object? sender, EndpointDataEventArgs e)
        {
            if (e.Buffer[0] == 0x01 && e.Buffer[3] == 0x10)
            {
                muted = !muted;
                UpdateLED();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (usbDevice != null)
            {
                usbDevice.Close();
            }
            base.OnClosing(e);
        }

        private void UpdateLED()
        {
            if (usbDevice != null && usbDevice.IsOpen)
            {
                if (muted)
                {
                    WriteLED(LEDEnum.Red);
                }
                else
                {
                    WriteLED(LEDEnum.Green);
                }

                getPrimaryMicDevice().AudioEndpointVolume.Mute = muted;
            }
        }

        private void btnRed_Click(object sender, RoutedEventArgs e)
        {
            WriteLED(LEDEnum.Red);
        }

        private void btnGreen_Click(object sender, RoutedEventArgs e)
        {
            WriteLED(LEDEnum.Green);
        }

        private void btnBlue_Click(object sender, RoutedEventArgs e)
        {
            WriteLED(LEDEnum.Blue);
        }

        private void WriteLED(LEDEnum ledStatus)
        {
            if (usbDevice != null && usbDevice.IsOpen && !writer.IsDisposed)
            {
                writer.SubmitAsyncTransfer(new byte[] {
                    0x05, (byte) ledStatus
                }, 0, 2, 1000, out UsbTransfer usbTransfer);
            }
        }


        private MMDevice getPrimaryMicDevice()
        {
            var enumerator = new MMDeviceEnumerator();
            var result = enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eCommunications);

            return result;
        }
    }
}
