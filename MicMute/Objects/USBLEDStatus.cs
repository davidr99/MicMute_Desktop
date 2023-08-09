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
        public byte Length;

        [FieldOffset(1)]
        public byte ReportID;

        [FieldOffset(2)]
        public LEDEnum Status;
    }
}
