using System;

namespace MicMute;
public class HasErrorEvent : EventArgs
{
    public string? Error { get; set;}
}
