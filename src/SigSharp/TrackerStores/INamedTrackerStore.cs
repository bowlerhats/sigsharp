using System;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

public record struct ComputedSignalId(string Name, int Line);
public sealed record ComputedSignalIdRef(ComputedSignalId Id);

internal interface INamedTrackerStore : IDisposable
{
    T? LookupComputed<T>(ComputedSignalId id)
        where T : SignalNode;
    
    void Clear();
    
    bool Track(SignalNode node, ComputedSignalId id);
}