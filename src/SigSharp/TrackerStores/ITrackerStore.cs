using System;
using System.Collections.Generic;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

public interface ITrackerStore : IDisposable
{
    bool HasAny { get; }

    void Track(SignalNode node);
    
    void UnTrack(SignalNode node);

    void Clear();

    bool Contains(SignalNode node);
    
    void WithEach(Action<SignalNode> action);
    void WithEach<TState>(TState state, Action<TState, SignalNode> action);
}