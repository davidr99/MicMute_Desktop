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
using System.Timers;
using System.Runtime.InteropServices;

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
        LEDEnum ledColor = 0;
        MMDevice mic = null!;

        public MainWindow()
        {
            InitializeComponent();

            mic = getPrimaryMicDevice();

            if (mic != null)
            {
                mic.AudioEndpointVolume!.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
        }

        private void MicCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            muted = GetMicStatus();
            UpdateLED();
        }

        private void btnLoadDevices_Click(object sender, RoutedEventArgs e)
        {
            deviceList.Items.Clear();

            LibUsbDotNet.LibUsb.LibUsbDevice.AllDevices.Where(d => d is LibUsbRegistry && MyUsbFinder.Check(d)).Select(d => new DeviceModel()
            {
                Name = d.Name,
                Path = d.DevicePath
            }).ToList().ForEach(d => deviceList.Items.Add(d));
        }

        private void btnSelectDevice_Click(object sender, RoutedEventArgs e)
        {
            if (deviceList.SelectedItem != null)
            {
                DeviceModel deviceModel = (deviceList.SelectedItem as DeviceModel)!;
                var selectedDevice = LibUsbDevice.AllDevices.Where(d => d.DevicePath == deviceModel.Path).FirstOrDefault()!;

                if (selectedDevice != null && !selectedDevice.Open(out usbDevice))
                {
                    MessageBox.Show("Error opening USB device!");
                }
                else if (selectedDevice == null)
                {
                    MessageBox.Show("Error Could not find device!");
                }


                muted = GetMicStatus();
                ConnectUSB();
            }
        }

        #region USB Stuff
        private void ConnectUSB()
        {
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
            var keyboardMessage = ReadBuffer<USBKeyboardMessage>(e.Buffer);
            if (keyboardMessage.ReportId == 0x01 && keyboardMessage.Key == 0x10)
            {
                var mic = getPrimaryMicDevice();

                if (mic != null && mic.AudioEndpointVolume != null)
                {
                    mic.AudioEndpointVolume.Mute = !mic.AudioEndpointVolume.Mute;
                    muted = mic.AudioEndpointVolume.Mute;
                }

                UpdateLED();
            }
        }

        public static T ReadBuffer<T>(byte[] data) where T : struct
        {
            T dataObject;
            int size = Marshal.SizeOf<T>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, ptr, size);
            dataObject = (T) Marshal.PtrToStructure(ptr, typeof(T))!;
            Marshal.FreeHGlobal(ptr);

            return dataObject;
        }

        public static byte[] WriteBuffer<T>(T status) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] packet = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(status));
            Marshal.StructureToPtr<T>(status, ptr, true);
            Marshal.Copy(ptr, packet, 0, size);
            Marshal.FreeHGlobal(ptr);

            return packet;
        }

        private void UpdateLED()
        {
            if (usbDevice != null && usbDevice.IsOpen)
            {
                if (muted)
                {
                    Uri iconUri = new Uri("pack://application:,,,/icons/muted.ico", UriKind.RelativeOrAbsolute);
                    tbiNotification.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
                    WriteLED(LEDEnum.Red);
                }
                else
                {
                    Uri iconUri = new Uri("pack://application:,,,/icons/unmuted.ico", UriKind.RelativeOrAbsolute);
                    tbiNotification.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
                    WriteLED(LEDEnum.Green);
                }
            }
        }

        private void WriteLED(LEDEnum ledStatus)
        {
            if (usbDevice != null && usbDevice.IsOpen && !writer.IsDisposed)
            {
                byte[] buffer = WriteBuffer<USBLEDStatus>(new USBLEDStatus()
                {
                    ReportID = 0x05,
                    Status = ledStatus
                });

                writer.SubmitAsyncTransfer(buffer, 0, 2, 1000, out UsbTransfer usbTransfer);

                this.Dispatcher.Invoke(() =>
                {
                    var color = Color.FromRgb(
                            (byte)(ledStatus.HasFlag(LEDEnum.Red) ? 255 : 0),
                            (byte)(ledStatus.HasFlag(LEDEnum.Green) ? 255 : 0),
                            (byte)(ledStatus.HasFlag(LEDEnum.Blue) ? 255 : 0));

                    ledDisplay.Fill = new SolidColorBrush(color);
                });
            }
        }

        private void deviceList_Loaded(object sender, RoutedEventArgs e)
        {
            deviceList.Items.Clear();

            var devList = LibUsbDotNet.LibUsb.LibUsbDevice.AllDevices.Where(d => d is LibUsbRegistry && MyUsbFinder.Check(d));

            devList.Select(d => new DeviceModel()
            {
                Name = d.Name,
                Path = d.DevicePath
            }).ToList().ForEach(d => deviceList.Items.Add(d));

            if (deviceList.Items.Count > 0)
            {
                usbDevice = devList.FirstOrDefault()!.Device;
                muted = GetMicStatus();
                ConnectUSB();
            }
        }

        #endregion USB Stuff

        private void btnRed_Click(object sender, RoutedEventArgs e)
        {
            ledColor = ledColor ^ LEDEnum.Red;

            WriteLED(ledColor);
        }

        private void btnGreen_Click(object sender, RoutedEventArgs e)
        {
            ledColor = ledColor ^ LEDEnum.Green;

            WriteLED(ledColor);
        }

        private void btnBlue_Click(object sender, RoutedEventArgs e)
        {
            ledColor = ledColor ^ LEDEnum.Blue;

            WriteLED(ledColor);
        }

        #region Mic Stuff

        private MMDevice getPrimaryMicDevice()
        {
            var enumerator = new MMDeviceEnumerator();
            var result = enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eCommunications);

            return result;
        }

        private bool GetMicStatus()
        {
            bool micStatus = false;

            var mic = getPrimaryMicDevice();

            if (mic != null)
            {
                micStatus = mic.AudioEndpointVolume!.Mute;
            }

            return micStatus;
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            muted = data.Muted;
            UpdateLED();
        }

        #endregion Mic Stuff

        #region Tray Stuff
        private void tbiNotification_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Focus();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (usbDevice != null)
            {
                usbDevice.Close();
            }
            tbiNotification.Dispose();
            base.OnClosing(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MenuItem_Click_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Click_Open(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Focus();
        }

        #endregion Tray Stuff
    }
}
