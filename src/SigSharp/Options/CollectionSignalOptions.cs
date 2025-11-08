namespace SigSharp;

public record CollectionSignalOptions
{
    public static CollectionSignalOptions Defaults { get; } = new();
    
    public DisposedSignalAccess.Strategy DisposedAccessStrategy { get; init; }
        = DisposedSignalAccess.Strategy.DefaultValue;
    
    public SignalAccessStrategy AccessStrategy { get; init; }
        = SignalAccessStrategy.Unrestricted;
}