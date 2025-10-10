using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

public interface ITrackerStore : IDisposable
{
    bool HasAny { get; }

    bool Track(SignalNode node);
    
    void UnTrack(SignalNode node);

    void Clear();

    bool Contains(SignalNode node);
    
    void WithEach(Action<SignalNode> action);
    void WithEach<TState>(TState state, Action<TState, SignalNode> action);
    ValueTask WithEachAsync(Func<SignalNode, ValueTask> action);
    ValueTask WithEachAsync<TState>(TState state, Func<TState, SignalNode, ValueTask> action);

    void Collect<TSignalNode>(ICollection<TSignalNode> target)
        where TSignalNode : SignalNode;
}