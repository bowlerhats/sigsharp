using System.Collections;

namespace SigSharp;

public record ComputedSignalOptions
{
    public static ComputedSignalOptions Defaults { get; } = new();
    
    public IEqualityComparer? EqualityComparer { get; init; }
    
    public DisposedSignalAccess.Strategy DisposedAccessStrategy { get; init; }
        = DisposedSignalAccess.Strategy.LastScalarOrDefault;
    
    public SignalAccessStrategy AccessStrategy { get; init; }
        = SignalAccessStrategy.PreemptiveLock;
    
    public SignalDefaultValueProvider DefaultValueProvider { get; init; }
        = SignalDefaultValueProvider.DefaultInstance;
}