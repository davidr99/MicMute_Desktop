using MicMute.Events;
using MicMute.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.Interfaces
{
    internal interface IMuteButtonDriver
    {
        void Init();
        bool AutoConnect();
        (bool error, string errorMsg) Connect(IMuteButtonDeviceData device);
        bool WriteLED(LEDEnum ledStatus);
        List<IMuteButtonDeviceData> GetDeviceList();
        void CloseDevice();


        event EventHandler<MuteButtonPressEvent> ButtonPressEvent;
        event EventHandler<HasErrorEvent>? HasErrorEvent;
    }
}
