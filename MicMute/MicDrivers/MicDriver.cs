using CoreAudio;
using MicMute.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicMute.MicDrivers
{
    internal class MicDriver : IMicDriver
    {
        public bool Muted 
        { 
            get
            {
                return GetMicStatus();
            }
        }

        protected MMDevice mic = null!;

        public event EventHandler<MicNotificationData>? MicNotification;

        public void Cleanup()
        {
            
        }

        public void Init()
        {
            mic = getPrimaryMicDevice();

            if (mic != null)
            {
                mic.AudioEndpointVolume!.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
        }

        public void Mute()
        {
            MuteUnmute(true);
        }

        public void Unmute()
        {
            MuteUnmute(false);
        }

        public void ToggleMute()
        {
            MuteUnmute(null);
        }

        private void MuteUnmute(bool? mute)
        {
            var mic = getPrimaryMicDevice();

            if (mic != null && mic.AudioEndpointVolume != null)
            {
                mic.AudioEndpointVolume.Mute = mute ?? !mic.AudioEndpointVolume.Mute;
            }
        }

        private MMDevice getPrimaryMicDevice()
        {
            var enumerator = new MMDeviceEnumerator(Guid.NewGuid());
            var result = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

            return result;
        }

        private bool GetMicStatus()
        {
            bool micStatus = false;

            var mic = getPrimaryMicDevice();

            if (mic != null)
            {
                micStatus = mic.AudioEndpointVolume!.Mute;
            }

            return micStatus;
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            MicNotification?.Invoke(this, new MicNotificationData()
            {
                Muted = Muted
            });
        }
    }
}
