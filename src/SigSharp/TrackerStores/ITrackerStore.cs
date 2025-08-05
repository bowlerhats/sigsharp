using System;
using System.Collections.Generic;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

public interface ITrackerStore : IDisposable
{
    IEnumerable<SignalNode> Tracked { get; }
    
    bool HasAny { get; }

    void Track(SignalNode node);
    
    void UnTrack(SignalNode node);

    void Clear();

    bool Contains(SignalNode node);
}