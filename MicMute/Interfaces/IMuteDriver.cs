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
    internal interface IMuteDriver
    {
        void Init();
        bool AutoConnect();
        (bool error, string errorMsg) Connect(IMuteDevice device);
        void WriteLED(LEDEnum ledStatus);
        List<IMuteDevice> GetDeviceList();
        void CloseDevice();


        event EventHandler<MuteButtonPressEvent> ButtonPressEvent;
    }
}
