namespace SigSharp;

public sealed partial class GlobalSignalOptions
{
    public LoggingOptions Logging { get; set; } = new();
    
    public SignalGroupOptions SignalGroup { get; set; } = SignalGroupOptions.Defaults;
}