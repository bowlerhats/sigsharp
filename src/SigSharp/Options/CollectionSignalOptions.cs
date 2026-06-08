namespace SigSharp;

public record CollectionSignalOptions
{
    public static CollectionSignalOptions Defaults { get; } = new();
    
    public DisposedSignalAccess.Strategy DisposedAccessStrategy { get; init; }
        = DisposedSignalAccess.Strategy.DefaultValue;
    
    public SignalAccessStrategy AccessStrategy { get; init; }
        = SignalAccessStrategy.PreemptiveLock;

    /// <summary>
    /// When true, it will pre-check backing collections to determine
    /// if the update will need to mutate or not, and only request update access if it would mutate. <br/>
    /// This is very useful to reduce lock contentions and to reduce changed events. Otherwise 'changed' events
    /// may be fired even when no actual mutation was done.<br/>
    /// </summary>
    public bool PrecheckUpdateRequests { get; init; } = true;
}