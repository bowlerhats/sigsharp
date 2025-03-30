using System.Collections;

namespace SigSharp;

public record SignalOptions
{
    public static SignalOptions Defaults { get; } = new();
    
    public IEqualityComparer EqualityComparer { get; init; }
}