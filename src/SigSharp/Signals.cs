using SigSharp.Utils;

namespace SigSharp;

public static partial class Signals
{
    /// <summary>
    /// Global options for signal handling
    /// </summary>
    public static GlobalSignalOptions Options { get; } = new();
    
    public static SignalSuspender Suspend()
    {
        return SignalGroup.CreateSuspended();
    }
}