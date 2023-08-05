using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MicMute.Interfaces
{
    internal interface IMuteDevice
    {
        string DisplayName { get; set; }
        string Value { get; set; }
    }
}
