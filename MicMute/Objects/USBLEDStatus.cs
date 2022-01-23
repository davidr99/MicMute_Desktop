using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.Objects
{
    [StructLayout(LayoutKind.Explicit, Size = 2, CharSet = CharSet.Ansi)]
    public struct USBLEDStatus
    {
        [FieldOffset(0)]
        public byte ReportID;

        [FieldOffset(1)]
        public LEDEnum Status;
    }
}
