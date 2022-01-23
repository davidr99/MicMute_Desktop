using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.Objects
{
    [StructLayout(LayoutKind.Explicit, Size = 9, CharSet = CharSet.Ansi)]
    public struct USBKeyboardMessage
    {
        [FieldOffset(0)]
        public byte ReportId;

        [FieldOffset(1)]
        public byte Modifier;

        [FieldOffset(2)]
        public byte unused;

        [FieldOffset(3)]
        public byte Key;

        [FieldOffset(4)]
        public byte unused2;

        [FieldOffset(5)]
        public byte unused3;

        [FieldOffset(6)]
        public byte unused4;

        [FieldOffset(7)]
        public byte unused5;

        [FieldOffset(8)]
        public byte unused6;
    }
}
