using System;

namespace SigSharp;

public record SignalGroupOptions
{
    private static SignalGroupOptions? _defaults;
    private static SignalGroupOptions? _weakTracked;

    public static SignalGroupOptions Defaults => _defaults ??= new SignalGroupOptions();
    public static SignalGroupOptions WeakTracked => _weakTracked ??= new SignalGroupOptions { WeakTrack = true };
    
    public bool WeakTrack { get; init; }
    
    public bool AutoResumeSuspendedEffects { get; init; } = true;

    public Action<SignalGroup, object>? DisposeLinker { get; init; }
}