using System.Collections;

namespace SigSharp;

public record ComputedSignalOptions
{
    public static ComputedSignalOptions Defaults { get; } = new();
    
    public IEqualityComparer EqualityComparer { get; init; }
}