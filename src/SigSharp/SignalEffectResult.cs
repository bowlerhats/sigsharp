namespace SigSharp;

public readonly struct SignalEffectResult
{
    internal bool ShouldStop { get; init; }
    internal bool ShouldDestroy { get; init; }
    
    public static SignalEffectResult Ok()
    {
        return new SignalEffectResult();
    }

    public static SignalEffectResult Stop()
    {
        return new SignalEffectResult { ShouldStop = true };
    }

    public static SignalEffectResult Destroy()
    {
        return new SignalEffectResult { ShouldDestroy = true };
    }
}