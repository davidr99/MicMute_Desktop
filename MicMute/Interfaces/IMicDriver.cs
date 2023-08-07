using CoreAudio;
using MicMute.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.Interfaces
{
    internal interface IMicDriver
    {
        public void Init();
        public void Cleanup();

        public void Mute();
        public void Unmute();
        public void ToggleMute();

        public bool Muted { get; }
        event EventHandler<MicNotificationDataEvent> MicNotification;
    }
}
