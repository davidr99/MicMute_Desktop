using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.Objects
{
    [Flags]
    public enum LEDEnum : byte
    {
        Red = 0x01,
        Green = 0x02,
        Blue = 0x04
    }
}
