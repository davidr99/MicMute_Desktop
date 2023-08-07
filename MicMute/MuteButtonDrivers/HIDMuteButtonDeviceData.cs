using MicMute.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MicMute.MuteDeviceDrivers
{
    internal class HIDMuteButtonDeviceData : IMuteButtonDeviceData
    {
        public string DisplayName { get; set; } = String.Empty;
        public string Value { get; set; } = String.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
