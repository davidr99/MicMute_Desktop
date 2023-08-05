using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.Models
{
    public class DeviceModel
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name;
        }
    }
}
