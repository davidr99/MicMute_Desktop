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
using System.ComponentModel;
using MicMute.Objects;
using CoreAudio;
using System.Timers;
using System.Runtime.InteropServices;
using MicMute.Interfaces;
using MicMute.MuteDeviceDrivers;
using MicMute.Events;
using MicMute.MicDrivers;
using System.Diagnostics;
using System.Windows.Interop;

namespace MicMute
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IMuteButtonDriver muteDriver = new HIDMuteButtonDevice();
        IMicDriver micDriver = new MicDriver();

        bool _muted = false;
        LEDEnum ledColor = 0;

        bool isConnected 
        {
            get
            {
                return !deviceList.IsEnabled;
            }
            set
            {
                deviceList.IsEnabled = !value;
                btnSelectDevice.Visibility = value ? Visibility.Hidden : Visibility.Visible;
                btnDisconnect.Visibility = value ? Visibility.Visible : Visibility.Hidden;
            }

        }

        bool muted
        {
            get
            {
                return _muted;
            }
            set
            {
                _muted = value;
                if (_muted)
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

        public MainWindow()
        {
            InitializeComponent();

            micDriver.Init();
            micDriver.MicNotification += MicDriver_Notification;

            muteDriver.ButtonPressEvent += MuteButtonPress_Event;
            muteDriver.HasErrorEvent += MuteButtonHasError_Event;

            disconnect();
            setupDriver();
        }


        private void MuteButtonPress_Event(object? sender, MuteButtonPressEvent e)
        {
            if (muted)
            {
                micDriver.Unmute();
                muted = false;
            }
            else
            {
                micDriver.Mute();
                muted = true;
            }
        }

        private void MicCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            muted = micDriver.Muted;
        }

        private void btnLoadDevices_Click(object sender, RoutedEventArgs e)
        {
            loadDevices();
        }

        private void btnSelectDevice_Click(object sender, RoutedEventArgs e)
        {
            if (deviceList.SelectedItem != null)
            {
                IMuteButtonDeviceData deviceModel = (deviceList.SelectedItem as IMuteButtonDeviceData)!;

                (bool error, string errorMsg) = muteDriver.Connect(deviceModel);

                if (error)
                {
                    MessageBox.Show(errorMsg);
                }
                else
                {
                    isConnected = true;
                }

                muted = micDriver.Muted;
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            muteDriver.CloseDevice();

            disconnect();
        }

        private void deviceList_Loaded(object sender, RoutedEventArgs e)
        {
            loadDevices();
        }

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

        private void cbDeviceType_DropDownClosed(object sender, EventArgs e)
        {
            muteDriver?.CloseDevice();

            if (cbDeviceType.SelectedItem.ToString()!.Contains("Serial"))
            {
                muteDriver = new SerialMuteButtonDevice();
            }
            else if (cbDeviceType.SelectedItem.ToString()!.Contains("HID"))
            {
                muteDriver = new HIDMuteButtonDevice();
            }

            setupDriver();
            loadDevices();
        }

        private void loadDevices()
        {
            deviceList.Items.Clear();

            muteDriver.GetDeviceList().ForEach(d => deviceList.Items.Add(d));
        }

        private void setupDriver()
        {
            autoConnect();
        }

        private void MuteButtonHasError_Event(object? sender, HasErrorEvent e)
        {
            MessageBox.Show(e.Error, "Connection Errror");
        }

        private void disconnect()
        {
            isConnected = false;
        }

        private void updateButtonStatus()
        {
            muted = _muted;
        }

        private bool autoConnect()
        {
            if (muteDriver.AutoConnect())
            {
                isConnected = true;
                updateButtonStatus();
            }

            return isConnected;
        }

        private void WriteLED(LEDEnum ledStatus)
        {
            if (!muteDriver.WriteLED(ledStatus))
            {
                disconnect();
                MessageBox.Show("Try and reconnect", "Error connecting to device.", MessageBoxButton.OK);
            }

            this.Dispatcher.Invoke(() =>
            {
                var color = Color.FromRgb(
                        (byte)(ledStatus.HasFlag(LEDEnum.Red) ? 255 : 0),
                        (byte)(ledStatus.HasFlag(LEDEnum.Green) ? 255 : 0),
                        (byte)(ledStatus.HasFlag(LEDEnum.Blue) ? 255 : 0));

                ledDisplay.Fill = new SolidColorBrush(color);
            });
        }

        #region Mic Stuff

        private void MicDriver_Notification(object? sender, MicNotificationDataEvent data)
        {
            muted = data.Muted;
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
            muteDriver.CloseDevice();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            muteDriver.CloseDevice();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddUsbConnectEvent();
        }

        #region Detect USB Changes

        private void AddUsbConnectEvent()
        {
            HwndSource hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (hwndSource != null)
            {
                IntPtr windowHandle = hwndSource.Handle;
                hwndSource.AddHook(UsbNotificationHandler);
                USBDetector.RegisterUsbDeviceNotification(windowHandle);
            }
        }

        private IntPtr UsbNotificationHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == USBDetector.UsbDevicechange)
            {
                switch ((int)wparam)
                {
                    case USBDetector.UsbDeviceRemoved:
                        //MessageBox.Show("USB Removed");
                        loadDevices();
                        if (deviceList.Items.Count == 0)
                        {
                            muteDriver.CloseDevice();
                            isConnected = false;
                        }
                        break;
                    case USBDetector.NewUsbDeviceConnected:
                        if (!isConnected)
                        {
                            loadDevices();
                            autoConnect();
                        }
                        break;
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        #endregion
    }
}
