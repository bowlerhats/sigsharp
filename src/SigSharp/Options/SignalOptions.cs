using System.Collections;

namespace SigSharp;

public record SignalOptions
{
    public static SignalOptions Defaults { get; } = new();
    
    public IEqualityComparer? EqualityComparer { get; init; }

    public DisposedSignalAccess.Strategy DisposedAccessStrategy { get; init; }
        = DisposedSignalAccess.Strategy.LastScalarOrDefault;

    public SignalAccessStrategy AccessStrategy { get; init; }
        = SignalAccessStrategy.PreemptiveLock;
}