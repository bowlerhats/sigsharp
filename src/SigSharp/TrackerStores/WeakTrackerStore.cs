using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp.TrackerStores;

public sealed class WeakTrackerStore : ITrackerStore
{
    public IEnumerable<SignalNode> Tracked => _tracked.Select(static d => d.Key);

    public bool HasAny => _tracked.Any();
    
    // TODO: Use WeakSet when stable
    private readonly ConditionalWeakTable<SignalNode, object> _tracked = [];
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed)
            return;

        var wasDisposed = Interlocked.CompareExchange(ref _disposed, true, false); 
        if (!wasDisposed && _disposed)
        {
            _tracked.Clear();
        }
    }

    public void Clear()
    {
        _tracked.Clear();
    }

    public bool Contains(SignalNode node)
    {
        this.CheckDisposed();

        return _tracked.TryGetValue(node, out _);
    }

    public bool Track(SignalNode node)
    {
        this.CheckDisposed();
        
        return _tracked.TryAdd(node, SignalNode.EmptyObject);
    }
    
    public void UnTrack(SignalNode node)
    {
        this.CheckDisposed();
        
        _tracked.RemoveSafe(node);
    }
    
    public void WithEach(Action<SignalNode> action)
    {
        foreach (var (node, _) in _tracked)
        {
            action(node);
        }
    }
    
    public void WithEach<TState>(TState state, Action<TState, SignalNode> action)
    {
        foreach (var (node, _) in _tracked)
        {
            action(state, node);
        }
    }

    public async ValueTask WithEachAsync(Func<SignalNode, ValueTask> action)
    {
        foreach (var (node, _) in _tracked)
        {
            await action(node);
        }
    }
    
    public async ValueTask WithEachAsync<TState>(TState state, Func<TState, SignalNode, ValueTask> action)
    {
        foreach (var (node, _) in _tracked)
        {
            await action(state, node);
        }
    }

    public void Collect<TSignalNode>(ICollection<TSignalNode> target)
        where TSignalNode : SignalNode
    {
        foreach (var (node, _) in _tracked)
        {
            if (node is TSignalNode signalNode)
                target.Add(signalNode);
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}